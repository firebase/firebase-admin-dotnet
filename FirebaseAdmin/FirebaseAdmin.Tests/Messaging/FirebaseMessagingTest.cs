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
using Google.Apis.Json;
using FirebaseAdmin.Tests;

namespace FirebaseAdmin.Messaging.Tests
{
    public class FirebaseMessagingTest: IDisposable
    {
        private static readonly GoogleCredential mockCredential =
            GoogleCredential.FromFile("./resources/service_account.json");

        [Fact]
        public void GetMessagingWithoutApp()
        {
            Assert.Null(FirebaseMessaging.DefaultInstance);
        }

        [Fact]
        public void GetDefaultMessaging()
        {
            var app = FirebaseApp.Create(new AppOptions(){Credential = mockCredential});
            FirebaseMessaging messaging = FirebaseMessaging.DefaultInstance;
            Assert.Same(messaging, FirebaseMessaging.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseMessaging.DefaultInstance);
        }

        [Fact]
        public void GetMessaging()
        {
            var app = FirebaseApp.Create(new AppOptions(){Credential = mockCredential}, "MyApp");
            FirebaseMessaging messaging = FirebaseMessaging.GetMessaging(app);
            Assert.Same(messaging, FirebaseMessaging.GetMessaging(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseMessaging.GetMessaging(app));
        }

        [Fact]
        public async Task UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions(){Credential = mockCredential});
            FirebaseMessaging messaging = FirebaseMessaging.DefaultInstance;
            app.Delete();
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await messaging.SendAsync(new Message(){Topic = "test-topic"}));
        }

        [Fact]
        public async Task SendMessageCancel()
        {
            var cred = GoogleCredential.FromFile("./resources/service_account.json");
            FirebaseApp.Create(new AppOptions(){Credential = cred});
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await FirebaseMessaging.DefaultInstance.SendAsync(
                    new Message(){Topic = "test-topic"}, canceller.Token));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }   
}