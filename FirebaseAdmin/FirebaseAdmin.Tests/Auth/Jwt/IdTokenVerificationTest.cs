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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class IdTokenVerificationTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { FirebaseAuthTestConfig.Instance },
            new object[] { TenantAwareFirebaseAuthTestConfig.Instance },
        };

        private const long ClockSkewSeconds = 5 * 60;

        private const string ProjectId = "test-project";

        private static readonly IClock Clock = new MockClock();

        private static readonly TestOptions WithIdTokenVerifier =
            new TestOptions { IdTokenVerifier = true };

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidToken(AuthTestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            config.AssertFirebaseToken(decoded);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidTokenWithClaims(AuthTestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "foo", "bar" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            config.AssertFirebaseToken(decoded, payload);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidArgument(AuthTestConfig config)
        {
            var auth = config.CreateAuth(WithIdTokenVerifier);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifyIdTokenAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task MalformedToken(AuthTestConfig config)
        {
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync("not-a-token"));

            this.CheckException(exception, "Incorrect number of segments in ID token.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task NoKid(AuthTestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Firebase ID token has no 'kid' claim.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectKid(AuthTestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", "incorrect-key-id" },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            this.CheckException(exception, "Failed to verify ID token signature.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectAlgorithm(AuthTestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "HS256" },
            };
            var idToken = await config.CreateIdTokenAsync(headerOverrides: header);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect algorithm."
                + " Expected RS256 but got HS256.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task Expired(AuthTestConfig config)
        {
            var expiryTime = Clock.UnixTimestamp() - (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTime },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token expired at {expiryTime}. "
                + $"Expected to be greater than {Clock.UnixTimestamp()}.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.ExpiredIdToken);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ExpiryTimeInAcceptableRange(AuthTestConfig config)
        {
            var expiryTimeSeconds = Clock.UnixTimestamp() - ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTimeSeconds },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(expiryTimeSeconds, decoded.ExpirationTimeSeconds);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuedAt(AuthTestConfig config)
        {
            var issuedAt = Clock.UnixTimestamp() + (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAt },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = $"Firebase ID token issued at future timestamp {issuedAt}.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IssuedAtInAcceptableRange(AuthTestConfig config)
        {
            var issuedAtSeconds = Clock.UnixTimestamp() + ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAtSeconds },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var decoded = await auth.VerifyIdTokenAsync(idToken);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(issuedAtSeconds, decoded.IssuedAtTimeSeconds);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuer(AuthTestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "iss", "wrong-issuer" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect issuer (iss) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CustomToken(AuthTestConfig config)
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
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "VerifyIdTokenAsync() expects an ID token, but was given "
                + "a custom token.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidAudience(AuthTestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "aud", "wrong-audience" },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect audience (aud) claim."
                + " Expected test-project but got wrong-audience";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task EmptySubject(AuthTestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", string.Empty },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has no or empty subject (sub) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task LongSubject(AuthTestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "sub", new string('a', 129) },
            };
            var idToken = await config.CreateIdTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth(WithIdTokenVerifier);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has a subject claim longer than"
                + " 128 characters.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task RevokedToken(AuthTestConfig config)
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
            var auth = config.CreateAuth(WithIdTokenVerifierAndUserManager(handler));

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
        public async Task ValidUnrevokedToken(AuthTestConfig config)
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
            var auth = config.CreateAuth(WithIdTokenVerifierAndUserManager(handler));

            var decoded = await auth.VerifyIdTokenAsync(idToken, true);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(1, handler.Calls);
            config.AssertRequest("accounts:lookup", handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CheckRevokedError(AuthTestConfig config)
        {
            var idToken = await config.CreateIdTokenAsync();
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = config.CreateAuth(WithIdTokenVerifierAndUserManager(handler));

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
        public async Task VerifyIdTokenCancel(AuthTestConfig config)
        {
            var auth = config.CreateAuth(WithIdTokenVerifier);
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var idToken = await config.CreateIdTokenAsync();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => auth.VerifyIdTokenAsync(idToken, canceller.Token));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task TenantIdMismatch(AuthTestConfig config)
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
            var auth = config.CreateAuth(WithIdTokenVerifier);

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
                { "iss", $"https://securetoken.google.com/{ProjectId}" },
                { "aud", ProjectId },
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

            return await JwtUtils.CreateSignedJwtAsync(
                header, payload, JwtTestUtils.DefaultSigner);
        }

        private static TestOptions WithIdTokenVerifierAndUserManager(MockMessageHandler handler)
        {
            return new TestOptions
            {
                IdTokenVerifier = true,
                UserManagerHandler = handler,
            };
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

        private sealed class FirebaseAuthTestConfig
        : AuthTestConfig.AbstractFirebaseAuthTestConfig
        {
            internal static readonly FirebaseAuthTestConfig Instance =
                new FirebaseAuthTestConfig();

            private protected override Context Init()
            {
                return new Context
                {
                    ProjectId = ProjectId,
                    Clock = Clock,
                    KeySource = JwtTestUtils.DefaultKeySource,
                    Signer = JwtTestUtils.DefaultSigner,
                };
            }
        }

        private sealed class TenantAwareFirebaseAuthTestConfig
        : AuthTestConfig.AbstractTenantAwareFirebaseAuthTestConfig
        {
            internal static readonly TenantAwareFirebaseAuthTestConfig Instance =
                new TenantAwareFirebaseAuthTestConfig();

            private protected override Context Init()
            {
                return new Context
                {
                    ProjectId = ProjectId,
                    Clock = Clock,
                    KeySource = JwtTestUtils.DefaultKeySource,
                    Signer = JwtTestUtils.DefaultSigner,
                    TenantId = "test-tenant",
                };
            }
        }
    }
}
