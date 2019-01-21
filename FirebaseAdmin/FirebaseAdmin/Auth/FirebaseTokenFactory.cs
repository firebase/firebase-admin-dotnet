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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Google.Apis.Auth;
using Google.Apis.Http;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;

[assembly: InternalsVisibleToAttribute("FirebaseAdmin.Tests,PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010081328559eaab41" +
"055b84af73469863499d81625dcbba8d8decb298b69e0f783a0958cf471fd4f76327b85a7d4b02" +
"3003684e85e61cf15f13150008c81f0b75a252673028e530ea95d0c581378da8c6846526ab9597" +
"4c6d0bc66d2462b51af69968a0e25114bde8811e0d6ee1dc22d4a59eee6a8bba4712cba839652f" +
"badddb9c")]
namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A helper class that creates Firebase custom tokens.
    /// </summary>
    internal class FirebaseTokenFactory : IDisposable
    {
        public const string FirebaseAudience = "https://identitytoolkit.googleapis.com/"
            + "google.identity.identitytoolkit.v1.IdentityToolkit";

        public const int TokenDurationSeconds = 3600;
        public static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static readonly ImmutableList<string> ReservedClaims = ImmutableList.Create(
            "acr",
            "amr",
            "at_hash",
            "aud",
            "auth_time",
            "azp",
            "cnf",
            "c_hash",
            "exp",
            "firebase",
            "iat",
            "iss",
            "jti",
            "nbf",
            "nonce",
            "sub");

        private readonly ISigner _signer;
        private readonly IClock _clock;

        public FirebaseTokenFactory(ISigner signer, IClock clock)
        {
            _signer = signer.ThrowIfNull(nameof(signer));
            _clock = clock.ThrowIfNull(nameof(clock));
        }

        public static FirebaseTokenFactory Create(FirebaseApp app)
        {
            ISigner signer = null;
            var serviceAccount = app.Options.Credential.ToServiceAccountCredential();
            if (serviceAccount != null)
            {
                // If the app was initialized with a service account, use it to sign
                // tokens locally.
                signer = new ServiceAccountSigner(serviceAccount);
            }
            else if (string.IsNullOrEmpty(app.Options.ServiceAccountId))
            {
                // If no service account ID is specified, attempt to discover one and invoke the
                // IAM service with it.
                signer = new IAMSigner(new HttpClientFactory(), app.Options.Credential);
            }
            else
            {
                // If a service account ID is specified, invoke the IAM service with it.
                signer = new FixedAccountIAMSigner(
                    new HttpClientFactory(), app.Options.Credential, app.Options.ServiceAccountId);
            }
            return new FirebaseTokenFactory(signer, SystemClock.Default);
        }

        public async Task<string> CreateCustomTokenAsync(
            string uid,
            IDictionary<string, object> developerClaims = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("uid must not be null or empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("uid must not be longer than 128 characters");
            }
            if (developerClaims != null)
            {
                foreach (var entry in developerClaims)
                {
                    if (ReservedClaims.Contains(entry.Key))
                    {
                        throw new ArgumentException(
                            $"reserved claim {entry.Key} not allowed in developerClaims");
                    }
                }
            }

            var header = new JsonWebSignature.Header()
            {
                Algorithm = "RS256",
                Type = "JWT",
            };

            var issued = (int)(_clock.UtcNow - UnixEpoch).TotalSeconds;
            var keyId = await _signer.GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var payload = new CustomTokenPayload()
            {
                Uid = uid,
                Issuer = keyId,
                Subject = keyId,
                Audience = FirebaseAudience,
                IssuedAtTimeSeconds = issued,
                ExpirationTimeSeconds = issued + TokenDurationSeconds,
            };
            if (developerClaims != null && developerClaims.Count > 0)
            {
                payload.Claims = developerClaims;
            }
            return await JwtUtils.CreateSignedJwtAsync(
                header, payload, _signer, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose()
        {
            _signer.Dispose();
        }
    }

    internal class CustomTokenPayload : JsonWebToken.Payload
    {
        [Newtonsoft.Json.JsonPropertyAttribute("uid")]
        public string Uid { get; set; }

        [Newtonsoft.Json.JsonPropertyAttribute("claims")]
        public IDictionary<string, object> Claims { get; set; }
    }
}
