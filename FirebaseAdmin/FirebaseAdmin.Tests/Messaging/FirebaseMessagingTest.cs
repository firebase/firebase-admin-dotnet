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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Xunit;

namespace FirebaseAdmin.Messaging.Tests
{
    public class FirebaseMessagingTest : IDisposable
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromFile("./resources/service_account.json");

        [Fact]
        public void GetMessagingWithoutApp()
        {
            Assert.Null(FirebaseMessaging.DefaultInstance);
        }

        [Fact]
        public void GetDefaultMessaging()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            FirebaseMessaging messaging = FirebaseMessaging.DefaultInstance;
            Assert.NotNull(messaging);
            Assert.Same(messaging, FirebaseMessaging.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseMessaging.DefaultInstance);
        }

        [Fact]
        public void GetMessaging()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential }, "MyApp");
            FirebaseMessaging messaging = FirebaseMessaging.GetMessaging(app);
            Assert.NotNull(messaging);
            Assert.Same(messaging, FirebaseMessaging.GetMessaging(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseMessaging.GetMessaging(app));
        }

        [Fact]
        public async Task GetMessagingWithClientFactory()
        {
            var handler = new MockMessageHandler()
            {
                Response = new FirebaseMessagingClient.SingleMessageResponse()
                {
                    Name = "test-response",
                },
            };
            var factory = new MockHttpClientFactory(handler);

            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("test-token"),
                HttpClientFactory = factory,
                ProjectId = "test-project",
            });
            FirebaseMessaging messaging = FirebaseMessaging.GetMessaging(app);
            Assert.NotNull(messaging);
            Assert.Same(messaging, FirebaseMessaging.GetMessaging(app));

            var response = await messaging.SendAsync(new Message() { Topic = "test-topic" });
            Assert.Equal("test-response", response);
            app.Delete();
        }

        [Fact]
        public async Task UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            FirebaseMessaging messaging = FirebaseMessaging.DefaultInstance;
            app.Delete();
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await messaging.SendAsync(new Message() { Topic = "test-topic" }));
        }

        [Fact]
        public async Task SendMessageCancel()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await FirebaseMessaging.DefaultInstance.SendAsync(
                    new Message() { Topic = "test-topic" }, canceller.Token));
        }

        [Fact]
        public async Task SendMessageCancelWithClientFactory()
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                HttpClientFactory = new HttpClientFactory(),
            });
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await FirebaseMessaging.DefaultInstance.SendAsync(
                    new Message() { Topic = "test-topic" }, canceller.Token));
        }

        [Fact]
        public async Task SubscribeWithClientFactory()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("test-token"),
                HttpClientFactory = factory,
                ProjectId = "test-project",
            });
            FirebaseMessaging messaging = FirebaseMessaging.GetMessaging(app);
            Assert.NotNull(messaging);
            Assert.Same(messaging, FirebaseMessaging.GetMessaging(app));

            var response = await messaging.SubscribeToTopicAsync(new List<string> { "test-token" }, "test-topic");
            Assert.Equal(0, response.FailureCount);
            Assert.Equal(1, response.SuccessCount);
            app.Delete();
        }

        [Fact]
        public async Task UnsubscribeWithClientFactory()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("test-token"),
                HttpClientFactory = factory,
                ProjectId = "test-project",
            });
            FirebaseMessaging messaging = FirebaseMessaging.GetMessaging(app);
            Assert.NotNull(messaging);
            Assert.Same(messaging, FirebaseMessaging.GetMessaging(app));

            var response = await messaging.UnsubscribeFromTopicAsync(new List<string> { "test-token" }, "test-topic");
            Assert.Equal(0, response.FailureCount);
            Assert.Equal(1, response.SuccessCount);
            app.Delete();
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}
