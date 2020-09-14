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
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>()
        {
            new object[] { null },
            new object[] { string.Empty },
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

        private static readonly string ClientVersion =
            $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

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

        [Fact]
        public async Task GetTenant()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);

            var provider = await auth.TenantManager.GetTenantAsync("tenant1");

            AssertTenant(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/v2/projects/project1/tenants/tenant1", request.Url.PathAndQuery);
            AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task GetTenantNoId(string tenantId)
        {
            var auth = CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.GetTenantAsync(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task GetTenantNotFoundError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = CreateFirebaseAuth(handler);

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

        [Fact]
        public async Task CreateTenant()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
                PasswordSignUpAllowed = true,
                EmailLinkSignInEnabled = true,
            };

            var provider = await auth.TenantManager.CreateTenantAsync(args);

            AssertTenant(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v2/projects/project1/tenants", request.Url.PathAndQuery);
            AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(3, body.Count);
            Assert.Equal("Test Tenant", body["displayName"]);
            Assert.True((bool)body["allowPasswordSignup"]);
            Assert.True((bool)body["enableEmailLinkSignin"]);
        }

        [Fact]
        public async Task CreateTenantMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);

            var provider = await auth.TenantManager.CreateTenantAsync(new TenantArgs());

            AssertTenant(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/v2/projects/project1/tenants", request.Url.PathAndQuery);
            AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Empty(body);
        }

        [Fact]
        public async Task CreateTenantNullArgs()
        {
            var auth = CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.TenantManager.CreateTenantAsync(null));
        }

        [Fact]
        public async Task CreateTenantError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = UnknownErrorResponse,
            };
            var auth = CreateFirebaseAuth(handler);

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

        [Fact]
        public async Task UpdateTenant()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
                PasswordSignUpAllowed = true,
                EmailLinkSignInEnabled = true,
            };

            var provider = await auth.TenantManager.UpdateTenantAsync("tenant1", args);

            AssertTenant(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpUtils.Patch, request.Method);
            var mask = "allowPasswordSignup,displayName,enableEmailLinkSignin";
            Assert.Equal(
                $"/v2/projects/project1/tenants/tenant1?updateMask={mask}",
                request.Url.PathAndQuery);
            AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Equal(3, body.Count);
            Assert.Equal("Test Tenant", body["displayName"]);
            Assert.True((bool)body["allowPasswordSignup"]);
            Assert.True((bool)body["enableEmailLinkSignin"]);
        }

        [Fact]
        public async Task UpdateTenantMinimal()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
            };

            var provider = await auth.TenantManager.UpdateTenantAsync("tenant1", args);

            AssertTenant(provider);
            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpUtils.Patch, request.Method);
            Assert.Equal(
                "/v2/projects/project1/tenants/tenant1?updateMask=displayName",
                request.Url.PathAndQuery);
            AssertClientVersionHeader(request);

            var body = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.Single(body);
            Assert.Equal("Test Tenant", body["displayName"]);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task UpdateTenantNoId(string tenantId)
        {
            var auth = CreateFirebaseAuth();
            var args = new TenantArgs()
            {
                DisplayName = "Test Tenant",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.UpdateTenantAsync(tenantId, args));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task UpdateTenantNullArgs()
        {
            var auth = CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentNullException>(
                () => auth.TenantManager.UpdateTenantAsync("tenant1", null));
        }

        [Fact]
        public async Task UpdateTenantEmptyArgs()
        {
            var auth = CreateFirebaseAuth();

            await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.UpdateTenantAsync("tenant1", new TenantArgs()));
        }

        [Fact]
        public async Task UpdateTenantError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task DeleteTenant()
        {
            var handler = new MockMessageHandler()
            {
                Response = TenantResponse,
            };
            var auth = CreateFirebaseAuth(handler);

            await auth.TenantManager.DeleteTenantAsync("tenant1");

            Assert.Equal(1, handler.Requests.Count);
            var request = handler.Requests[0];
            Assert.Equal(HttpMethod.Delete, request.Method);
            Assert.Equal("/v2/projects/project1/tenants/tenant1", request.Url.PathAndQuery);
            AssertClientVersionHeader(request);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task DeleteTenantNoId(string tenantId)
        {
            var auth = CreateFirebaseAuth();

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.TenantManager.DeleteTenantAsync(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task DeleteTenantNotFoundError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = TenantNotFoundResponse,
            };
            var auth = CreateFirebaseAuth(handler);

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

        [Fact]
        public async Task ListTenants()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
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
            var query = ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, AssertClientVersionHeader);
        }

        [Fact]
        public void ListTenantsForEach()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();

            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            foreach (var tenant in pagedEnumerable.ToEnumerable())
            {
                tenants.Add(tenant);
            }

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);

            Assert.Equal(2, handler.Requests.Count);
            var query = ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, AssertClientVersionHeader);
        }

        [Fact]
        public async Task ListTenantsByPages()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
            var tenants = new List<Tenant>();

            // Read page 1.
            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);
            var tenantsPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, tenantsPage.Count());
            Assert.Equal("token", tenantsPage.NextPageToken);

            Assert.Single(handler.Requests);
            var query = ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("3", query["pageSize"]);
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
            query = ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);
            tenants.AddRange(tenantsPage);

            Assert.Equal(5, tenants.Count);
            Assert.All(tenants, AssertTenant);
        }

        [Fact]
        public async Task ListTenantsAsRawResponses()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
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
            var query = ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("100", query["pageSize"]);

            query = ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("100", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);
        }

        [Fact]
        public void ListTenantsOptions()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
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
            var query = ExtractQueryParams(handler.Requests[0]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("custom-token", query["pageToken"]);

            query = ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["pageSize"]);
            Assert.Equal("token", query["pageToken"]);

            Assert.All(handler.Requests, AssertClientVersionHeader);
        }

        [Theory]
        [ClassData(typeof(TenantManagerTest.InvalidListOptions))]
        public void ListInvalidOptions(ListTenantsOptions options, string expected)
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);

            var exception = Assert.Throws<ArgumentException>(
                () => auth.TenantManager.ListTenantsAsync(options));

            Assert.Equal(expected, exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ListReadPageSizeTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListTenantsResponses,
            };
            var auth = CreateFirebaseAuth(handler);
            var pagedEnumerable = auth.TenantManager.ListTenantsAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(101));

            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void AuthForTenant()
        {
            var auth = CreateFirebaseAuth();

            var tenantAwareAuth = auth.TenantManager.AuthForTenant("tenant1");

            Assert.Equal("tenant1", tenantAwareAuth.TenantId);
        }

        [Fact]
        public void AuthForTenantCaching()
        {
            var auth = CreateFirebaseAuth();

            var tenantAwareAuth1 = auth.TenantManager.AuthForTenant("tenant1");
            var tenantAwareAuth2 = auth.TenantManager.AuthForTenant("tenant1");
            var tenantAwareAuth3 = auth.TenantManager.AuthForTenant("tenant2");

            Assert.Same(tenantAwareAuth1, tenantAwareAuth2);
            Assert.NotSame(tenantAwareAuth1, tenantAwareAuth3);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void AuthForTenantNoTenantId(string tenantId)
        {
            var auth = CreateFirebaseAuth();

            var exception = Assert.Throws<ArgumentException>(
                () => auth.TenantManager.AuthForTenant(tenantId));
            Assert.Equal("Tenant ID cannot be null or empty.", exception.Message);
        }

        [Fact]
        public async Task UseAfterDelete()
        {
            var auth = CreateFirebaseAuth();
            var tenantManager = auth.TenantManager;

            (auth as IFirebaseService).Delete();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => tenantManager.GetTenantAsync("tenant1"));
            Assert.Throws<ObjectDisposedException>(
                () => tenantManager.AuthForTenant("tenant1"));
        }

        private static FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler = null)
        {
            var tenantManager = new TenantManager(new TenantManager.Args
            {
                Credential = MockCredential,
                ProjectId = "project1",
                ClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                RetryOptions = RetryOptions.NoBackOff,
            });
            var args = FirebaseAuth.Args.CreateDefault();
            args.TenantManager = new Lazy<TenantManager>(tenantManager);
            return new FirebaseAuth(args);
        }

        private static void AssertTenant(Tenant tenant)
        {
            Assert.Equal("tenant1", tenant.TenantId);
            Assert.Equal("Test Tenant", tenant.DisplayName);
            Assert.True(tenant.PasswordSignUpAllowed);
            Assert.True(tenant.EmailLinkSignInEnabled);
        }

        private static void AssertClientVersionHeader(MockMessageHandler.IncomingRequest request)
        {
            Assert.Contains(ClientVersion, request.Headers.GetValues("X-Client-Version"));
        }

        private static IDictionary<string, string> ExtractQueryParams(
            MockMessageHandler.IncomingRequest req)
        {
            return req.Url.Query.Substring(1).Split('&').ToDictionary(
                entry => entry.Split('=')[0], entry => entry.Split('=')[1]);
        }

        public class InvalidListOptions : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                // {
                //    1st element: InvalidInput,
                //    2nd element: ExpectedError,
                // }
                yield return new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = 101,
                    },
                    "Page size must not exceed 100.",
                };
                yield return new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = 0,
                    },
                    "Page size must be positive.",
                };
                yield return new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageSize = -1,
                    },
                    "Page size must be positive.",
                };
                yield return new object[]
                {
                    new ListTenantsOptions()
                    {
                        PageToken = string.Empty,
                    },
                    "Page token must not be empty.",
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
