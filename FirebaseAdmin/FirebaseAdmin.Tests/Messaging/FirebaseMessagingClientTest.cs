// Copyright 2018, Google Inc. All rights reserved.
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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.Messaging.Tests
{
    public class FirebaseMessagingClientTest
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void NoProjectId()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<FirebaseException>(
                () => new FirebaseMessagingClient(clientFactory, MockCredential, null));
            Assert.Throws<FirebaseException>(
                () => new FirebaseMessagingClient(clientFactory, MockCredential, string.Empty));
        }

        [Fact]
        public void NoCredential()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new FirebaseMessagingClient(clientFactory, null, "test-project"));
        }

        [Fact]
        public void NoClientFactory()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new FirebaseMessagingClient(null, MockCredential, "test-project"));
        }

        [Fact]
        public async Task SendAsync()
        {
            var handler = new MockMessageHandler()
            {
                Response = new FirebaseMessagingClient.SingleMessageResponse()
                {
                    Name = "test-response",
                },
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var response = await client.SendAsync(message);

            Assert.Equal("test-response", response);
            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
            var versionHeader = handler.LastRequestHeaders.GetValues("X-Firebase-Client").First();
            Assert.Equal(FirebaseMessagingClient.ClientVersion, versionHeader);
        }

        [Fact]
        public async Task SendDryRunAsync()
        {
            var handler = new MockMessageHandler()
            {
                Response = new FirebaseMessagingClient.SingleMessageResponse()
                {
                    Name = "test-response",
                },
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var response = await client.SendAsync(message, dryRun: true);

            Assert.Equal("test-response", response);
            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.True(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
            var versionHeader = handler.LastRequestHeaders.GetValues("X-Firebase-Client").First();
            Assert.Equal(FirebaseMessagingClient.ClientVersion, versionHeader);
        }

        [Fact]
        public async Task SendAllAsync()
        {
            var rawResponse = @"
--batch_test-boundary
Content-Type: application/http
Content-ID: response-

HTTP/1.1 200 OK
Content-Type: application/json; charset=UTF-8
Vary: Origin
Vary: X-Origin
Vary: Referer

{
  ""name"": ""projects/fir-adminintegrationtests/messages/8580920590356323124""
}

--batch_test-boundary
Content-Type: application/http
Content-ID: response-

HTTP/1.1 200 OK
Content-Type: application/json; charset=UTF-8
Vary: Origin
Vary: X-Origin
Vary: Referer

{
  ""name"": ""projects/fir-adminintegrationtests/messages/5903525881088369386""
}

--batch_test-boundary
";
            var handler = new MockMessageHandler()
            {
                Response = rawResponse,
                ApplyHeaders = (_, headers) =>
                {
                    headers.Remove("Content-Type");
                    headers.TryAddWithoutValidation("Content-Type", "multipart/mixed; boundary=batch_test-boundary");
                },
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var message1 = new Message()
            {
                Token = "test-token1",
            };
            var message2 = new Message()
            {
                Token = "test-token2",
            };

            var response = await client.SendAllAsync(new[] { message1, message2 });

            Assert.Equal(2, response.SuccessCount);
            Assert.Equal("projects/fir-adminintegrationtests/messages/8580920590356323124", response.Responses[0].MessageId);
            Assert.Equal("projects/fir-adminintegrationtests/messages/5903525881088369386", response.Responses[1].MessageId);
            Assert.Equal(1, handler.Calls);
            var versionHeader = $"X-Firebase-Client: {FirebaseMessagingClient.ClientVersion}";
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, versionHeader));
        }

        [Fact]
        public async Task SendAllAsyncWithError()
        {
            var rawResponse = @"
--batch_test-boundary
Content-Type: application/http
Content-ID: response-

HTTP/1.1 200 OK
Content-Type: application/json; charset=UTF-8
Vary: Origin
Vary: X-Origin
Vary: Referer

{
  ""name"": ""projects/fir-adminintegrationtests/messages/8580920590356323124""
}

--batch_test-boundary
Content-Type: application/http
Content-ID: response-

HTTP/1.1 400 Bad Request
Content-Type: application/json; charset=UTF-8
Vary: Origin
Vary: X-Origin
Vary: Referer

{
  ""error"": {
    ""code"": 400,
    ""message"": ""The registration token is not a valid FCM registration token"",
    ""errors"": [
      {
        ""message"": ""The registration token is not a valid FCM registration token"",
        ""domain"": ""global"",
        ""reason"": ""badRequest""
      }
    ],
    ""status"": ""INVALID_ARGUMENT""
  }
}

--batch_test-boundary
";
            var handler = new MockMessageHandler()
            {
                Response = rawResponse,
                ApplyHeaders = (_, headers) =>
                {
                    headers.Remove("Content-Type");
                    headers.TryAddWithoutValidation("Content-Type", "multipart/mixed; boundary=batch_test-boundary");
                },
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var message1 = new Message()
            {
                Token = "test-token1",
            };
            var message2 = new Message()
            {
                Token = "test-token2",
            };

            var response = await client.SendAllAsync(new[] { message1, message2 });

            Assert.Equal(1, response.SuccessCount);
            Assert.Equal(1, response.FailureCount);
            Assert.Equal("projects/fir-adminintegrationtests/messages/8580920590356323124", response.Responses[0].MessageId);
            Assert.NotNull(response.Responses[1].Exception);
            Assert.Equal(1, handler.Calls);
            var versionHeader = $"X-Firebase-Client: {FirebaseMessagingClient.ClientVersion}";
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, versionHeader));
        }

        [Fact]
        public async Task SendAllAsyncNullList()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");

            await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAllAsync(null));
        }

        [Fact]
        public async Task SendAllAsyncWithNoMessages()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            await Assert.ThrowsAsync<ArgumentException>(() => client.SendAllAsync(Enumerable.Empty<Message>()));
        }

        [Fact]
        public async Task SendAllAsyncWithTooManyMessages()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var messages = Enumerable.Range(0, 101).Select(_ => new Message { Topic = "test-topic" });
            await Assert.ThrowsAsync<ArgumentException>(() => client.SendAllAsync(messages));
        }

        [Fact]
        public async Task HttpErrorAsync()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "not json",
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, MockCredential, "test-project");
            var message = new Message()
            {
                Topic = "test-topic",
            };
            var ex = await Assert.ThrowsAsync<FirebaseException>(
                async () => await client.SendAsync(message));
            Assert.Contains("not json", ex.Message);
            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(
                handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
        }

        private int CountLinesWithPrefix(string body, string linePrefix)
        {
            return body.Split('\n').Count((line) => line.StartsWith(linePrefix));
        }
    }
}
