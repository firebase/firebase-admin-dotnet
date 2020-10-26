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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
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

        private static readonly string VersionHeader =
            $"X-Firebase-Client: {FirebaseMessagingClient.ClientVersion}";

        private static readonly string ApiFormatHeader =
            "X-GOOG-API-FORMAT-VERSION: 2";

        [Fact]
        public void NoProjectId()
        {
            var args = new FirebaseMessagingClient.Args()
            {
                ClientFactory = new HttpClientFactory(),
                Credential = null,
            };

            args.ProjectId = null;
            Assert.Throws<ArgumentException>(() => new FirebaseMessagingClient(args));

            args.ProjectId = string.Empty;
            Assert.Throws<ArgumentException>(() => new FirebaseMessagingClient(args));
        }

        [Fact]
        public void NoCredential()
        {
            var args = new FirebaseMessagingClient.Args()
            {
                ClientFactory = new HttpClientFactory(),
                Credential = null,
                ProjectId = "test-project",
            };
            Assert.Throws<ArgumentNullException>(() => new FirebaseMessagingClient(args));
        }

        [Fact]
        public void NoClientFactory()
        {
            var clientFactory = new HttpClientFactory();
            var args = new FirebaseMessagingClient.Args()
            {
                ClientFactory = null,
                Credential = MockCredential,
                ProjectId = "test-project",
            };
            Assert.Throws<ArgumentNullException>(() => new FirebaseMessagingClient(args));
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
            var client = this.CreateMessagingClient(factory);
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
            this.CheckHeaders(handler.LastRequestHeaders);
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
            var client = this.CreateMessagingClient(factory);
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
            this.CheckHeaders(handler.LastRequestHeaders);
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
            var client = this.CreateMessagingClient(factory);
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

            var userAgent = handler.LastRequestHeaders.UserAgent.First();
            Assert.Equal("fire-admin-dotnet", userAgent.Product.Name);
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, VersionHeader));
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, ApiFormatHeader));
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
    ""details"": [
        {
            ""@type"": ""type.googleapis.com/google.firebase.fcm.v1.FcmError"",
            ""errorCode"": ""UNREGISTERED""
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
            var client = this.CreateMessagingClient(factory);
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

            var exception = response.Responses[1].Exception;
            Assert.NotNull(exception);
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal("The registration token is not a valid FCM registration token", exception.Message);
            Assert.Equal(MessagingErrorCode.Unregistered, exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);

            Assert.Equal(1, handler.Calls);
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, VersionHeader));
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, ApiFormatHeader));
        }

        [Fact]
        public async Task SendAllAsyncWithErrorNoDetail()
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

