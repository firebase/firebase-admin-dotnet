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
using System.Diagnostics.CodeAnalysis;
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

        private const string SessionCookieCertUrl = "https://www.googleapis.com/identitytoolkit/v3/"
            + "relyingparty/publicKeys";

        private const string FirebaseAudience = "https://identitytoolkit.googleapis.com/"
            + "google.identity.identitytoolkit.v1.IdentityToolkit";

        private const long ClockSkewSeconds = 5 * 60;

        // See http://oid-info.com/get/2.16.840.1.101.3.4.2.1
        private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

        private static readonly IReadOnlyList<string> StandardClaims =
            ImmutableList.Create<string>("iss", "aud", "exp", "iat", "sub", "uid");

        private readonly string shortName;
        private readonly string articledShortName;
        private readonly string operation;
        private readonly string url;
        private readonly string issuer;
        private readonly IClock clock;
        private readonly IPublicKeySource keySource;
        private readonly AuthErrorCode invalidTokenCode;
        private readonly AuthErrorCode expiredIdTokenCode;

        internal FirebaseTokenVerifier(FirebaseTokenVerifierArgs args)
        {
            this.ProjectId = args.ProjectId.ThrowIfNullOrEmpty(nameof(args.ProjectId));
            this.shortName = args.ShortName.ThrowIfNullOrEmpty(nameof(args.ShortName));
            this.operation = args.Operation.ThrowIfNullOrEmpty(nameof(args.Operation));
            this.url = args.Url.ThrowIfNullOrEmpty(nameof(args.Url));
            this.issuer = args.Issuer.ThrowIfNullOrEmpty(nameof(args.Issuer));
            this.clock = args.Clock.ThrowIfNull(nameof(args.Clock));
            this.keySource = args.PublicKeySource.ThrowIfNull(nameof(args.PublicKeySource));
            this.invalidTokenCode = args.InvalidTokenCode;
            this.expiredIdTokenCode = args.ExpiredTokenCode;
            if ("aeiou".Contains(this.shortName.ToLower().Substring(0, 1)))
            {
                this.articledShortName = $"an {this.shortName}";
            }
            else
            {
                this.articledShortName = $"a {this.shortName}";
            }
        }

        public string ProjectId { get; }

        internal static FirebaseTokenVerifier CreateIDTokenVerifier(FirebaseApp app)
        {
            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to verify ID tokens.");
            }

            var keySource = new HttpPublicKeySource(
                IdTokenCertUrl, SystemClock.Default, app.Options.HttpClientFactory);
            var args = FirebaseTokenVerifierArgs.ForIdTokens(projectId, keySource);
            return new FirebaseTokenVerifier(args);
        }

        internal static FirebaseTokenVerifier CreateSessionCookieVerifier(FirebaseApp app)
        {
            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to verify session cookies.");
            }

            var keySource = new HttpPublicKeySource(
                SessionCookieCertUrl, SystemClock.Default, app.Options.HttpClientFactory);
            var args = FirebaseTokenVerifierArgs.ForSessionCookies(projectId, keySource);
            return new FirebaseTokenVerifier(args);
        }

        internal async Task<FirebaseToken> VerifyTokenAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException($"{this.shortName} must not be null or empty.");
            }

            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw this.CreateException($"Incorrect number of segments in {this.shortName}.");
            }

            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            var payload = JwtUtils.Decode<FirebaseTokenArgs>(segments[1]);
            var projectIdMessage = $"Make sure the {this.shortName} comes from the same Firebase "
                + "project as the credential used to initialize this SDK.";
            var verifyTokenMessage = $"See {this.url} for details on how to retrieve a value "
                + $"{this.shortName}.";
            var issuer = this.issuer + this.ProjectId;
            string error = null;
            var errorCode = this.invalidTokenCode;
            var currentTimeInSeconds = this.clock.UnixTimestamp();

            if (string.IsNullOrEmpty(header.KeyId))
            {
                if (payload.Audience == FirebaseAudience)
                {
                    error = $"{this.operation} expects {this.articledShortName}, but was given a custom "
                        + "token.";
                }
                else if (header.Algorithm == "HS256")
                {
                    error = $"{this.operation} expects {this.articledShortName}, but was given a legacy "
                        + "custom token.";
                }
                else
                {
                    error = $"Firebase {this.shortName} has no 'kid' claim.";
                }
            }
            else if (header.Algorithm != "RS256")
            {
                error = $"Firebase {this.shortName} has incorrect algorithm. Expected RS256 but got "
                    + $"{header.Algorithm}. {verifyTokenMessage}";
            }
            else if (this.ProjectId != payload.Audience)
            {
                error = $"Firebase {this.shortName} has incorrect audience (aud) claim. Expected "
                    + $"{this.ProjectId} but got {payload.Audience}. {projectIdMessage} "
                    + $"{verifyTokenMessage}";
            }
            else if (payload.Issuer != issuer)
            {
                error = $"Firebase {this.shortName} has incorrect issuer (iss) claim. Expected "
                    + $"{issuer} but got {payload.Issuer}.  {projectIdMessage} {verifyTokenMessage}";
            }
            else if (payload.IssuedAtTimeSeconds - ClockSkewSeconds > currentTimeInSeconds)
            {
                error = $"Firebase {this.shortName} issued at future timestamp "
                    + $"{payload.IssuedAtTimeSeconds}. Expected to be less than "
                    + $"{currentTimeInSeconds}.";
            }
            else if (payload.ExpirationTimeSeconds + ClockSkewSeconds < currentTimeInSeconds)
            {
                error = $"Firebase {this.shortName} expired at {payload.ExpirationTimeSeconds}. "
                    + $"Expected to be greater than {currentTimeInSeconds}.";
                errorCode = this.expiredIdTokenCode;
            }
            else if (string.IsNullOrEmpty(payload.Subject))
            {
                error = $"Firebase {this.shortName} has no or empty subject (sub) claim.";
            }
            else if (payload.Subject.Length > 128)
            {
                error = $"Firebase {this.shortName} has a subject claim longer than 128 characters.";
            }

            if (error != null)
            {
                throw this.CreateException(error, errorCode);
            }

            await this.VerifySignatureAsync(segments, header.KeyId, cancellationToken)
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
        [SuppressMessage(
            "StyleCop.Analyzers",
            "SA1009:ClosingParenthesisMustBeSpacedCorrectly",
            Justification = "Use of directives.")]
        [SuppressMessage(
            "StyleCop.Analyzers",
            "SA1111:ClosingParenthesisMustBeOnLineOfLastParameter",
            Justification = "Use of directives.")]
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
            var keys = await this.keySource.GetPublicKeysAsync(cancellationToken)
                .ConfigureAwait(false);
            var verified = keys.Any(key =>
#if NETSTANDARD1_5 || NETSTANDARD2_0
                key.Id == keyId && key.RSA.VerifyHash(
                    hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
#elif NET45
                key.Id == keyId &&
                    ((RSACryptoServiceProvider)key.RSA).VerifyHash(hash, Sha256Oid, signature)
#else
#error Unsupported target
#endif
            );
            if (!verified)
            {
                throw this.CreateException($"Failed to verify {this.shortName} signature.");
            }
        }

        private FirebaseAuthException CreateException(
            string message, AuthErrorCode? errorCode = null)
        {
            if (errorCode == null)
            {
                errorCode = this.invalidTokenCode;
            }

            return new FirebaseAuthException(ErrorCode.InvalidArgument, message, errorCode);
        }
    }
}
