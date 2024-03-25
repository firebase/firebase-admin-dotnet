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
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Auth.Multitenancy.Tests
{
    public class TenantManagerTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { TestConfig.Default },
            new object[] { TestConfig.WithEmulator },
        };

        private const string TenantResponse = @"{
            ""name"": ""projects/project1/tenants/tenant1"",
            ""displayName"": ""Test Tenant"",
            ""allowPasswordSignup"": true,
            ""enableEmailLinkSignin"": true
        }";

        private const string TenantNotFoundResponse = @"{
            ""error"": {
                ""message"": ""TENANT_NOT_FOUND""
            }
        }";

        private const string UnknownErrorResponse = @"{
            ""error"": {
                ""message"": ""UNKNOWN""
            }
        }";

        private static readonly IList<string> ListTenantsResponses = new List<string>()
        {
            $@"{{
                ""nextPageToken"": ""token"",
                ""tenants"": [
                    {TenantResponse},
                    {TenantResponse},
                    {TenantResponse}
                ]
            }}",
            $@"{{
                ""tenants"": [
                    {TenantResponse},
                    {TenantResponse}
                ]
            }}",
        };

        public static IEnumerable<object[]> InvalidStrings()
        {
            var strings = new List<string>() { null, string.Empty };
            return TestConfigs.SelectMany(
                config => strings, (config, str) => config.Concat(new object[] { str }).ToArray());
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetTenant(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var provider = await auth.TenantManager.GetTenantAsync("tenant1");

            AssertTenant(provider);
            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            config.AssertRequest("tenants/tenant1", request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task GetTenantNoId(TestConfig config, string tenantId)
        {
            var auth = config.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.GetTenantAsync(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetTenantNotFoundError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.TenantManager.GetTenantAsync("tenant1"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.TenantNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No tenant found for the given identifier (TENANT_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateTenant(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
                PasswordSignUpAllowed = true,
                EmailLinkSignInEnabled = true,
            };

            var provider = await auth.TenantManager.CreateTenantAsync(args);

            AssertTenant(provider);
            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            config.AssertRequest("tenants", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(3, body.Count);
            Assert.Equal("Test Tenant", body["displayName"]);
            Assert.True((bool)body["allowPasswordSignup"]);
            Assert.True((bool)body["enableEmailLinkSignin"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateTenantMinimal(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var provider = await auth.TenantManager.CreateTenantAsync(new TenantArgs());

            AssertTenant(provider);
            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            config.AssertRequest("tenants", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Empty(body);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateTenantNullArgs(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.TenantManager.CreateTenantAsync(null));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateTenantError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = UnknownErrorResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.TenantManager.CreateTenantAsync(new TenantArgs()));
            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.StartsWith(
                "Unexpected HTTP response with status: 500 (InternalServerError)",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateTenant(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
                PasswordSignUpAllowed = true,
                EmailLinkSignInEnabled = true,
            };

            var provider = await auth.TenantManager.UpdateTenantAsync("tenant1", args);

            AssertTenant(provider);
            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpUtils.Patch, request.Method);
            var mask = "allowPasswordSignup,displayName,enableEmailLinkSignin";
            config.AssertRequest($"tenants/tenant1?updateMask={mask}", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(3, body.Count);
            Assert.Equal("Test Tenant", body["displayName"]);
            Assert.True((bool)body["allowPasswordSignup"]);
            Assert.True((bool)body["enableEmailLinkSignin"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateTenantMinimal(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
            };

            var provider = await auth.TenantManager.UpdateTenantAsync("tenant1", args);

            AssertTenant(provider);
            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpUtils.Patch, request.Method);
            config.AssertRequest("tenants/tenant1?updateMask=displayName", request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Single(body);
            Assert.Equal("Test Tenant", body["displayName"]);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task UpdateTenantNoId(TestConfig config, string tenantId)
        {
            var auth = config.CreateFirebaseAuth();
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.UpdateTenantAsync(tenantId, args));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateTenantNullArgs(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.TenantManager.UpdateTenantAsync("tenant1", null));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateTenantEmptyArgs(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.UpdateTenantAsync("tenant1", new TenantArgs()));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateTenantError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.TenantManager.UpdateTenantAsync("tenant1", args));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.TenantNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No tenant found for the given identifier (TENANT_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteTenant(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            await auth.TenantManager.DeleteTenantAsync("tenant1");

            Assert.Single(handler.Requests);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Delete, request.Method);
            config.AssertRequest("tenants/tenant1", request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task DeleteTenantNoId(TestConfig config, string tenantId)
        {
            var auth = config.CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.DeleteTenantAsync(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteTenantNotFoundError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => auth.TenantManager.DeleteTenantAsync("tenant1"));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.TenantNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No tenant found for the given identifier (TENANT_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListTenants(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();

            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                tenants.Add(enumerator.Current);
            }

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("tenants?pageSize=100", handler.Requests[0]);
            config.AssertRequest("tenants?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListTenantsForEach(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();

            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            foreach (var tenant in pagedEnumerable.ToEnumerable())
            {
                tenants.Add(tenant);
            }

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("tenants?pageSize=100", handler.Requests[0]);
            config.AssertRequest("tenants?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListTenantsByPages(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();

            // Read page 1.
            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            var tenantsPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, tenantsPage.Count());
            Assert.Equal("token", tenantsPage.NextPageToken);

            Assert.Single(handler.Requests);
            config.AssertRequest("tenants?pageSize=3", handler.Requests[0]);
            tenants.AddRange(tenantsPage);

            // Read page 2.
            pagedEnumerable = auth.TenantManager.ListTenantsAsync(new ListTenantsOptions()
            {
                PageToken = tenantsPage.NextPageToken,
            });
            tenantsPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(2, tenantsPage.Count());
            Assert.Null(tenantsPage.NextPageToken);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("tenants?pageSize=3&pageToken=token", handler.Requests[1]);
            tenants.AddRange(tenantsPage);

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListTenantsAsRawResponses(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
            while (await responses.MoveNextAsync())
            {
                tenants.AddRange(responses.Current.Tenants);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(new List<string>() { "token", null }, tokens);
            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("tenants?pageSize=100", handler.Requests[0]);
            config.AssertRequest("tenants?pageSize=100&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListTenantsOptions(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();
            var customOptions = new ListTenantsOptions()
            {
                PageSize = 3,
                PageToken = "custom-token",
            };

            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(customOptions);
            foreach (var tenant in pagedEnumerable.ToEnumerable())
            {
                tenants.Add(tenant);
            }

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("tenants?pageSize=3&pageToken=custom-token", handler.Requests[0]);
            config.AssertRequest("tenants?pageSize=3&pageToken=token", handler.Requests[1]);
        }

        [Theory]
        [ClassData(typeof(TenantManagerTest.InvalidListOptions))]
        public void ListInvalidOptions(TestConfig config, ListTenantsOptions options, string expected)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);

            var exception = Assert.Throws<ArgumentException>(
                () => auth.TenantManager.ListTenantsAsync(options));

            Assert.Equal(expected, exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListReadPageSizeTooLarge(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = config.CreateFirebaseAuth(handler);
            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(101));

            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void AuthForTenant(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();

            var tenantAwareAuth = auth.TenantManager.AuthForTenant("tenant1");

            Assert.Equal("tenant1", tenantAwareAuth.TenantId);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void AuthForTenantCaching(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();

            var tenantAwareAuth1 = auth.TenantManager.AuthForTenant("tenant1");
            var tenantAwareAuth2 = auth.TenantManager.AuthForTenant("tenant1");
            var tenantAwareAuth3 = auth.TenantManager.AuthForTenant("tenant2");

            Assert.Same(tenantAwareAuth1, tenantAwareAuth2);
            Assert.NotSame(tenantAwareAuth1, tenantAwareAuth3);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void AuthForTenantNoTenantId(TestConfig config, string tenantId)
        {
            var auth = config.CreateFirebaseAuth();

            var exception = Assert.Throws<ArgumentException>(
                () => auth.TenantManager.AuthForTenant(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UseAfterDelete(TestConfig config)
        {
            var auth = config.CreateFirebaseAuth();
            var tenantManager = auth.TenantManager;

            (auth as IFirebaseService).Delete();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => tenantManager.GetTenantAsync("tenant1"));
            Assert.Throws<ObjectDisposedException>(
                () => tenantManager.AuthForTenant("tenant1"));
        }

        private static void AssertTenant(Tenant tenant)
        {
            Assert.Equal("tenant1", tenant.TenantId);
            Assert.Equal("Test Tenant", tenant.DisplayName);
            Assert.True(tenant.PasswordSignUpAllowed);
            Assert.True(tenant.EmailLinkSignInEnabled);
        }

        public class TestConfig
        {
            private static readonly string IdToolkitUrl = $"identitytoolkit.googleapis.com/v2/projects/project1";

            private static readonly string ClientVersion =
                $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

            private static readonly GoogleCredential MockCredential =
                GoogleCredential.FromAccessToken("test-token");

            private TestConfig(string emulatorHost = null)
            {
                this.EmulatorHost = emulatorHost;
            }

            public static TestConfig Default => new TestConfig();

            public static TestConfig WithEmulator => new TestConfig("localhost:9090");

            internal string EmulatorHost { get; }

            internal FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler = null)
            {
                var tenantManager = new TenantManager(new TenantManager.Args
                {
                    Credential = MockCredential,
                    ProjectId = "project1",
                    ClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                    RetryOptions = RetryOptions.NoBackOff,
                    EmulatorHost = this.EmulatorHost,
                });
                var args = FirebaseAuth.Args.CreateDefault();
                args.TenantManager = new Lazy<TenantManager>(() => tenantManager);
                return new FirebaseAuth(args);
            }

            internal void AssertRequest(
                string expectedSuffix, MockMessageHandler.IncomingRequest request)
            {
                if (this.EmulatorHost != null)
                {
                    var expectedUrl = $"http://{this.EmulatorHost}/{IdToolkitUrl}/{expectedSuffix}";
                    Assert.Equal(expectedUrl, request.Url.ToString());
                    Assert.Equal("Bearer owner", request.Headers.Authorization.ToString());
                }
                else
                {
                    var expectedUrl = $"https://{IdToolkitUrl}/{expectedSuffix}";
                    Assert.Equal(expectedUrl, request.Url.ToString());
                    Assert.Equal("Bearer test-token", request.Headers.Authorization.ToString());
                }

                Assert.Contains(ClientVersion, request.Headers.GetValues("X-Client-Version"));
            }
        }

        public class InvalidListOptions : IEnumerable<object[]>
        {
            // {
            //    1st element: InvalidInput,
            //    2nd element: ExpectedError,
            // }
            private static readonly List<object[]> TestCases = new List<object[]>
            {
                new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = 101,
                    },
                    "Page size must not exceed 100.",
                },
                new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = 0,
                    },
                    "Page size must be positive.",
                },
                new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = -1,
                    },
                    "Page size must be positive.",
                },
                new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageToken = string.Empty,
                    },
                    "Page token must not be empty.",
                },
            };

            public IEnumerator<object[]> GetEnumerator()
            {
                return TestConfigs
                    .SelectMany(config => TestCases, (config, testCase) => config.Concat(testCase).ToArray())
                    .GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
