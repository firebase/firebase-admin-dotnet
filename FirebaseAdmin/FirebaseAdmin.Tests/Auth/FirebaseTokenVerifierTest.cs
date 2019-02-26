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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Tests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseTokenVerifierTest : IDisposable
    {
        private const long ClockSkewSeconds = 5 * 60;

        private static readonly IPublicKeySource KeySource = new FileSystemPublicKeySource(
            "./resources/public_cert.pem");

        private static readonly IClock Clock = new MockClock();

        private static readonly ISigner Signer = CreateTestSigner();

        private static readonly FirebaseTokenVerifier TokenVerifier = new FirebaseTokenVerifier(
            new FirebaseTokenVerifierArgs()
        {
            ProjectId = "test-project",
            ShortName = "ID token",
            Operation = "VerifyIdTokenAsync()",
            Url = "https://firebase.google.com/docs/auth/admin/verify-id-tokens",
            Issuer = "https://securetoken.google.com/",
            Clock = Clock,
            PublicKeySource = KeySource,
        });

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public async Task ValidToken()
        {
            var payload = new Dictionary<string, object>()
            {
                { "foo", "bar" },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var decoded = await TokenVerifier.VerifyTokenAsync(idToken);
            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal("test-project", decoded.Audience);
            Assert.Equal("testuser", decoded.Subject);

            // The default test token created by CreateTestTokenAsync has an issue time 10 minutes
            // ago, and an expiry time 50 minutes in the future.
            Assert.Equal(Clock.UnixTimestamp() - (60 * 10), decoded.IssuedAtTimeSeconds);
            Assert.Equal(Clock.UnixTimestamp() + (60 * 50), decoded.ExpirationTimeSeconds);
            Assert.Single(decoded.Claims);
            object value;
            Assert.True(decoded.Claims.TryGetValue("foo", out value));
            Assert.Equal("bar", value);
        }

        [Fact]
        public async Task InvalidArgument()
        {
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await TokenVerifier.VerifyTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await TokenVerifier.VerifyTokenAsync(string.Empty));
        }

        [Fact]
        public async Task MalformedToken()
        {
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync("not-a-token"));
        }

        [Fact]
        public async Task NoKid()
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task IncorrectKid()
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", "incorrect-key-id" },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task IncorrectAlgorithm()
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "HS256" },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task Expired()
        {
            var payload = new Dictionary<string, object>()
            {
                { "exp", Clock.UnixTimestamp() - (ClockSkewSeconds + 1) },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task ExpiryTimeInAcceptableRange()
        {
            var expiryTimeSeconds = Clock.UnixTimestamp() - ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTimeSeconds },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);

            var decoded = await TokenVerifier.VerifyTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(expiryTimeSeconds, decoded.ExpirationTimeSeconds);
        }

        [Fact]
        public async Task InvalidIssuedAt()
        {
            var payload = new Dictionary<string, object>()
            {
                { "iat", Clock.UnixTimestamp() + (ClockSkewSeconds + 1) },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task IssuedAtInAcceptableRange()
        {
            var issuedAtSeconds = Clock.UnixTimestamp() + ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAtSeconds },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);

            var decoded = await TokenVerifier.VerifyTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(issuedAtSeconds, decoded.IssuedAtTimeSeconds);
        }

        [Fact]
        public async Task InvalidIssuer()
        {
            var payload = new Dictionary<string, object>()
            {
                { "iss", "wrong-issuer" },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task InvalidAudience()
        {
            var payload = new Dictionary<string, object>()
            {
                { "aud", "wrong-audience" },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task EmptySubject()
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", string.Empty },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public async Task LongSubject()
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", new string('a', 129) },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await TokenVerifier.VerifyTokenAsync(idToken));
        }

        [Fact]
        public void ProjectIdFromOptions()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                ProjectId = "explicit-project-id",
            });
            var verifier = FirebaseTokenVerifier.CreateIDTokenVerifier(app);
            Assert.Equal("explicit-project-id", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromServiceAccount()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            });
            var verifier = FirebaseTokenVerifier.CreateIDTokenVerifier(app);
            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromEnvironment()
        {
            Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", "env-project-id");
            try
            {
                var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = MockCredential,
                });
                var verifier = FirebaseTokenVerifier.CreateIDTokenVerifier(app);
                Assert.Equal("env-project-id", verifier.ProjectId);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", string.Empty);
            }
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        /// <summary>
        /// Creates a mock ID token for testing purposes. By default the created token has an issue
        /// time 10 minutes ago, and an expirty time 50 minutes into the future. All header and
        /// payload claims can be overridden if needed.
        /// </summary>
        internal static async Task<string> CreateTestTokenAsync(
            Dictionary<string, object> headerOverrides = null,
            Dictionary<string, object> payloadOverrides = null)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "RS256" },
                { "typ", "jwt" },
                { "kid", "test-key-id" },
            };
            if (headerOverrides != null)
            {
                foreach (var entry in headerOverrides)
                {
                    header[entry.Key] = entry.Value;
                }
            }

            var payload = new Dictionary<string, object>()
            {
                { "sub", "testuser" },
                { "iss", "https://securetoken.google.com/test-project" },
                { "aud", "test-project" },
                { "iat", Clock.UnixTimestamp() - (60 * 10) },
                { "exp", Clock.UnixTimestamp() + (60 * 50) },
            };
            if (payloadOverrides != null)
            {
                foreach (var entry in payloadOverrides)
                {
                    payload[entry.Key] = entry.Value;
                }
            }

            return await JwtUtils.CreateSignedJwtAsync(header, payload, Signer);
        }

        private static ISigner CreateTestSigner()
        {
            var credential = GoogleCredential.FromFile("./resources/service_account.json");
            var serviceAccount = (ServiceAccountCredential)credential.UnderlyingCredential;
            return new ServiceAccountSigner(serviceAccount);
        }
    }

    internal class FileSystemPublicKeySource : IPublicKeySource
    {
        private IReadOnlyList<PublicKey> rsa;

        public FileSystemPublicKeySource(string file)
        {
            var x509cert = new X509Certificate2(File.ReadAllBytes(file));
            var rsa = (RSA)x509cert.PublicKey.Key;
            this.rsa = ImmutableList.Create(new PublicKey("test-key-id", rsa));
        }

        public Task<IReadOnlyList<PublicKey>> GetPublicKeysAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult(this.rsa);
        }
    }
}
