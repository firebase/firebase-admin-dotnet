using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Tests.AppCheck;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.AppCheck.Tests
{
    public class AppCheckApiClientTest
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromFile("./resources/service_account.json");

        private readonly string appId = "1:1234:android:1234";

        [Fact]
        public void NoProjectId()
        {
            var args = new AppCheckApiClient.Args()
            {
                ClientFactory = new HttpClientFactory(),
                Credential = null,
            };

            args.ProjectId = null;
            Assert.Throws<FirebaseAppCheckException>(() => new AppCheckApiClient(args));

            args.ProjectId = string.Empty;
            Assert.Throws<FirebaseAppCheckException>(() => new AppCheckApiClient(args));
        }

        [Fact]
        public void NoCredential()
        {
            var args = new AppCheckApiClient.Args()
            {
                ClientFactory = new HttpClientFactory(),
                Credential = null,
                ProjectId = "test-project",
            };

            Assert.Throws<ArgumentNullException>(() => new AppCheckApiClient(args));
        }

        [Fact]
        public void NoClientFactory()
        {
            var args = new AppCheckApiClient.Args()
            {
                ClientFactory = null,
                Credential = MockCredential,
                ProjectId = "test-project",
            };

            Assert.Throws<ArgumentNullException>(() => new AppCheckApiClient(args));
        }

        [Fact]
        public async Task ExchangeToken()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var response = await client.ExchangeTokenAsync(customToken, this.appId);

            Assert.Equal("test-token", response.Token);
            Assert.Equal(36000000, response.TtlMillis);

            var req = JsonConvert.DeserializeObject<AppCheckApiClient.ExchangeTokenRequest>(handler.LastRequestBody);
            Assert.Equal("test-token", req.CustomToken);
            Assert.Equal(1, handler.Calls);
            this.CheckHeaders(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task ExchangeTokenWithEmptyAppId()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(customToken, string.Empty));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("appId must be a non-empty string.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, handler.Calls);
        }

        [Fact]
        public async Task ExchangeTokenWithNullAppId()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(customToken, null));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("appId must be a non-empty string.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, handler.Calls);
        }

        [Fact]
        public async Task ExchangeTokenWithEmptyCustomToken()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(string.Empty, this.appId));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("customToken must be a non-empty string.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, handler.Calls);
        }

        [Fact]
        public async Task ExchangeTokenWithNullCustomToken()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(null, this.appId));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("customToken must be a non-empty string.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(0, handler.Calls);
        }

        [Fact]
        public async Task ExchangeTokenWithErrorNoTtlResponse()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Token = "test-token",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(customToken, this.appId));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("`ttl` must be a valid duration string with the suffix `s`.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task ExchangeTokenWithErrorNoTokenResponse()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.ExchangeTokenResponse()
                {
                    Ttl = "36000s",
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await client.ExchangeTokenAsync(customToken, this.appId));

            Assert.Equal(ErrorCode.PermissionDenied, ex.ErrorCode);
            Assert.Equal("Token is not valid", ex.Message);
            Assert.Equal(AppCheckErrorCode.AppCheckTokenExpired, ex.AppCheckErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task VerifyReplayProtectionWithTrue()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.VerifyTokenResponse()
                {
                    AlreadyConsumed = true,
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var response = await client.VerifyReplayProtectionAsync(customToken);

            Assert.True(response);

            var req = JsonConvert.DeserializeObject<AppCheckApiClient.ExchangeTokenRequest>(handler.LastRequestBody);
            Assert.Equal(1, handler.Calls);
            this.CheckHeaders(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task VerifyReplayProtectionWithFalse()
        {
            var handler = new MockAppCheckHandler()
            {
                Response = new AppCheckApiClient.VerifyTokenResponse()
                {
                    AlreadyConsumed = false,
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateAppCheckApiClient(factory);

            string customToken = "test-token";

            var response = await client.VerifyReplayProtectionAsync(customToken);

            Assert.False(response);

            var req = JsonConvert.DeserializeObject<AppCheckApiClient.ExchangeTokenRequest>(handler.LastRequestBody);
            Assert.Equal(1, handler.Calls);
            this.CheckHeaders(handler.LastRequestHeaders);
        }

        private AppCheckApiClient CreateAppCheckApiClient(HttpClientFactory factory)
        {
            return new AppCheckApiClient(new AppCheckApiClient.Args()
            {
                ClientFactory = factory,
                Credential = MockCredential,
                ProjectId = "test-project",
                RetryOptions = RetryOptions.NoBackOff,
            });
        }

        private void CheckHeaders(HttpRequestHeaders header)
        {
            var versionHeader = header.GetValues("X-Firebase-Client").First();
            Assert.Equal(AppCheckApiClient.ClientVersion, versionHeader);
        }
    }
}
