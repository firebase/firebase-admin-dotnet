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
using FirebaseAdmin.Auth.Multitenancy;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class IdTokenVerificationTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { FirebaseAuthTestConfig.DefaultInstance },
            new object[] { TenantAwareFirebaseAuthTestConfig.DefaultInstance },
        };

        private const long ClockSkewSeconds = 5 * 60;

        private const string ProjectId = "test-project";

        private const string TenantId = "test-tenant";

        private static readonly IPublicKeySource KeySource = new FileSystemPublicKeySource(
            "./resources/public_cert.pem");

        private static readonly IClock Clock = new MockClock();

        private static readonly ISigner Signer = CreateTestSigner();

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidToken(TestConfig config)
        {
            var idToken = await config.CreateTestTokenAsync();
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(headerOverrides: header);
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
            var idToken = await config.CreateTestTokenAsync(headerOverrides: header);
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
            var idToken = await config.CreateTestTokenAsync(headerOverrides: header);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(header, payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
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
            var idToken = await config.CreateTestTokenAsync();
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
            config.AssertRequest(handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ValidUnrevokedToken(TestConfig config)
        {
            var idToken = await config.CreateTestTokenAsync();
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
            config.AssertRequest(handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CheckRevokedError(TestConfig config)
        {
            var idToken = await config.CreateTestTokenAsync();
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
            config.AssertRequest(handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task VerifyIdTokenCancel(TestConfig config)
        {
            var auth = config.CreateAuth();
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var idToken = await config.CreateTestTokenAsync();

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
            var idToken = await config.CreateTestTokenAsync(payloadOverrides: payload);
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.VerifyIdTokenAsync(idToken));

            var expectedMessage = "Firebase ID token has incorrect tenant ID.";
            this.CheckException(exception, expectedMessage, AuthErrorCode.TenantIdMismatch);
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

            return await JwtUtils.CreateSignedJwtAsync(header, payload, Signer);
        }

        internal static ISigner CreateTestSigner()
        {
            var credential = GoogleCredential.FromFile("./resources/service_account.json");
            var serviceAccount = (ServiceAccountCredential)credential.UnderlyingCredential;
            return new ServiceAccountSigner(serviceAccount);
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

        public abstract class TestConfig
        {
            protected abstract string BaseUrl { get; }

            /// <summary>
            /// Creates an instance of <c>AbstractFirebaseAuth</c> for testing. If specified, the
            /// HTTP message handler is used to respond to user management API calls.
            /// </summary>
            internal AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler = null)
            {
                var tokenVerifier = new FirebaseTokenVerifier(this.CreateTokenVerifierArgs());
                var userManager = new FirebaseUserManager(this.CreateUserManagerArgs(handler));
                return this.CreateAuth(tokenVerifier, userManager);
            }

            /// <summary>
            /// Creates an ID token for testing with the given header and payload claims. The
            /// returned ID token is guaranteed to contain the <c>firebase</c> claim.
            /// </summary>
            internal async Task<string> CreateTestTokenAsync(
                Dictionary<string, object> headerOverrides = null,
                Dictionary<string, object> payloadOverrides = null)
            {
                var payloadCopy = payloadOverrides != null ?
                    new Dictionary<string, object>(payloadOverrides)
                    : new Dictionary<string, object>();
                if (!payloadCopy.ContainsKey("firebase"))
                {
                    payloadCopy["firebase"] = this.GetFirebaseClaims();
                }

                return await IdTokenVerificationTest.CreateTestTokenAsync(
                    headerOverrides, payloadCopy);
            }

            /// <summary>
            /// Asserts that the given <c>FirebaseToken</c> is correctly populated, and contains
            /// the expected claims.
            /// </summary>
            internal void AssertFirebaseToken(
                FirebaseToken decoded, IDictionary<string, object> expected = null)
            {
                Assert.Equal(ProjectId, decoded.Audience);
                Assert.Equal("testuser", decoded.Uid);
                Assert.Equal("testuser", decoded.Subject);

                // The default test token created by CreateTestTokenAsync has an issue time 10 minutes
                // ago, and an expiry time 50 minutes in the future.
                Assert.Equal(Clock.UnixTimestamp() - (60 * 10), decoded.IssuedAtTimeSeconds);
                Assert.Equal(Clock.UnixTimestamp() + (60 * 50), decoded.ExpirationTimeSeconds);

                if (expected != null)
                {
                    Assert.Equal(expected.Count + 1, decoded.Claims.Count);
                    Assert.Contains(decoded.Claims, (kvp) => kvp.Key == "firebase");
                    foreach (var entry in expected)
                    {
                        Assert.Equal(entry.Value, decoded.Claims[entry.Key]);
                    }
                }
                else
                {
                    Assert.Equal("firebase", Assert.Single(decoded.Claims).Key);
                }

                this.AssertDecodedToken(decoded);
            }

            internal void AssertRequest(MockMessageHandler.IncomingRequest request)
            {
                Assert.Equal($"{this.BaseUrl}/accounts:lookup", request.Url.PathAndQuery);
            }

            private protected virtual IDictionary<string, object> GetFirebaseClaims() =>
                new Dictionary<string, object>();

            private protected virtual FirebaseUserManager.Args CreateUserManagerArgs(
                HttpMessageHandler handler)
            {
                return new FirebaseUserManager.Args
                {
                    Credential = MockCredential,
                    ProjectId = ProjectId,
                    ClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                    RetryOptions = RetryOptions.NoBackOff,
                };
            }

            private protected abstract FirebaseTokenVerifierArgs CreateTokenVerifierArgs();

            private protected abstract AbstractFirebaseAuth CreateAuth(
                FirebaseTokenVerifier verifier, FirebaseUserManager userManager);

            private protected abstract void AssertDecodedToken(FirebaseToken decoded);
        }

        private sealed class FirebaseAuthTestConfig : TestConfig
        {
            internal static readonly FirebaseAuthTestConfig DefaultInstance =
                new FirebaseAuthTestConfig();

            protected override string BaseUrl => $"/v1/projects/{ProjectId}";

            private protected override FirebaseTokenVerifierArgs CreateTokenVerifierArgs()
            {
                return FirebaseTokenVerifierArgs.ForIdTokens(ProjectId, KeySource, Clock);
            }

            private protected override AbstractFirebaseAuth CreateAuth(
                FirebaseTokenVerifier tokenVerifier, FirebaseUserManager userManager)
            {
                var authArgs = FirebaseAuth.Args.CreateDefault();
                authArgs.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(tokenVerifier);
                authArgs.UserManager = new Lazy<FirebaseUserManager>(userManager);
                return new FirebaseAuth(authArgs);
            }

            private protected override void AssertDecodedToken(FirebaseToken decoded)
            {
                Assert.Null(decoded.TenantId);
            }
        }

        private sealed class TenantAwareFirebaseAuthTestConfig : TestConfig
        {
            internal static readonly TenantAwareFirebaseAuthTestConfig DefaultInstance =
                new TenantAwareFirebaseAuthTestConfig();

            protected override string BaseUrl => $"/v1/projects/{ProjectId}/tenants/{TenantId}";

            private protected override IDictionary<string, object> GetFirebaseClaims()
            {
                return new Dictionary<string, object>
                {
                    { "tenant", TenantId },
                };
            }

            private protected override FirebaseUserManager.Args CreateUserManagerArgs(
                HttpMessageHandler handler)
            {
                var args = base.CreateUserManagerArgs(handler);
                args.TenantId = TenantId;
                return args;
            }

            private protected override FirebaseTokenVerifierArgs CreateTokenVerifierArgs()
            {
                return FirebaseTokenVerifierArgs.ForIdTokens(
                    ProjectId, KeySource, Clock, TenantId);
            }

            private protected override AbstractFirebaseAuth CreateAuth(
                FirebaseTokenVerifier tokenVerifier, FirebaseUserManager userManager)
            {
                var authArgs = TenantAwareFirebaseAuth.Args.CreateDefault(TenantId);
                authArgs.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(tokenVerifier);
                authArgs.UserManager = new Lazy<FirebaseUserManager>(userManager);
                return new TenantAwareFirebaseAuth(authArgs);
            }

            private protected override void AssertDecodedToken(FirebaseToken decoded)
            {
                Assert.Equal(TenantId, decoded.TenantId);
            }
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
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(this.rsa);
        }
    }
}
