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
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Auth.Providers.Tests
{
    public class OidcProviderConfigTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>()
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private const string OidcProviderConfigResponse = @"{
            ""name"": ""projects/mock-project-id/oauthIdpConfigs/oidc.provider"",
            ""clientId"": ""CLIENT_ID"",
            ""issuer"": ""https://oidc.com/issuer"",
            ""displayName"": ""oidcProviderName"",
            ""enabled"": true
        }";

        private const string ConfigNotFoundResponse = @"{
            ""error"": {
                ""message"": ""CONFIGURATION_NOT_FOUND""
            }
        }";

        private const string UnknownErrorResponse = @"{
            ""error"": {
                ""message"": ""UNKNOWN""
            }
        }";

        private static readonly string ClientVersion =
            $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public async Task GetConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = CreateFirebaseAuth(handler);

            var provider = await auth.GetOidcProviderConfigAsync("oidc.provider");

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs/oidc.provider",
                request.Url.PathAndQuery);
            this.AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task GetConfigNoProviderId(string providerId)
        {
            var auth = CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetOidcProviderConfigAsync(providerId));
            Assert.Equal("Provider ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task GetConfigInvalidProviderId()
        {
            var auth = CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetOidcProviderConfigAsync("saml.provider"));
            Assert.Equal("OIDC provider ID must have the prefix 'oidc.'.", exception.Message);
        }

        [Fact]
        public async Task GetConfigNotFoundError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ConfigNotFoundResponse,
            };
            var auth = CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.GetOidcProviderConfigAsync("oidc.provider"));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No identity provider configuration found for the given identifier "
                + "(CONFIGURATION_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task CreateConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.provider",
                DisplayName = "oidcProviderName",
                Enabled = true,
                ClientId = "CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };

            var provider = await auth.CreateProviderConfigAsync(args);

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs?oauthIdpConfigId=oidc.provider",
                request.Url.PathAndQuery);
            this.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("oidcProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            Assert.Equal("CLIENT_ID", body["clientId"]);
            Assert.Equal("https://oidc.com/issuer", body["issuer"]);
        }

        [Fact]
        public async Task CreateConfigMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.minimal-provider",
                ClientId = "CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };

            var provider = await auth.CreateProviderConfigAsync(args);

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs?oauthIdpConfigId=oidc.minimal-provider",
                request.Url.PathAndQuery);
            this.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(2, body.Count);
            Assert.Equal("CLIENT_ID", body["clientId"]);
            Assert.Equal("https://oidc.com/issuer", body["issuer"]);
        }

        [Fact]
        public async Task CreateConfigNullArgs()
        {
            var auth = CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.CreateProviderConfigAsync(null as OidcProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidCreateArgs))]
        public async Task CreateConfigInvalidArgs(OidcProviderConfigArgs args, string expected)
        {
            var auth = CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.CreateProviderConfigAsync(args));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task CreateConfigUnknownError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = UnknownErrorResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.provider",
                ClientId = "CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.CreateProviderConfigAsync(args));
            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.StartsWith(
                "Unexpected HTTP response with status: 500 (InternalServerError)",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        internal static FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler = null)
        {
            var providerConfigManager = new ProviderConfigManager(new ProviderConfigManager.Args
            {
                Credential = MockCredential,
                ProjectId = "project1",
                ClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                RetryOptions = RetryOptions.NoBackOff,
            });
            var args = FirebaseAuth.Args.CreateDefault();
            args.ProviderConfigManager = new Lazy<ProviderConfigManager>(providerConfigManager);
            return new FirebaseAuth(args);
        }

        private void AssertOidcProviderConfig(OidcProviderConfig provider)
        {
            Assert.Equal("oidc.provider", provider.ProviderId);
            Assert.Equal("oidcProviderName", provider.DisplayName);
            Assert.True(provider.Enabled);
            Assert.Equal("CLIENT_ID", provider.ClientId);
            Assert.Equal("https://oidc.com/issuer", provider.Issuer);
        }

        private void AssertClientVersionHeader(MockMessageHandler.IncomingRequest request)
        {
            Assert.Contains(ClientVersion, request.Headers.GetValues("X-Client-Version"));
        }

        public class InvalidCreateArgs : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                // { InvalidInput, ExpectedError }
                yield return new object[]
                {
                    new OidcProviderConfigArgs(),
                    "OIDC provider ID must have the prefix 'oidc.'.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = string.Empty,
                    },
                    "OIDC provider ID must have the prefix 'oidc.'.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                    },
                    "OIDC provider ID must have the prefix 'oidc.'.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                    },
                    "Client ID must not be null or empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                        ClientId = "CLIENT_ID",
                    },
                    "Issuer must not be null or empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                        ClientId = "CLIENT_ID",
                        Issuer = "not a url",
                    },
                    "Malformed issuer string: not a url",
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
