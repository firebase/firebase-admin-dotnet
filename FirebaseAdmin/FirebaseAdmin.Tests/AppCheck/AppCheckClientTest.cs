// Copyright 2026, Google Inc. All rights reserved.
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
using FirebaseAdmin.AppCheck;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckClientTest
    {
        private const string ProjectId = "test-project";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-access-token");

        [Fact]
        public async Task SendsCorrectRequest()
        {
            var handler = new MockMessageHandler() { Response = @"{""alreadyConsumed"": false}" };
            var client = new AppCheckClient(
                new MockHttpClientFactory(handler), MockCredential, ProjectId);

            await client.VerifyReplayProtectionAsync("app-check-token");

            var request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal(
                "https://firebaseappcheck.googleapis.com/v1beta/projects/test-project:verifyAppCheckToken",
                request.Url.ToString());
            Assert.Equal(
                "Bearer test-access-token", request.Headers.GetValues("Authorization").Single());
            var body = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, string>>(
                request.Body);
            Assert.Equal("app-check-token", Assert.Single(body, kv => kv.Key == "app_check_token").Value);
        }

        [Theory]
        [InlineData(@"{""alreadyConsumed"": true}", true)]
        [InlineData(@"{""alreadyConsumed"": false}", false)]
        [InlineData("{}", false)]
        public async Task AlreadyConsumed(string response, bool expected)
        {
            var handler = new MockMessageHandler() { Response = response };
            var client = new AppCheckClient(
                new MockHttpClientFactory(handler), MockCredential, ProjectId);

            var alreadyConsumed = await client.VerifyReplayProtectionAsync("app-check-token");

            Assert.Equal(expected, alreadyConsumed);
        }

        [Fact]
        public async Task HttpErrorIsNotRetried()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var client = new AppCheckClient(
                new MockHttpClientFactory(handler), MockCredential, ProjectId);

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => client.VerifyReplayProtectionAsync("app-check-token"));

            Assert.Equal(ErrorCode.Unavailable, exception.ErrorCode);
            Assert.Equal(AppCheckErrorCode.ServiceError, exception.AppCheckErrorCode);
            Assert.Equal(1, handler.Calls);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void NoProjectId(string projectId)
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());

            Assert.Throws<ArgumentException>(
                () => new AppCheckClient(factory, MockCredential, projectId));
        }
    }
}
