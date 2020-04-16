// Copyright 2020, Google Inc. All rights reserved.
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
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class IdTokenVerificationTest
    {
        private const long ClockSkewSeconds = 5 * 60;

        private static readonly IPublicKeySource KeySource = new FileSystemPublicKeySource(
            "./resources/public_cert.pem");

        private static readonly IClock Clock = new MockClock();

        private static readonly ISigner Signer = CreateTestSigner();

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
            var auth = this.CreateFirebaseAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

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
            var auth = this.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(string.Empty));
        }

        [Fact]
        public async Task MalformedToken()
        {
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync("not-a-token"));

            this.CheckException(exception, "Incorrect number of segments in ID token.");
        }

        [Fact]
        public async Task NoKid()
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Firebase ID token has no 'kid' claim.");
        }

        [Fact]
        public async Task IncorrectKid()
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", "incorrect-key-id" },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Failed to verify ID token signature.");
        }

        [Fact]
        public async Task IncorrectAlgorithm()
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "HS256" },
            };
            var idToken = await CreateTestTokenAsync(headerOverrides: header);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect algorithm."
                + " Expected RS256 but got HS256.";
            this.CheckException(exception, expectedMessage);
        }

        [Fact]
        public async Task Expired()
        {
            var expiryTime = Clock.UnixTimestamp() - (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTime },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token expired at {expiryTime}. "
                + $"Expected to be greater than {Clock.UnixTimestamp()}.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.ExpiredIdToken);
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
            var auth = this.CreateFirebaseAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(expiryTimeSeconds, decoded.ExpirationTimeSeconds);
        }

        [Fact]
        public async Task InvalidIssuedAt()
        {
            var issuedAt = Clock.UnixTimestamp() + (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAt },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token issued at future timestamp {issuedAt}.";
            this.CheckException(exception, expectedMessage);
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
            var auth = this.CreateFirebaseAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

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
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "ID token has incorrect issuer (iss) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Fact]
        public async Task InvalidAudience()
        {
            var payload = new Dictionary<string, object>()
            {
                { "aud", "wrong-audience" },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "ID token has incorrect audience (aud) claim."
                + " Expected test-project but got wrong-audience";
            this.CheckException(exception, expectedMessage);
        }

        [Fact]
        public async Task EmptySubject()
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", string.Empty },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has no or empty subject (sub) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Fact]
        public async Task LongSubject()
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", new string('a', 129) },
            };
            var idToken = await CreateTestTokenAsync(payloadOverrides: payload);
            var auth = this.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has a subject claim longer than"
                + " 128 characters.";
            this.CheckException(exception, expectedMessage);
        }

        [Fact]
        public async Task RevokedToken()
        {
            var idToken = await CreateTestTokenAsync();
            var handler = new MockMessageHandler()
            {
                Response = $@"{{
                    ""users"": [
                        {{
                            ""localId"": ""testuser"",
                            ""validSince"": {Clock.UnixTimestamp()}
                        }}
                    ]
                }}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(0, handler.Calls);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken, true));

            var expectedMessage = "Firebase ID token has been revoked.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.RevokedIdToken);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task ValidUnrevokedToken()
        {
            var idToken = await CreateTestTokenAsync();
            var handler = new MockMessageHandler()
            {
                Response = @"{
                    ""users"": [
                        {
                            ""localId"": ""testuser""
                        }
                    ]
                }",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var decoded = await auth.VerifyIdTokenAsync(idToken, true);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task CheckRevokedError()
        {
            var idToken = await CreateTestTokenAsync();
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken, true));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.StartsWith("No user record found for the given identifier", exception.Message);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);
            Assert.Equal(1, handler.Calls);
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

        private FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler = null)
        {
            var args = FirebaseTokenVerifierArgs.ForIdTokens("test-project", KeySource, Clock);
            var tokenVerifier = new FirebaseTokenVerifier(args);

            FirebaseUserManager userManager = null;
            if (handler != null)
            {
                userManager = new FirebaseUserManager(new FirebaseUserManager.Args
                {
                    Credential = MockCredential,
                    ProjectId = "test-project",
                    ClientFactory = new MockHttpClientFactory(handler),
                    RetryOptions = RetryOptions.NoBackOff,
                });
            }

            return new FirebaseAuth(new FirebaseAuth.FirebaseAuthArgs()
            {
                IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(tokenVerifier),
                UserManager = new Lazy<FirebaseUserManager>(userManager),
                TokenFactory = new Lazy<FirebaseTokenFactory>(),
            });
        }

        private void CheckException(
            FirebaseAuthException exception,
            string prefix,
            AuthErrorCode errorCode = AuthErrorCode.InvalidIdToken)
        {
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.StartsWith(prefix, exception.Message);
            Assert.Equal(errorCode, exception.AuthErrorCode);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HttpResponse);
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
