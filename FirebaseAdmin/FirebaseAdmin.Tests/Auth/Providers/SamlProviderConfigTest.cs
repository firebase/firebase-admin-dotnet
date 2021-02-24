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

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task GetConfig(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = config.CreateAuth(handler);

            var provider = await auth.GetSamlProviderConfigAsync("saml.provider");

            this.AssertSamlProviderConfig(provider);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Get, request.Method);
            config.AssertRequest("inboundSamlConfigs/saml.provider", request);
        }

        [Theory]
        [MemberData(
            nameof(ProviderTestConfig.InvalidProvierIds), MemberType=typeof(ProviderTestConfig))]
        public async Task GetConfigNoProviderId(ProviderTestConfig config, string providerId)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetSamlProviderConfigAsync(providerId));
            Assert.Equal("Provider ID cannot be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task GetConfigInvalidProviderId(ProviderTestConfig config)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.GetSamlProviderConfigAsync("oidc.provider"));
            Assert.Equal("SAML provider ID must have the prefix 'saml.'.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task GetConfigNotFoundError(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ProviderTestConfig.ConfigNotFoundResponse,
            };
            var auth = config.CreateAuth(handler);

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

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task CreateConfig(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = config.CreateAuth(handler);
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
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            config.AssertRequest("inboundSamlConfigs?inboundSamlConfigId=saml.provider", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("samlProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task CreateConfigMinimal(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = config.CreateAuth(handler);
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
            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            config.AssertRequest(
                "inboundSamlConfigs?inboundSamlConfigId=saml.minimal-provider", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(2, body.Count);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task CreateConfigNullArgs(ProviderTestConfig config)
        {
            var auth = config.CreateAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.CreateProviderConfigAsync(null as SamlProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidCreateArgs))]
        public async Task CreateConfigInvalidArgs(
            ProviderTestConfig config, SamlProviderConfigArgs args, string expected)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.CreateProviderConfigAsync(args));
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task CreateConfigUnknownError(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = ProviderTestConfig.UnknownErrorResponse,
            };
            var auth = config.CreateAuth(handler);
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

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task UpdateConfig(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = config.CreateAuth(handler);
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
            var request = Assert.Single(handler.Requests);
            Assert.Equal(ProviderTestConfig.PatchMethod, request.Method);
            var mask = "displayName,enabled,idpConfig.idpCertificates,idpConfig.idpEntityId,idpConfig.ssoUrl,"
                + "spConfig.callbackUri,spConfig.spEntityId";
            config.AssertRequest(
                $"inboundSamlConfigs/saml.provider?updateMask={mask}", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(4, body.Count);
            Assert.Equal("samlProviderName", body["displayName"]);
            Assert.True((bool)body["enabled"]);
            this.AssertIdpConfig((JObject)body["idpConfig"]);
            this.AssertSpConfig((JObject)body["spConfig"]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task UpdateConfigMinimal(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = SamlProviderConfigResponse,
            };
            var auth = config.CreateAuth(handler);
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = "saml.minimal-provider",
                IdpEntityId = "IDP_ENTITY_ID",
            };

            var provider = await auth.UpdateProviderConfigAsync(args);

            this.AssertSamlProviderConfig(provider);
            var request = Assert.Single(handler.Requests);
            Assert.Equal(ProviderTestConfig.PatchMethod, request.Method);
            config.AssertRequest(
                "inboundSamlConfigs/saml.minimal-provider?updateMask=idpConfig.idpEntityId",
                request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Single(body);
            var idpConfig = (JObject)body["idpConfig"];
            Assert.Equal("IDP_ENTITY_ID", idpConfig["idpEntityId"]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task UpdateConfigNullArgs(ProviderTestConfig config)
        {
            var auth = config.CreateAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.UpdateProviderConfigAsync(null as SamlProviderConfigArgs));
        }

        [Theory]
        [ClassData(typeof(InvalidUpdateArgs))]
        public async Task UpdateConfigInvalidArgs(
            ProviderTestConfig config, SamlProviderConfigArgs args, string expected)
        {
            var auth = config.CreateAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.UpdateProviderConfigAsync(args));
            Assert.Equal(expected, exception.Message);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task UpdateConfigNotFoundError(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ProviderTestConfig.ConfigNotFoundResponse,
            };
            var auth = config.CreateAuth(handler);
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

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task DeleteConfig(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);

            await auth.DeleteProviderConfigAsync("saml.provider");

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Delete, request.Method);
            config.AssertRequest("inboundSamlConfigs/saml.provider", request);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task DeleteConfigNotFoundError(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = ProviderTestConfig.ConfigNotFoundResponse,
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.DeleteProviderConfigAsync("saml.provider"));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No identity provider configuration found for the given identifier "
                + "(CONFIGURATION_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task ListConfigs(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
            var configs = new List<SamlProviderConfig>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                configs.Add(enumerator.Current);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100", handler.Requests[0]);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public void ListSamlForEach(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
            var configs = new List<SamlProviderConfig>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                configs.Add(user);
            }

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100", handler.Requests[0]);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task ListSamlByPages(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
            var configs = new List<SamlProviderConfig>();

            // Read page 1.
            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var configPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, configPage.Count());
            Assert.Equal("token", configPage.NextPageToken);

            var request = Assert.Single(handler.Requests);
            config.AssertRequest("inboundSamlConfigs?pageSize=3", request);
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
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=3&pageToken=token", handler.Requests[1]);
            configs.AddRange(configPage);

            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task ListSamlAsRawResponses(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
            var configs = new List<SamlProviderConfig>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.ListSamlProviderConfigsAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
            while (await responses.MoveNextAsync())
            {
                configs.AddRange(responses.Current.ProviderConfigs);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(new List<string>() { "token", null }, tokens);
            Assert.Equal(5, configs.Count);
            Assert.All(configs, this.AssertSamlProviderConfig);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100", handler.Requests[0]);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public void ListSamlOptions(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
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
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=3&pageToken=custom-token", handler.Requests[0]);
            config.AssertRequest(
                "inboundSamlConfigs?pageSize=3&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [ClassData(typeof(ProviderTestConfig.InvalidListOptions))]
        public void ListSamlInvalidOptions(
            ProviderTestConfig config, ListProviderConfigsOptions options, string expected)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);

            var exception = Assert.Throws<ArgumentException>(
                () => auth.ListSamlProviderConfigsAsync(options));

            Assert.Equal(expected, exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(ProviderTestConfig.TestConfigs), MemberType=typeof(ProviderTestConfig))]
        public async Task ListSamlReadPageSizeTooLarge(ProviderTestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListConfigsResponses,
            };
            var auth = config.CreateAuth(handler);
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
            public IEnumerator<object[]> GetEnumerator() =>
                ProviderTestConfig.WithTestConfigs(this.MakeEnumerator());

            public IEnumerator<object[]> MakeEnumerator()
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
            public IEnumerator<object[]> GetEnumerator() =>
                ProviderTestConfig.WithTestConfigs(this.MakeEnumerator());

            public IEnumerator<object[]> MakeEnumerator()
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