{
  ""error"": {
    ""code"": 400,
    ""message"": ""The registration token is not a valid FCM registration token"",
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
            var client = new FirebaseMessagingClient(new FirebaseMessagingClient.Args()
            {
                ClientFactory = factory,
                Credential = MockCredential,
                ProjectId = "test-project",
            });
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

            var exception = response.Responses[1].Exception;
            Assert.NotNull(exception);
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal("The registration token is not a valid FCM registration token", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);

            Assert.Equal(1, handler.Calls);
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, VersionHeader));
            Assert.Equal(2, this.CountLinesWithPrefix(handler.LastRequestBody, ApiFormatHeader));
        }

        [Fact]
        public async Task SendAllAsyncNullList()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = this.CreateMessagingClient(factory);

            await Assert.ThrowsAsync<ArgumentNullException>(() => client.SendAllAsync(null));
        }

        [Fact]
        public async Task SendAllAsyncWithNoMessages()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = this.CreateMessagingClient(factory);
            await Assert.ThrowsAsync<ArgumentException>(() => client.SendAllAsync(Enumerable.Empty<Message>()));
        }

        [Fact]
        public async Task SendAllAsyncWithTooManyMessages()
        {
            var factory = new MockHttpClientFactory(new MockMessageHandler());
            var client = this.CreateMessagingClient(factory);
            var messages = Enumerable.Range(0, 501).Select(_ => new Message { Topic = "test-topic" });
            await Assert.ThrowsAsync<ArgumentException>(() => client.SendAllAsync(messages));
        }

        [Fact]
        public async Task HttpErrorAsync()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {
                        ""status"": ""PERMISSION_DENIED"",
                        ""message"": ""test error"",
                        ""details"": [
                            {
                                ""@type"": ""type.googleapis.com/google.firebase.fcm.v1.FcmError"",
                                ""errorCode"": ""UNREGISTERED""
                            }
                        ]
                    }
                }",
            };
            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateMessagingClient(factory);
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var ex = await Assert.ThrowsAsync<FirebaseMessagingException>(
                async () => await client.SendAsync(message));

            Assert.Equal(ErrorCode.PermissionDenied, ex.ErrorCode);
            Assert.Equal("test error", ex.Message);
            Assert.Equal(MessagingErrorCode.Unregistered, ex.MessagingErrorCode);
            Assert.NotNull(ex.HttpResponse);

            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(
                handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task HttpErrorNoDetailAsync()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {
                        ""status"": ""PERMISSION_DENIED"",
                        ""message"": ""test error""
                    }
                }",
            };
            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateMessagingClient(factory);
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var ex = await Assert.ThrowsAsync<FirebaseMessagingException>(
                async () => await client.SendAsync(message));

            Assert.Equal(ErrorCode.PermissionDenied, ex.ErrorCode);
            Assert.Equal("test error", ex.Message);
            Assert.Null(ex.MessagingErrorCode);
            Assert.NotNull(ex.HttpResponse);

            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(
                handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task HttpErrorNonJsonAsync()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "not json",
            };
            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateMessagingClient(factory);
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var ex = await Assert.ThrowsAsync<FirebaseMessagingException>(
                async () => await client.SendAsync(message));

            Assert.Equal(ErrorCode.Internal, ex.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}not json",
                ex.Message);
            Assert.Null(ex.MessagingErrorCode);
            Assert.NotNull(ex.HttpResponse);

            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(
                handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task Unavailable()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "ServiceUnavailable",
            };
            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateMessagingClient(factory);
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var ex = await Assert.ThrowsAsync<FirebaseMessagingException>(
                async () => await client.SendAsync(message));

            Assert.Equal(ErrorCode.Unavailable, ex.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}ServiceUnavailable",
                ex.Message);
            Assert.Null(ex.MessagingErrorCode);
            Assert.NotNull(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public async Task TransportError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Transport error"),
            };
            var factory = new MockHttpClientFactory(handler);
            var client = this.CreateMessagingClient(factory);
            var message = new Message()
            {
                Topic = "test-topic",
            };

            var ex = await Assert.ThrowsAsync<FirebaseMessagingException>(
                async () => await client.SendAsync(message));

            Assert.Equal(ErrorCode.Unknown, ex.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: Transport error",
                ex.Message);
            Assert.Null(ex.MessagingErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.Same(handler.Exception, ex.InnerException);

            var req = JsonConvert.DeserializeObject<FirebaseMessagingClient.SendRequest>(
                handler.LastRequestBody);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(5, handler.Calls);
        }

        private FirebaseMessagingClient CreateMessagingClient(HttpClientFactory factory)
        {
            return new FirebaseMessagingClient(new FirebaseMessagingClient.Args()
            {
                ClientFactory = factory,
                Credential = MockCredential,
                ProjectId = "test-project",
                RetryOptions = RetryOptions.NoBackOff,
            });
        }

        private void CheckHeaders(HttpRequestHeaders header)
        {
            var versionHeader = header.GetValues("X-Firebase-Client").First();
            Assert.Equal(FirebaseMessagingClient.ClientVersion, versionHeader);

            var apiFormatHeader = header.GetValues("X-GOOG-API-FORMAT-VERSION").First();
            Assert.Equal("2", apiFormatHeader);
        }

        private int CountLinesWithPrefix(string body, string linePrefix)
        {
            return body.Split('\n').Count((line) => line.StartsWith(linePrefix));
        }
    }
}
