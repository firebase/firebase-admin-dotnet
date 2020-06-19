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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Providers.Tests
{
    public class ProviderConfigManagerTest
    {
        private const string MockProjectId = "project1";

        private const string ClientVersionHeader = "X-Client-Version";

        private const string IdTooklitUrl = "https://identitytoolkit.googleapis.com/v2/projects/{0}";

        private static readonly string ClientVersion = $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        private static readonly string OidcProviderConfigResponse = @"{
            ""name"": ""projects/mock-project-id/oauthIdpConfigs/oidc.provider"",
            ""clientId"": ""CLIENT_ID"",
            ""issuer"": ""https://oidc.com/issuer"",
            ""displayName"": ""oidcProviderName"",
            ""enabled"": true
        }";

        private static readonly string ConfigNotFoundResponse = @"{
            ""error"": {
                ""message"": ""CONFIGURATION_NOT_FOUND""
            }
        }";

        [Fact]
        public async Task OidcProviderConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var provider = await auth.GetOidcProviderConfigAsync("oidc.provider");

            Assert.Equal("oidc.provider", provider.ProviderId);
            Assert.Equal("oidcProviderName", provider.DisplayName);
            Assert.True(provider.Enabled);
            Assert.Equal("CLIENT_ID", provider.ClientId);
            Assert.Equal("https://oidc.com/issuer", provider.Issuer);

            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs/oidc.provider",
                request.Url.PathAndQuery);
            Assert.Contains(ClientVersion, request.Headers.GetValues(ClientVersionHeader));
        }

        [Fact]
        public void NoOidcProviderConfigId()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GetOidcProviderConfigAsync(null));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GetOidcProviderConfigAsync(string.Empty));
        }

        [Fact]
        public async Task InvalidOidcProviderProviderId()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GetOidcProviderConfigAsync("saml.provider"));
            Assert.Equal("OIDC provider ID must have the prefix 'oidc.'.", exception.Message);
        }

        [Fact]
        public async Task OidcProviderConfigNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ConfigNotFoundResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetOidcProviderConfigAsync("oidc.provider"));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No identity provider configuration found for the given identifier "
                + "(CONFIGURATION_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        private FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler)
        {
            var providerConfigManager = new ProviderConfigManager(new ProviderConfigManager.Args
            {
                Credential = MockCredential,
                ProjectId = MockProjectId,
                ClientFactory = new MockHttpClientFactory(handler),
                RetryOptions = RetryOptions.NoBackOff,
            });
            var args = FirebaseAuth.Args.CreateDefault();
            args.ProviderConfigManager = new Lazy<ProviderConfigManager>(providerConfigManager);
            return new FirebaseAuth(args);
        }
    }
}
