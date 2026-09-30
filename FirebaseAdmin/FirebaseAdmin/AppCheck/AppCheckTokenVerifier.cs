// Copyright 2026, Google Inc. All rights reserved.
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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Verifies Firebase App Check tokens, and optionally consumes them for replay protection.
    /// </summary>
    internal sealed class AppCheckTokenVerifier
    {
        private const string AppCheckIssuer = "https://firebaseappcheck.googleapis.com/";
        private const long ClockSkewSeconds = 60;

        private static readonly long MaxUnixSeconds = DateTimeOffset.MaxValue.ToUnixTimeSeconds();

        private readonly string projectId;
        private readonly AppCheckPublicKeySource keySource;
        private readonly AppCheckClient client;
        private readonly IClock clock;

        internal AppCheckTokenVerifier(
            string projectId, AppCheckPublicKeySource keySource, AppCheckClient client, IClock clock)
        {
            this.projectId = projectId.ThrowIfNullOrEmpty(nameof(projectId));
            this.keySource = keySource.ThrowIfNull(nameof(keySource));
            this.client = client.ThrowIfNull(nameof(client));
            this.clock = clock.ThrowIfNull(nameof(clock));
        }

        internal async Task<VerifyAppCheckTokenResponse> VerifyTokenAsync(
            string token,
            VerifyAppCheckTokenOptions options = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("App Check token must not be null or empty.");
            }

            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw CreateException("Incorrect number of segments in App Check token.");
            }

            JsonWebSignature.Header header;
            DecodedAppCheckToken.Args payload;
            Dictionary<string, object> claims;
            byte[] signature;
            try
            {
                header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
                var payloadJson = JwtUtils.Base64Decode(segments[1]);
                var serializer = NewtonsoftJsonSerializer.Instance;
                payload = serializer.Deserialize<DecodedAppCheckToken.Args>(payloadJson);
                claims = serializer.Deserialize<Dictionary<string, object>>(payloadJson);
                signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            }
            catch (Exception e) when (e is FormatException || e is JsonException)
            {
                throw CreateException("Failed to decode App Check token.", inner: e);
            }

            if (header == null || payload == null || claims == null)
            {
                throw CreateException("Failed to decode App Check token.");
            }

            payload.Claims = claims.ToImmutableDictionary();

            this.VerifyHeaderAndClaims(header, payload);
            await this.VerifySignatureAsync(segments, header.KeyId, signature, cancellationToken)
                .ConfigureAwait(false);

            bool? alreadyConsumed = null;
            if (options?.Consume == true)
            {
                alreadyConsumed = await this.client
                    .VerifyReplayProtectionAsync(token, cancellationToken)
                    .ConfigureAwait(false);
            }

            return new VerifyAppCheckTokenResponse(
                payload.Subject, new DecodedAppCheckToken(payload), alreadyConsumed);
        }

        private static FirebaseAppCheckException CreateException(
            string message,
            AppCheckErrorCode appCheckCode = AppCheckErrorCode.InvalidAppCheckToken,
            Exception inner = null)
        {
            return new FirebaseAppCheckException(
                ErrorCode.InvalidArgument, message, appCheckCode, inner);
        }

        private void VerifyHeaderAndClaims(
            JsonWebSignature.Header header, DecodedAppCheckToken.Args payload)
        {
            var expectedAudience = $"projects/{this.projectId}";
            var now = this.clock.UnixTimestamp();
            string error = null;
            var errorCode = AppCheckErrorCode.InvalidAppCheckToken;

            if (header.Algorithm != JwtUtils.AlgorithmRS256)
            {
                error = "App Check token has incorrect algorithm. Expected RS256 but got "
                    + $"{header.Algorithm}.";
            }
            else if (string.IsNullOrEmpty(header.KeyId))
            {
                error = "App Check token has no 'kid' header.";
            }
            else if (!string.Equals(header.Type, "JWT", StringComparison.OrdinalIgnoreCase))
            {
                error = "App Check token has incorrect type header. Expected JWT but got "
                    + $"{header.Type}.";
            }
            else if (payload.Issuer == null
                || !payload.Issuer.StartsWith(AppCheckIssuer, StringComparison.Ordinal))
            {
                error = "App Check token has incorrect issuer (iss) claim. Expected it to start "
                    + $"with {AppCheckIssuer} but got {payload.Issuer}.";
            }
            else if (payload.Audience == null || !payload.Audience.Contains(expectedAudience))
            {
                var audience = payload.Audience == null ? null : string.Join(", ", payload.Audience);
                error = "App Check token has incorrect audience (aud) claim. Expected "
                    + $"{expectedAudience} but got {audience}.";
            }
            else if (string.IsNullOrEmpty(payload.Subject))
            {
                error = "App Check token has no or empty subject (sub) claim.";
            }
            else if (payload.IssuedAtTimeSeconds < 0 || payload.IssuedAtTimeSeconds > MaxUnixSeconds)
            {
                error = "App Check token has invalid issued-at (iat) claim: "
                    + $"{payload.IssuedAtTimeSeconds}.";
            }
            else if (payload.ExpirationTimeSeconds < 0 || payload.ExpirationTimeSeconds > MaxUnixSeconds)
            {
                error = "App Check token has invalid expiration (exp) claim: "
                    + $"{payload.ExpirationTimeSeconds}.";
            }
            else if (payload.IssuedAtTimeSeconds - ClockSkewSeconds > now)
            {
                error = $"App Check token issued at future timestamp {payload.IssuedAtTimeSeconds}. "
                    + $"Expected to be less than {now}.";
            }
            else if (payload.ExpirationTimeSeconds + ClockSkewSeconds < now)
            {
                error = $"App Check token expired at {payload.ExpirationTimeSeconds}. Expected to "
                    + $"be greater than {now}.";
                errorCode = AppCheckErrorCode.AppCheckTokenExpired;
            }

            if (error != null)
            {
                throw CreateException(error, errorCode);
            }
        }

        private async Task VerifySignatureAsync(
            string[] segments, string keyId, byte[] signature, CancellationToken cancellationToken)
        {
            var key = await this.keySource.GetPublicKeyAsync(keyId, cancellationToken)
                .ConfigureAwait(false);

            byte[] hash;
            using (var hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"));
            }

            if (key == null || !key.RSA.VerifyHash(
                hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
            {
                throw CreateException("Failed to verify App Check token signature.");
            }
        }
    }
}
