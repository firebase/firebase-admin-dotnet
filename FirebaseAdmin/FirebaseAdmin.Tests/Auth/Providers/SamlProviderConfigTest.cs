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
    public class SamlProviderConfigTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>()
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private const string SamlProviderConfigResponse = @"{
            ""name"": ""projects/mock-project-id/inboundSamlConfigs/saml.provider"",
            ""displayName"": ""samlProviderName"",
            ""enabled"": true,
            ""idpConfig"": {
                ""idpEntityId"": ""IDP_ENTITY_ID"",
                ""ssoUrl"": ""https://example.com/login"",
                ""idpCertificates"": [
                    {""x509Certificate"": ""CERT1""},
                    {""x509Certificate"": ""CERT2""}
                ]
            },
            ""spConfig"": {
                ""spEntityId"": ""RP_ENTITY_ID"",
                ""callbackUri"": ""https://projectId.firebaseapp.com/__/auth/handler""
            },
        }";

        private static readonly IList<string> ListConfigsResponses = new List<string>()
        {
            $@"{{
                ""nextPageToken"": ""token"",
                ""inboundSamlConfigs"": [
                    {SamlProviderConfigResponse},
                    {SamlProviderConfigResponse},
                    {SamlProviderConfigResponse}
                ]
            }}",
            $@"{{
                ""inboundSamlConfigs"": [
                    {SamlProviderConfigResponse},
                    {SamlProviderConfigResponse}
                ]
            }}",
        };

        [Fact]
        public async Task GetConfig()
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            var provider = await auth.GetSamlProviderConfigAsync("saml.provider");

            this.AssertSamlProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal(
                "/v2/projects/project1/inboundSamlConfigs/saml.provider",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task GetConfigNoProviderId(string providerId)
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetSamlProviderConfigAsync(providerId));
            Assert.Equal("Provider ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task GetConfigInvalidProviderId()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetSamlProviderConfigAsync("oidc.provider"));
            Assert.Equal("SAML provider ID must have the prefix 'saml.'.", exception.Message);
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
                () => auth.GetSamlProviderConfigAsync("saml.provider"));
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
                Response = SamlProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.provider",
                DisplayName = "samlProviderName",
                Enabled = true,
                IdpEntityId = "IDP_ENTITY_ID",
                SsoUrl = "https://example.com/login",
                X509Certificates = new List<string>() { "CERT1", "CERT2" },
                RpEntityId = "RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/handler",
            };

            var provider = await auth.CreateProviderConfigAsync(args);

            this.AssertSamlProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "/v2/projects/project1/inboundSamlConfigs?inboundSamlConfigId=saml.provider",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("samlProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Fact]
        public async Task CreateConfigMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.minimal-provider",
                IdpEntityId = "IDP_ENTITY_ID",
                SsoUrl = "https://example.com/login",
                X509Certificates = new List<string>() { "CERT1", "CERT2" },
                RpEntityId = "RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/handler",
            };

            var provider = await auth.CreateProviderConfigAsync(args);

            this.AssertSamlProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "/v2/projects/project1/inboundSamlConfigs?inboundSamlConfigId=saml.minimal-provider",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(2, body.Count);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Fact]
        public async Task CreateConfigNullArgs()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.CreateProviderConfigAsync(null as SamlProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidCreateArgs))]
        public async Task CreateConfigInvalidArgs(SamlProviderConfigArgs args, string expected)
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
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.minimal-provider",
                IdpEntityId = "IDP_ENTITY_ID",
                SsoUrl = "https://example.com/login",
                X509Certificates = new List<string>() { "CERT1", "CERT2" },
                RpEntityId = "RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/handler",
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
                Response = SamlProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.provider",
                DisplayName = "samlProviderName",
                Enabled = true,
                IdpEntityId = "IDP_ENTITY_ID",
                SsoUrl = "https://example.com/login",
                X509Certificates = new List<string>() { "CERT1", "CERT2" },
                RpEntityId = "RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/handler",
            };

            var provider = await auth.UpdateProviderConfigAsync(args);

            this.AssertSamlProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(ProviderConfigTestUtils.PatchMethod, request.Method);
            var mask = "displayName,enabled,idpConfig.idpCertificates,idpConfig.idpEntityId,idpConfig.ssoUrl,"
                + "spConfig.callbackUri,spConfig.spEntityId";
            Assert.Equal(
                $"/v2/projects/project1/inboundSamlConfigs/saml.provider?updateMask={mask}",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("samlProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Fact]
        public async Task UpdateConfigMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.minimal-provider",
                IdpEntityId = "IDP_ENTITY_ID",
            };

            var provider = await auth.UpdateProviderConfigAsync(args);

            this.AssertSamlProviderConfig(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(ProviderConfigTestUtils.PatchMethod, request.Method);
            Assert.Equal(
                "/v2/projects/project1/inboundSamlConfigs/saml.minimal-provider?updateMask=idpConfig.idpEntityId",
                request.Url.PathAndQuery);
            ProviderConfigTestUtils.AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Single(body);
            var idpConfig = (JObject)body["idpConfig"];
            Assert.Equal("IDP_ENTITY_ID", idpConfig["idpEntityId"]);
        }

        [Fact]
        public async Task UpdateConfigNullArgs()
        {
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.UpdateProviderConfigAsync(null as SamlProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidUpdateArgs))]
        public async Task UpdateConfigInvalidArgs(SamlProviderConfigArgs args, string expected)
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
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.provider",
                IdpEntityId = "IDP_ENTITY_ID",
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
        public async Task ListConfigs()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<SamlProviderConfig>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                configs.Add(enumerator.Current);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

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
        public void ListSamlForEach()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<SamlProviderConfig>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                configs.Add(user);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

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
        public async Task ListSamlByPages()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<SamlProviderConfig>();

            // Read page 1.
            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var configPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, configPage.Count());
            Assert.Equal("token", configPage.NextPageToken);

            Assert.Single(handler.Requests);
            var query = ProviderConfigTestUtils.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("3", query["pageSize"]);
            configs.AddRange(configPage);

            // Read page 2.
            pagedEnumerable = auth.ListSamlProviderConfigsAsync(new ListProviderConfigsOptions()
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
            Assert.All(configs, this.AssertSamlProviderConfig);
        }

        [Fact]
        public async Task ListSamlAsRawResponses()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<SamlProviderConfig>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetEnumerator();
            while (await responses.MoveNext())
            {
                configs.AddRange(responses.Current.ProviderConfigs);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(new List<string>() { "token", null }, tokens);
            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

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
        public void ListSamlOptions()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var configs = new List<SamlProviderConfig>();
            var customOptions = new ListProviderConfigsOptions()
            {
                PageSize = 3,
                PageToken = "custom-token",
            };

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(customOptions);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                configs.Add(user);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

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
        public void ListSamlInvalidOptions(ListProviderConfigsOptions options, string expected)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);

            var exception = Assert.Throws<ArgumentException>(
                () => auth.ListSamlProviderConfigsAsync(options));

            Assert.Equal(expected, exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ListSamlReadPageSizeTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = ProviderConfigTestUtils.CreateFirebaseAuth(handler);
            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(101));

            Assert.Empty(handler.Requests);
        }

        private void AssertSamlProviderConfig(SamlProviderConfig provider)
        {
            Assert.Equal("saml.provider", provider.ProviderId);
            Assert.Equal("samlProviderName", provider.DisplayName);
            Assert.True(provider.Enabled);
            Assert.Equal("IDP_ENTITY_ID", provider.IdpEntityId);
            Assert.Equal("https://example.com/login", provider.SsoUrl);
            Assert.Equal(2, provider.X509Certificates.Count());
            Assert.Equal("CERT1", provider.X509Certificates.ElementAt(0));
            Assert.Equal("CERT2", provider.X509Certificates.ElementAt(1));
            Assert.Equal("RP_ENTITY_ID", provider.RpEntityId);
            Assert.Equal(
              "https://projectId.firebaseapp.com/__/auth/handler", provider.CallbackUrl);
        }

        private void AssertIdpConfig(JObject idpConfig)
        {
            Assert.Equal(3, idpConfig.Count);
            Assert.Equal("IDP_ENTITY_ID", idpConfig["idpEntityId"]);
            Assert.Equal("https://example.com/login", idpConfig["ssoUrl"]);
            var certs = idpConfig["idpCertificates"].Select((token) => (JObject)token);
            Assert.Equal(2, certs.Count());
            Assert.Equal("CERT1", certs.ElementAt(0)["x509Certificate"]);
            Assert.Equal("CERT2", certs.ElementAt(1)["x509Certificate"]);
        }

        private void AssertSpConfig(JObject spConfig)
        {
            Assert.Equal(2, spConfig.Count);
            Assert.Equal("RP_ENTITY_ID", spConfig["spEntityId"]);
            Assert.Equal(
                "https://projectId.firebaseapp.com/__/auth/handler", spConfig["callbackUri"]);
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
                    new SamlProviderConfigArgs(),
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = string.Empty,
                    },
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                    },
                    "SAML provider ID must have the prefix 'saml.'.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                    },
                    "IDP entity ID must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                    },
                    "SSO URL must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "not a url",
                    },
                    "Malformed SSO URL: not a url",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                    },
                    "X509 certificates must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>(),
                    },
                    "X509 certificates must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>() { string.Empty },
                    },
                    "X509 certificates must not contain null or empty values.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>() { null },
                    },
                    "X509 certificates must not contain null or empty values.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>() { "CERT" },
                    },
                    "RP entity ID must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>() { "CERT" },
                        RpEntityId = "RP_ENTITY_ID",
                    },
                    "Callback URL must not be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = "IDP_ENTITY_ID",
                        SsoUrl = "https://example.com/login",
                        X509Certificates = new List<string>() { "CERT" },
                        RpEntityId = "RP_ENTITY_ID",
                        CallbackUrl = "not a url",
                    },
                    "Malformed callback URL: not a url",
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
                    new SamlProviderConfigArgs(),
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = string.Empty,
                    },
                    "Provider ID cannot be null or empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "oidc.provider",
                    },
                    "SAML provider ID must have the prefix 'saml.'.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                    },
                    "At least one field must be specified for update.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        IdpEntityId = string.Empty,
                    },
                    "IDP entity ID must not be empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        SsoUrl = string.Empty,
                    },
                    "SSO URL must not be empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        SsoUrl = "not a url",
                    },
                    "Malformed SSO URL: not a url",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        X509Certificates = new List<string>(),
                    },
                    "X509 certificates must not be empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        X509Certificates = new List<string>() { string.Empty },
                    },
                    "X509 certificates must not contain null or empty values.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        X509Certificates = new List<string>() { null },
                    },
                    "X509 certificates must not contain null or empty values.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        RpEntityId = string.Empty,
                    },
                    "RP entity ID must not be empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        CallbackUrl = string.Empty,
                    },
                    "Callback URL must not be empty.",
                };
                yield return new object[]
                {
                    new SamlProviderConfigArgs()
                    {
                        ProviderId = "saml.provider",
                        CallbackUrl = "not a url",
                    },
                    "Malformed callback URL: not a url",
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
