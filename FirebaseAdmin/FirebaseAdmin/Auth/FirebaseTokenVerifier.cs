// Copyright 2018, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A helper class that can be used to verify signed Firebase tokens (e.g. ID tokens).
    /// </summary>
    internal sealed class FirebaseTokenVerifier
    {
        private const string IdTokenCertUrl = "https://www.googleapis.com/robot/v1/metadata/x509/"
            + "securetoken@system.gserviceaccount.com";

        private const string FirebaseAudience ="https://identitytoolkit.googleapis.com/"
            + "google.identity.identitytoolkit.v1.IdentityToolkit";

        private static readonly IReadOnlyList<string> StandardClaims =
            ImmutableList.Create<string>("iss", "aud", "exp", "iat", "sub", "uid");

        // See http://oid-info.com/get/2.16.840.1.101.3.4.2.1
        private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

        public string ProjectId { get; }
        private readonly string _shortName;
        private readonly string _articledShortName;
        private readonly string _operation;
        private readonly string _url;
        private readonly string _issuer;
        private readonly IClock _clock;
        private readonly IPublicKeySource _keySource;

        public FirebaseTokenVerifier(FirebaseTokenVerifierArgs args)
        {
            ProjectId = args.ProjectId.ThrowIfNullOrEmpty(nameof(args.ProjectId));
            _shortName = args.ShortName.ThrowIfNullOrEmpty(nameof(args.ShortName));
            _operation = args.Operation.ThrowIfNullOrEmpty(nameof(args.Operation));
            _url = args.Url.ThrowIfNullOrEmpty(nameof(args.Url));
            _issuer = args.Issuer.ThrowIfNullOrEmpty(nameof(args.Issuer));
            _clock = args.Clock.ThrowIfNull(nameof(args.Clock));
            _keySource = args.PublicKeySource.ThrowIfNull(nameof(args.PublicKeySource));
            if ("aeiou".Contains(_shortName.ToLower().Substring(0, 1)))
            {
                _articledShortName = $"an {_shortName}";
            }
            else
            {
                _articledShortName = $"a {_shortName}";
            }
        }

        public async Task<FirebaseToken> VerifyTokenAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"{_shortName} must not be null or empty.");
            }
            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw new FirebaseException($"Incorrect number of segments in ${_shortName}.");
            }

            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            var payload = JwtUtils.Decode<FirebaseTokenArgs>(segments[1]);
            var projectIdMessage = $"Make sure the {_shortName} comes from the same Firebase "
                + "project as the credential used to initialize this SDK.";
            var verifyTokenMessage = $"See {_url} for details on how to retrieve a value "
                + $"{_shortName}.";
            var issuer = _issuer + ProjectId;
            string error = null;
            if (string.IsNullOrEmpty(header.KeyId))
            {
                if (FirebaseAudience == payload.Audience)
                {
                    error = $"{_operation} expects {_articledShortName}, but was given a custom "
                        + "token.";
                }
                else if (header.Algorithm == "HS256")
                {
                    error = $"{_operation} expects {_articledShortName}, but was given a legacy "
                        + "custom token.";
                }
                else
                {
                    error = $"Firebase {_shortName} has no 'kid' claim.";
                }
            }
            else if (header.Algorithm != "RS256")
            {
                error = $"Firebase {_shortName} has incorrect algorithm. Expected RS256 but got "
                    + $"{header.Algorithm}. {verifyTokenMessage}";
            }
            else if (ProjectId != payload.Audience)
            {
                error = $"{_shortName} has incorrect audience (aud) claim. Expected {ProjectId} "
                    + $"but got {payload.Audience}. {projectIdMessage} {verifyTokenMessage}";
            }
            else if (payload.Issuer != issuer)
            {
                error = $"{_shortName} has incorrect issuer (iss) claim. Expected {issuer} but "
                    + $"got {payload.Issuer}.  {projectIdMessage} {verifyTokenMessage}";
            }
            else if (payload.IssuedAtTimeSeconds > _clock.UnixTimestamp())
            {
                error = $"Firebase {_shortName} issued at future timestamp";
            }
            else if (payload.ExpirationTimeSeconds < _clock.UnixTimestamp())
            {
                error = $"Firebase {_shortName} expired at {payload.ExpirationTimeSeconds}";
            }
            else if (string.IsNullOrEmpty(payload.Subject))
            {
                error = $"Firebase {_shortName} has no or empty subject (sub) claim.";
            }
            else if (payload.Subject.Length > 128)
            {
                error = $"Firebase {_shortName} has a subject claim longer than 128 characters.";
            }
            
            if (error != null)
            {
                throw new FirebaseException(error);
            }

            await VerifySignatureAsync(segments, header.KeyId, cancellationToken)
                .ConfigureAwait(false);
            var allClaims = JwtUtils.Decode<Dictionary<string, object>>(segments[1]);
            // Remove standard claims, so that only custom claims would remain.
            foreach (var claim in StandardClaims)
            {
                allClaims.Remove(claim);
            }
            payload.Claims = allClaims.ToImmutableDictionary();
            return new FirebaseToken(payload);
        }

        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature. The JWT must be specified
        /// as an array of three segments (header, body and signature).
        /// </summary>
        private async Task VerifySignatureAsync(
            string[] segments, string keyId, CancellationToken cancellationToken)
        {
            byte[] hash;
            using (var hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(
                    Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"));
            }
            var signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            var keys = await _keySource.GetPublicKeysAsync(cancellationToken)
                .ConfigureAwait(false);
            var verified = keys.Any(key =>
            {
#if NETSTANDARD1_5 || NETSTANDARD2_0                
                return key.Id == keyId && key.RSA.VerifyHash(
                    hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
#elif NET45
                return key.Id == keyId && 
                    ((RSACryptoServiceProvider) key.RSA).VerifyHash(hash, Sha256Oid, signature);
#else
#error Unsupported target
#endif  
            });
            if (!verified)
            {
                throw new FirebaseException($"Failed to verify {_shortName} signature.");
            }
        }

        internal static FirebaseTokenVerifier CreateIDTokenVerifier(FirebaseApp app)
        {
            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to verify ID tokens.");
            }
            var keySource = new HttpPublicKeySource(
                IdTokenCertUrl, SystemClock.Default, new HttpClientFactory());
            var args = new FirebaseTokenVerifierArgs()
            {
                ProjectId = projectId,
                ShortName = "ID token",
                Operation = "VerifyIdTokenAsync()",
                Url = "https://firebase.google.com/docs/auth/admin/verify-id-tokens",
                Issuer = "https://securetoken.google.com/",
                Clock = SystemClock.Default,
                PublicKeySource = keySource,
            };
            return new FirebaseTokenVerifier(args);
        }
    }

    internal sealed class FirebaseTokenVerifierArgs
    {
        public string ProjectId { get; set; }
        public string ShortName { get; set; }
        public string Operation { get; set; }
        public string Url { get; set; }
        public string Issuer { get; set; }
        public IClock Clock { get; set; }
        public IPublicKeySource PublicKeySource { get; set; }
    }
}
