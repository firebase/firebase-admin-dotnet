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
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class SessionCookieVerificationTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { TestConfig.ForFirebaseAuth() },
            // TODO(hkj): Add tenant-aware tests when the support is available.
        };

        private const long ClockSkewSeconds = 5 * 60;

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidSessionCookie(TestConfig config)
        {
            var cookie = await config.CreateSessionCookieAsync();
            var auth = config.CreateAuth();

            var decoded = await auth.VerifySessionCookieAsync(cookie);

            config.AssertFirebaseToken(decoded);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidSessionCookieWithClaims(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "foo", "bar" },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifySessionCookieAsync(cookie);

            config.AssertFirebaseToken(decoded, payload);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidArgument(TestConfig config)
        {
            var auth = config.CreateAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifySessionCookieAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.VerifySessionCookieAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task MalformedCookie(TestConfig config)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync("not-a-token"));

            this.CheckException(exception, "Incorrect number of segments in session cookie.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task NoKid(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", string.Empty },
            };
            var cookie = await config.CreateSessionCookieAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            this.CheckException(exception, "Firebase session cookie has no 'kid' claim.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectKid(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "kid", "incorrect-key-id" },
            };
            var cookie = await config.CreateSessionCookieAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            this.CheckException(exception, "Failed to verify session cookie signature.");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IncorrectAlgorithm(TestConfig config)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "HS256" },
            };
            var cookie = await config.CreateSessionCookieAsync(headerOverrides: header);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie has incorrect algorithm."
                + " Expected RS256 but got HS256.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task Expired(TestConfig config)
        {
            var expiryTime = JwtTestUtils.Clock.UnixTimestamp() - (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTime },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = $"Firebase session cookie expired at {expiryTime}. "
                + $"Expected to be greater than {JwtTestUtils.Clock.UnixTimestamp()}.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.ExpiredSessionCookie);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ExpiryTimeInAcceptableRange(TestConfig config)
        {
            var expiryTimeSeconds = JwtTestUtils.Clock.UnixTimestamp() - ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "exp", expiryTimeSeconds },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifySessionCookieAsync(cookie);

            config.AssertFirebaseToken(decoded, payload);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuedAt(TestConfig config)
        {
            var issuedAt = JwtTestUtils.Clock.UnixTimestamp() + (ClockSkewSeconds + 1);
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAt },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie issued at future "
                + $"timestamp {issuedAt}.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IssuedAtInAcceptableRange(TestConfig config)
        {
            var issuedAtSeconds = JwtTestUtils.Clock.UnixTimestamp() + ClockSkewSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "iat", issuedAtSeconds },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var decoded = await auth.VerifySessionCookieAsync(cookie);

            config.AssertFirebaseToken(decoded, payload);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task InvalidIssuer(TestConfig config)
        {
            var payload = new Dictionary<string, object>()
            {
                { "iss", "wrong-issuer" },
            };
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie has incorrect issuer (iss) claim.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task IdToken(TestConfig config)
        {
            var tokenBuilder = JwtTestUtils.IdTokenBuilder();
            var idToken = await tokenBuilder.CreateTokenAsync();
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(idToken));

            var expectedMessage = "Firebase session cookie has incorrect issuer (iss) claim.";
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
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie has incorrect audience (aud) claim."
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
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie has no or empty subject (sub) claim.";
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
            var cookie = await config.CreateSessionCookieAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie));

            var expectedMessage = "Firebase session cookie has a subject claim longer than"
                + " 128 characters.";
            this.CheckException(exception, expectedMessage);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task RevokedToken(TestConfig config)
        {
            var cookie = await config.CreateSessionCookieAsync();
            var handler = new MockMessageHandler()
            {
                Response = $@"{{
                    ""users"": [
                        {{
                            ""localId"": ""testuser"",
                            ""validSince"": {JwtTestUtils.Clock.UnixTimestamp()}
                        }}
                    ]
                }}",
            };
            var auth = config.CreateAuth(handler);

            var decoded = await auth.VerifySessionCookieAsync(cookie);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(0, handler.Calls);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie, true));

            var expectedMessage = "Firebase session cookie has been revoked.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.RevokedSessionCookie);
            Assert.Equal(1, handler.Calls);
            JwtTestUtils.AssertRevocationCheckRequest(null, handler.Requests[0].Url);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidUnrevokedToken(TestConfig config)
        {
            var cookie = await config.CreateSessionCookieAsync();
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

            var decoded = await auth.VerifySessionCookieAsync(cookie, true);

            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(1, handler.Calls);
            JwtTestUtils.AssertRevocationCheckRequest(null, handler.Requests[0].Url);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CheckRevokedError(TestConfig config)
        {
            var cookie = await config.CreateSessionCookieAsync();
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifySessionCookieAsync(cookie, true));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.StartsWith("No user record found for the given identifier", exception.Message);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);
            Assert.Equal(1, handler.Calls);
            JwtTestUtils.AssertRevocationCheckRequest(null, handler.Requests[0].Url);
        }

        private void CheckException(
            FirebaseAuthException exception,
            string prefix,
            AuthErrorCode errorCode = AuthErrorCode.InvalidSessionCookie)
        {
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.StartsWith(prefix, exception.Message);
            Assert.Equal(errorCode, exception.AuthErrorCode);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }

        public class TestConfig
        {
            private readonly AuthBuilder authBuilder;
            private readonly MockTokenBuilder tokenBuilder;

            private TestConfig()
            {
                this.authBuilder = JwtTestUtils.AuthBuilderForTokenVerification();
                this.tokenBuilder = JwtTestUtils.SessionCookieBuilder();
            }

            public static TestConfig ForFirebaseAuth()
            {
                return new TestConfig();
            }

            public FirebaseAuth CreateAuth(HttpMessageHandler handler = null)
            {
                var options = new TestOptions
                {
                    UserManagerRequestHandler = handler,
                    SessionCookieVerifier = true,
                };
                return (FirebaseAuth)this.authBuilder.Build(options);
            }

            public async Task<string> CreateSessionCookieAsync(
                IDictionary<string, object> headerOverrides = null,
                IDictionary<string, object> payloadOverrides = null)
            {
                return await this.tokenBuilder.CreateTokenAsync(headerOverrides, payloadOverrides);
            }

            public void AssertFirebaseToken(
                FirebaseToken token, IDictionary<string, object> expectedClaims = null)
            {
                this.tokenBuilder.AssertFirebaseToken(token, expectedClaims);
            }
        }
    }
}
