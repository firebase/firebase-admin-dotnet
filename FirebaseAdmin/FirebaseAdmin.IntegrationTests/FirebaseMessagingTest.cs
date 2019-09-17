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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseMessagingTest
    {
        public FirebaseMessagingTest()
        {
            IntegrationTestUtils.EnsureDefaultApp();
        }

        [Fact]
        public async Task Send()
        {
            var message = new Message()
            {
                Topic = "foo-bar",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                    ImageUrl = "https://example.com/image.png",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
            };
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendAll()
        {
            var message1 = new Message()
            {
                Topic = "foo-bar",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                    ImageUrl = "https://example.com/image.png",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
            };
            var message2 = new Message()
            {
                Topic = "fiz-buz",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
            };
            var response = await FirebaseMessaging.DefaultInstance.SendAllAsync(new[] { message1, message2 }, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(2, response.SuccessCount);
            Assert.True(!string.IsNullOrEmpty(response.Responses[0].MessageId));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[0].MessageId);
            Assert.True(!string.IsNullOrEmpty(response.Responses[1].MessageId));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[1].MessageId);
        }

        [Fact]
        public async Task SendMulticast()
        {
            var multicastMessage = new MulticastMessage
            {
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
                Tokens = new[]
                {
                    "token1",
                    "token2",
                },
            };
            var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(multicastMessage, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(2, response.FailureCount);
            Assert.NotNull(response.Responses[0].Exception);
            Assert.NotNull(response.Responses[1].Exception);
        }

        [Fact]
        public async Task SubscribeToTopic()
        {
            var response = await FirebaseMessaging.DefaultInstance.SubscribeToTopicAsync(
                new List<string> { "token1", "token2" }, "test-topic");
            Assert.NotNull(response);
            Assert.Equal(2, response.FailureCount);
            Assert.Equal("invalid-argument", response.Errors[0].Reason);
            Assert.Equal(0, response.Errors[0].Index);
            Assert.Equal("invalid-argument", response.Errors[1].Reason);
            Assert.Equal(1, response.Errors[1].Index);
        }

        [Fact]
        public async Task UnsubscribeFromTopic()
        {
            var response = await FirebaseMessaging.DefaultInstance.UnsubscribeFromTopicAsync(
                new List<string> { "token1", "token2" }, "test-topic");
            Assert.NotNull(response);
            Assert.Equal(2, response.FailureCount);
            Assert.Equal("invalid-argument", response.Errors[0].Reason);
            Assert.Equal(0, response.Errors[0].Index);
            Assert.Equal("invalid-argument", response.Errors[1].Reason);
            Assert.Equal(1, response.Errors[1].Index);
        }
    }
}
