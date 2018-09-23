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
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using FirebaseAdmin.Tests;

namespace FirebaseAdmin.Messaging.Tests
{
    public class FirebaseMessagingClientTest
    {
        private static readonly GoogleCredential mockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void NoProjectId()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<FirebaseException>(() => new FirebaseMessagingClient(clientFactory, mockCredential, null));
            Assert.Throws<FirebaseException>(() => new FirebaseMessagingClient(clientFactory, mockCredential, ""));
        }

        [Fact]
        public void NoCredential()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(() => new FirebaseMessagingClient(clientFactory, null, "test-project"));
        }

        [Fact]
        public void NoClientFactory()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(() => new FirebaseMessagingClient(null, mockCredential, "test-project"));
        }

        [Fact]
        public async Task Send()
        {
            var handler = new MockMessageHandler()
            {
                Response = new SendResponse()
                {
                        Name = "test-response",
                },
            };
            var factory = new MockHttpClientFactory(handler);
            var client = new FirebaseMessagingClient(factory, mockCredential, "test-project");
            var message = new Message()
            {
                Topic = "test-topic"
            };
            var response = await client.SendAsync(message, false, default(CancellationToken));
            Assert.Equal("test-response", response);
            var req = NewtonsoftJsonSerializer.Instance.Deserialize<SendRequest>(handler.Request);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.False(req.ValidateOnly);
            Assert.Equal(1, handler.Calls);

            // Send in dryRun mode.
            response = await client.SendAsync(message, true, default(CancellationToken));
            Assert.Equal("test-response", response);
            req = NewtonsoftJsonSerializer.Instance.Deserialize<SendRequest>(handler.Request);
            Assert.Equal("test-topic", req.Message.Topic);
            Assert.True(req.ValidateOnly);
            Assert.Equal(2, handler.Calls);
        }
    }

    internal sealed class SendRequest
    {
        [Newtonsoft.Json.JsonProperty("message")]
        public Message Message { get; set; }

        [Newtonsoft.Json.JsonProperty("validate_only")]
        public bool ValidateOnly { get; set; }
    }
}