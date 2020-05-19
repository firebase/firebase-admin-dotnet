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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class EmailActionRequestTest
    {
        public static readonly IEnumerable<object[]> InvalidActionCodeSettingsArgs =
            new List<object[]>()
            {
                new object[] { new ActionCodeSettings() },
                new object[] { new ActionCodeSettings() { Url = string.Empty } },
                new object[] { new ActionCodeSettings() { Url = "not a url" } },
                new object[]
                {
                    new ActionCodeSettings()
                    {
                        Url = "https://example.dynamic.link",
                        AndroidInstallApp = true,
                    },
                },
                new object[]
                {
                    new ActionCodeSettings()
                    {
                        Url = "https://example.dynamic.link",
                        DynamicLinkDomain = string.Empty,
                    },
                },
                new object[]
                {
                    new ActionCodeSettings()
                    {
                        Url = "https://example.dynamic.link",
                        AndroidMinimumVersion = string.Empty,
                    },
                },
                new object[]
                {
                    new ActionCodeSettings()
                    {
                        Url = "https://example.dynamic.link",
                        AndroidPackageName = string.Empty,
                    },
                },
                new object[]
                {
                    new ActionCodeSettings()
                    {
                        Url = "https://example.dynamic.link",
                        IosBundleId = string.Empty,
                    },
                },
            };

        private const string GenerateEmailLinkResponse = @"{
            ""oobLink"": ""https://mock-oob-link.for.auth.tests""
        }";

        private static readonly ActionCodeSettings ActionCodeSettings = new ActionCodeSettings()
        {
            Url = "https://example.dynamic.link",
            HandleCodeInApp = true,
            DynamicLinkDomain = "custom.page.link",
            IosBundleId = "com.example.ios",
            AndroidPackageName = "com.example.android",
            AndroidMinimumVersion = "6",
            AndroidInstallApp = true,
        };

        [Fact]
        public void NoEmail()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateEmailVerificationLinkAsync(null));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateEmailVerificationLinkAsync(string.Empty));

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GeneratePasswordResetLinkAsync(null));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GeneratePasswordResetLinkAsync(string.Empty));

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(null, ActionCodeSettings));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(string.Empty, ActionCodeSettings));
        }

        [Theory]
        [MemberData(nameof(InvalidActionCodeSettingsArgs))]
        public void InvalidActionCodeSettings(ActionCodeSettings settings)
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var email = "user@example.com";

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateEmailVerificationLinkAsync(email, settings));

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GeneratePasswordResetLinkAsync(email, settings));

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(email, settings));
        }

        [Fact]
        public async Task EmailVerificationLink()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var link = await auth.GenerateEmailVerificationLinkAsync("user@example.com");

            Assert.Equal("https://mock-oob-link.for.auth.tests", link);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(
                handler.LastRequestBody);
            Assert.Equal(3, request.Count);
            Assert.Equal("user@example.com", request["email"]);
            Assert.Equal("VERIFY_EMAIL", request["requestType"]);
            Assert.True((bool)request["returnOobLink"]);
            this.AssertRequest(handler.Requests[0]);
        }

        [Fact]
        public async Task EmailVerificationLinkWithSettings()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var link = await auth.GenerateEmailVerificationLinkAsync(
                "user@example.com", ActionCodeSettings);

            Assert.Equal("https://mock-oob-link.for.auth.tests", link);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(
                handler.LastRequestBody);
            Assert.Equal(10, request.Count);
            Assert.Equal("user@example.com", request["email"]);
            Assert.Equal("VERIFY_EMAIL", request["requestType"]);
            Assert.True((bool)request["returnOobLink"]);

            Assert.Equal(ActionCodeSettings.Url, request["continueUrl"]);
            Assert.True((bool)request["canHandleCodeInApp"]);
            Assert.Equal(ActionCodeSettings.DynamicLinkDomain, request["dynamicLinkDomain"]);
            Assert.Equal(ActionCodeSettings.IosBundleId, request["iOSBundleId"]);
            Assert.Equal(ActionCodeSettings.AndroidPackageName, request["androidPackageName"]);
            Assert.Equal(
                ActionCodeSettings.AndroidMinimumVersion, request["androidMinimumVersion"]);
            Assert.True((bool)request["androidInstallApp"]);
            this.AssertRequest(handler.Requests[0]);
        }

        [Fact]
        public async Task EmailVerificationLinkUnexpectedResponse()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GenerateEmailVerificationLinkAsync("user@example.com"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal(
                $"Failed to generate email action link for: user@example.com",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task PasswordResetLink()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var link = await auth.GeneratePasswordResetLinkAsync("user@example.com");

            Assert.Equal("https://mock-oob-link.for.auth.tests", link);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(
                handler.LastRequestBody);
            Assert.Equal(3, request.Count);
            Assert.Equal("user@example.com", request["email"]);
            Assert.Equal("PASSWORD_RESET", request["requestType"]);
            Assert.True((bool)request["returnOobLink"]);
            this.AssertRequest(handler.Requests[0]);
        }

        [Fact]
        public async Task PasswordResetLinkWithSettings()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var link = await auth.GeneratePasswordResetLinkAsync(
                "user@example.com", ActionCodeSettings);

            Assert.Equal("https://mock-oob-link.for.auth.tests", link);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(
                handler.LastRequestBody);
            Assert.Equal(10, request.Count);
            Assert.Equal("user@example.com", request["email"]);
            Assert.Equal("PASSWORD_RESET", request["requestType"]);
            Assert.True((bool)request["returnOobLink"]);

            Assert.Equal(ActionCodeSettings.Url, request["continueUrl"]);
            Assert.True((bool)request["canHandleCodeInApp"]);
            Assert.Equal(ActionCodeSettings.DynamicLinkDomain, request["dynamicLinkDomain"]);
            Assert.Equal(ActionCodeSettings.IosBundleId, request["iOSBundleId"]);
            Assert.Equal(ActionCodeSettings.AndroidPackageName, request["androidPackageName"]);
            Assert.Equal(
                ActionCodeSettings.AndroidMinimumVersion, request["androidMinimumVersion"]);
            Assert.True((bool)request["androidInstallApp"]);
            this.AssertRequest(handler.Requests[0]);
        }

        [Fact]
        public async Task PasswordResetLinkUnexpectedResponse()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GeneratePasswordResetLinkAsync("user@example.com"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal(
                $"Failed to generate email action link for: user@example.com",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public void SignInWithEmailLinkNoSettings()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var email = "user@example.com";

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(email, null));
        }

        [Fact]
        public async Task SignInWithEmailLink()
        {
            var handler = new MockMessageHandler() { Response = GenerateEmailLinkResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var link = await auth.GenerateSignInWithEmailLinkAsync(
                "user@example.com", ActionCodeSettings);

            Assert.Equal("https://mock-oob-link.for.auth.tests", link);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(
                handler.LastRequestBody);
            Assert.Equal(10, request.Count);
            Assert.Equal("user@example.com", request["email"]);
            Assert.Equal("EMAIL_SIGNIN", request["requestType"]);
            Assert.True((bool)request["returnOobLink"]);

            Assert.Equal(ActionCodeSettings.Url, request["continueUrl"]);
            Assert.True((bool)request["canHandleCodeInApp"]);
            Assert.Equal(ActionCodeSettings.DynamicLinkDomain, request["dynamicLinkDomain"]);
            Assert.Equal(ActionCodeSettings.IosBundleId, request["iOSBundleId"]);
            Assert.Equal(ActionCodeSettings.AndroidPackageName, request["androidPackageName"]);
            Assert.Equal(
                ActionCodeSettings.AndroidMinimumVersion, request["androidMinimumVersion"]);
            Assert.True((bool)request["androidInstallApp"]);
            this.AssertRequest(handler.Requests[0]);
        }

        [Fact]
        public async Task SignInWithEmailLinkUnexpectedResponse()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(
                    "user@example.com", ActionCodeSettings));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal(
                $"Failed to generate email action link for: user@example.com",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task InvalidDynamicLinkDomain()
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""INVALID_DYNAMIC_LINK_DOMAIN"",
                }}
            }}";
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = json,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GenerateSignInWithEmailLinkAsync(
                    "user@example.com", ActionCodeSettings));

            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.InvalidDynamicLinkDomain, exception.AuthErrorCode);
            Assert.Equal(
                "Dynamic link domain specified in ActionCodeSettings is not authorized "
                + "(INVALID_DYNAMIC_LINK_DOMAIN).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        private FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler)
        {
            var userManager = new FirebaseUserManager(new FirebaseUserManager.Args
            {
                Credential = GoogleCredential.FromAccessToken("test-token"),
                ProjectId = "project1",
                ClientFactory = new MockHttpClientFactory(handler),
                RetryOptions = RetryOptions.NoBackOff,
            });
            return new FirebaseAuth(new FirebaseAuth.FirebaseAuthArgs()
            {
                UserManager = new Lazy<FirebaseUserManager>(userManager),
                TokenFactory = new Lazy<FirebaseTokenFactory>(),
                IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(),
                SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(),
            });
        }

        private void AssertRequest(MockMessageHandler.IncomingRequest message)
        {
            Assert.Equal(message.Method, HttpMethod.Post);
            Assert.EndsWith("/accounts:sendOobCode", message.Url.PathAndQuery);
            Assert.Equal(
                FirebaseUserManager.ClientVersion,
                message.Headers.GetValues(FirebaseUserManager.ClientVersionHeader).First());
        }
    }
}
