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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
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

        private static readonly IList<string> ListConfigsResponses = new List<string>()
        {
            $@"{{
                ""nextPageToken"": ""token"",
                ""oauthIdpConfigs"": [
                    {OidcProviderConfigResponse},
                    {OidcProviderConfigResponse},
                    {OidcProviderConfigResponse}
                ]
            }}",
            $@"{{
                ""oauthIdpConfigs"": [
                    {OidcProviderConfigResponse},
                    {OidcProviderConfigResponse}
                ]
            }}",
        };

        [Fact]
        public async Task GetConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            var provider = await auth.GetOidcProviderConfigAsync("oidc.provider");

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs/oidc.provider",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task GetConfigNoProviderId(string providerId)
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetOidcProviderConfigAsync(providerId));
            Assert.Equal("Provider ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task GetConfigInvalidProviderId()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

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
                Response = ProviderConfigTestUtils.ConfigNotFoundResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

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
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
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
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

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
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
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
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(2, body.Count);
            Assert.Equal("CLIENT_ID", body["clientId"]);
            Assert.Equal("https://oidc.com/issuer", body["issuer"]);
        }

        [Fact]
        public async Task CreateConfigNullArgs()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.CreateProviderConfigAsync(null as OidcProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidCreateArgs))]
        public async Task CreateConfigInvalidArgs(OidcProviderConfigArgs args, string expected)
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

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
                Response = ProviderConfigTestUtils.UnknownErrorResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task UpdateConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.provider",
                DisplayName = "oidcProviderName",
                Enabled = true,
                ClientId = "CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };

            var provider = await auth.UpdateProviderConfigAsync(args);

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(ProviderConfigTestUtils.PatchMethod, request.Method);
            var mask = "clientId,displayName,enabled,issuer";
            Assert.Equal(
                $"/v2/projects/project1/oauthIdpConfigs/oidc.provider?updateMask={mask}",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("oidcProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            Assert.Equal("CLIENT_ID", body["clientId"]);
            Assert.Equal("https://oidc.com/issuer", body["issuer"]);
        }

        [Fact]
        public async Task UpdateConfigMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = OidcProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.minimal-provider",
                ClientId = "CLIENT_ID",
            };

            var provider = await auth.UpdateProviderConfigAsync(args);

            this.AssertOidcProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(ProviderConfigTestUtils.PatchMethod, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs/oidc.minimal-provider?updateMask=clientId",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Single(body);
            Assert.Equal("CLIENT_ID", body["clientId"]);
        }

        [Fact]
        public async Task UpdateConfigNullArgs()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.UpdateProviderConfigAsync(null as OidcProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidUpdateArgs))]
        public async Task UpdateConfigInvalidArgs(OidcProviderConfigArgs args, string expected)
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.UpdateProviderConfigAsync(args));
            Assert.Equal(expected, exception.Message);
        }

        [Fact]
        public async Task UpdateConfigNotFoundError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ProviderConfigTestUtils.ConfigNotFoundResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = "oidc.provider",
                ClientId = "CLIENT_ID",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.UpdateProviderConfigAsync(args));

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
        public async Task DeleteConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            await auth.DeleteProviderConfigAsync("oidc.provider");

            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal(
                "/v2/projects/project1/oauthIdpConfigs/oidc.provider",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task DeleteConfigNoProviderId(string providerId)
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                () => auth.DeleteProviderConfigAsync(providerId));
        }

        [Fact]
        public async Task DeleteConfigInvalidProviderId()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.DeleteProviderConfigAsync("unknown.provider"));
            Assert.Equal(
                "Provider ID must have 'oidc.' or 'saml.' as the prefix.",
                exception.Message);
        }

        [Fact]
        public async Task DeleteConfigNotFoundError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ProviderConfigTestUtils.ConfigNotFoundResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.DeleteProviderConfigAsync("oidc.provider"));
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
        public async Task ListConfigs()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<OidcProviderConfig>();

            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                configs.Add(enumerator.Current);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertOidcProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, ProviderConfigTestUtils.AssertClientVersionHeader);
        }

        [Fact]
        public void ListOidcForEach()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<OidcProviderConfig>();

            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(null);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                configs.Add(user);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertOidcProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, ProviderConfigTestUtils.AssertClientVersionHeader);
        }

        [Fact]
        public async Task ListOidcByPages()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<OidcProviderConfig>();

            // Read page 1.
            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(null);
            var configPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, configPage.Count());
            Assert.Equal("token", configPage.NextPageToken);

            Assert.Single(handler.Requests);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("3", query["pageSize"]);
            configs.AddRange(configPage);

            // Read page 2.
            pagedEnumerable = auth.ListOidcProviderConfigsAsync(new ListProviderConfigsOptions()
            {
                PageToken = configPage.NextPageToken,
            });
            configPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(2, configPage.Count());
            Assert.Null(configPage.NextPageToken);

            Assert.Equal(2, handler.Requests.Count);
            query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);
            configs.AddRange(configPage);

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertOidcProviderConfig);
        }

        [Fact]
        public async Task ListOidcAsRawResponses()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<OidcProviderConfig>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetEnumerator();
            while (await responses.MoveNext())
            {
                configs.AddRange(responses.Current.ProviderConfigs);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(new List<string>() { "token", null }, tokens);
            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertOidcProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);
        }

        [Fact]
        public void ListOidcOptions()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<OidcProviderConfig>();
            var customOptions = new ListProviderConfigsOptions()
            {
                PageSize = 3,
                PageToken = "custom-token",
            };

            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(customOptions);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                configs.Add(user);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertOidcProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("custom-token", query["pageToken"]);

            query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, ProviderConfigTestUtils.AssertClientVersionHeader);
        }

        [Theory]
        [ClassData(typeof(ProviderConfigTestUtils.InvalidListOptions))]
        public void ListOidcInvalidOptions(ListProviderConfigsOptions options, string expected)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            var exception = Assert.Throws<ArgumentException>(
                () => auth.ListOidcProviderConfigsAsync(options));

            Assert.Equal(expected, exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ListOidcReadPageSizeTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var pagedEnumerable = auth.ListOidcProviderConfigsAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(101));

            Assert.Empty(handler.Requests);
        }

        private void AssertOidcProviderConfig(OidcProviderConfig provider)
        {
            Assert.Equal("oidc.provider", provider.ProviderId);
            Assert.Equal("oidcProviderName", provider.DisplayName);
            Assert.True(provider.Enabled);
            Assert.Equal("CLIENT_ID", provider.ClientId);
            Assert.Equal("https://oidc.com/issuer", provider.Issuer);
        }

        public class InvalidCreateArgs : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                // {
                //    1st element: InvalidInput,
                //    2nd element: ExpectedError,
                // }
                yield return new object[]
                {
                    new OidcProviderConfigArgs(),
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = string.Empty,
                    },
                    "Provider ID cannot be null or empty.",
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

        public class InvalidUpdateArgs : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                // {
                //    1st element: InvalidInput,
                //    2nd element: ExpectedError,
                // }
                yield return new object[]
                {
                    new OidcProviderConfigArgs(),
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = string.Empty,
                    },
                    "Provider ID cannot be null or empty.",
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
                    "At least one field must be specified for update.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                        ClientId = string.Empty,
                    },
                    "Client ID must not be empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                        Issuer = string.Empty,
                    },
                    "Issuer must not be empty.",
                };
                yield return new object[]
                {
                    new OidcProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                        Issuer = "not a url",
                    },
                    "Malformed issuer string: not a url",
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
