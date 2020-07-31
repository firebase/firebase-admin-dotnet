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
    }
}
