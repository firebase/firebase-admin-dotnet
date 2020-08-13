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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class IdTokenVerificationTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { new TestConfig() },
            new object[] { new TestConfig("test-tenant") },
        };

        private const long ClockSkewSeconds = 5 * 60;

        private const string ProjectId = "test-project";

        private static readonly IClock Clock = new MockClock();

        private static readonly TestOptions WithIdTokenVerifier =
            new TestOptions { IdTokenVerifier = true };

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidToken(TestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
            var auth = config.CreateAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            config.AssertFirebaseToken(decoded);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidTokenWithClaims(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "foo", "bar" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            config.AssertFirebaseToken(decoded, payload);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidArgument(TestConfig config)
        {
            var auth = config.CreateAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task MalformedToken(TestConfig config)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync("not-a-token"));

            this.CheckException(exception, "Incorrect number of segments in ID token.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task NoKid(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Firebase ID token has no 'kid' claim.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectKid(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", "incorrect-key-id" },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Failed to verify ID token signature.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectAlgorithm(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "HS256" },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect algorithm."
                + " Expected RS256 but got HS256.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task Expired(TestConfig config)
        {
            var expiryTime = Clock.UnixTimestamp() - (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTime },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token expired at {expiryTime}. "
                + $"Expected to be greater than {Clock.UnixTimestamp()}.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.ExpiredIdToken);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ExpiryTimeInAcceptableRange(TestConfig config)
        {
            var expiryTimeSeconds = Clock.UnixTimestamp() - ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTimeSeconds },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(expiryTimeSeconds, decoded.ExpirationTimeSeconds);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuedAt(TestConfig config)
        {
            var issuedAt = Clock.UnixTimestamp() + (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAt },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token issued at future timestamp {issuedAt}.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IssuedAtInAcceptableRange(TestConfig config)
        {
            var issuedAtSeconds = Clock.UnixTimestamp() + ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAtSeconds },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(issuedAtSeconds, decoded.IssuedAtTimeSeconds);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuer(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "iss", "wrong-issuer" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect issuer (iss) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CustomToken(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var payload = new Dictionary<string, object>()
            {
                { "aud", FirebaseTokenFactory.FirebaseAudience },
            };
            var idToken = await config.CreateIdTokenAsync(header, payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "VerifyIdTokenAsync() expects an ID token, but was given "
                + "a custom token.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidAudience(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "aud", "wrong-audience" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect audience (aud) claim."
                + " Expected test-project but got wrong-audience";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task EmptySubject(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", string.Empty },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has no or empty subject (sub) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task LongSubject(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", new string('a', 129) },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has a subject claim longer than"
                + " 128 characters.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task RevokedToken(TestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
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
            var auth = config.CreateAuth(handler);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(0, handler.Calls);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken, true));

            var expectedMessage = "Firebase ID token has been revoked.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.RevokedIdToken);
            Assert.Equal(1, handler.Calls);
            config.AssertRequest("accounts:lookup", handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidUnrevokedToken(TestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
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
            var auth = config.CreateAuth(handler);

            var decoded = await auth.VerifyIdTokenAsync(idToken, true);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(1, handler.Calls);
            config.AssertRequest("accounts:lookup", handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CheckRevokedError(TestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken, true));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.StartsWith("No user record found for the given identifier", exception.Message);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);
            Assert.Equal(1, handler.Calls);
            config.AssertRequest("accounts:lookup", handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task VerifyIdTokenCancel(TestConfig config)
        {
            var auth = config.CreateAuth();
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var idToken = await config.CreateIdTokenAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => auth.VerifyIdTokenAsync(idToken, canceller.Token));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task TenantIdMismatch(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                {
                    "firebase", new Dictionary<string, object>
                    {
                        { "tenant", "other-tenant" },
                    }
                },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect tenant ID.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.TenantIdMismatch);
        }

        // TODO(hkj): Remove the following method once the session cookie tests have been
        // refactored.

        /// <summary>
        /// Creates a mock ID token for testing purposes. By default the created token has an issue
        /// time 10 minutes ago, and an expirty time 50 minutes into the future. All header and
        /// payload claims can be overridden if needed.
        /// </summary>
        internal static async Task<string> CreateTestTokenAsync(
            Dictionary<string, object> headerOverrides = null,
            Dictionary<string, object> payloadOverrides = null)
        {
            var tokenBuilder = new MockTokenBuilder
                {
                    ProjectId = ProjectId,
                    Clock = Clock,
                    Signer = JwtTestUtils.DefaultSigner,
                    IssuerPrefix = "https://securetoken.google.com",
                    Uid = "testuser",
                };
            return await tokenBuilder.CreateTokenAsync(
                headerOverrides, payloadOverrides);
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

        public class TestConfig
        {
            private readonly string tenantId;

            public TestConfig(string tenantId = null)
            {
                this.tenantId = tenantId;
            }

            private AuthBuilder AuthBuilder => new AuthBuilder
                {
                    ProjectId = ProjectId,
                    Clock = Clock,
                    KeySource = JwtTestUtils.DefaultKeySource,
                    RetryOptions = RetryOptions.NoBackOff,
                    TenantId = tenantId,
                };

            private MockTokenBuilder TokenBuilder => new MockTokenBuilder
                {
                    ProjectId = ProjectId,
                    Clock = Clock,
                    Signer = JwtTestUtils.DefaultSigner,
                    IssuerPrefix = "https://securetoken.google.com",
                    Uid = "testuser",
                    TenantId = this.tenantId,
                };

            public AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler = null)
            {
                var options = new TestOptions
                {
                    UserManagerRequestHandler = handler,
                    IdTokenVerifier = true,
                };
                return this.AuthBuilder.Build(options);
            }

            public async Task<string> CreateIdTokenAsync(
                IDictionary<string, object> headerOverrides = null,
                IDictionary<string, object> payloadOverrides = null)
            {
                return await this.TokenBuilder.CreateTokenAsync(headerOverrides, payloadOverrides);
            }

            public void AssertFirebaseToken(
                FirebaseToken token, IDictionary<string, object> expectedClaims = null)
            {
                this.TokenBuilder.AssertFirebaseToken(token, expectedClaims);
            }

            internal void AssertRequest(
                string expectedSuffix, MockMessageHandler.IncomingRequest request)
            {
                Assert.Equal(
                    this.AuthBuilder.BuildRequestPath("v1", expectedSuffix),
                    request.Url.PathAndQuery);
            }
        }
    }
}
