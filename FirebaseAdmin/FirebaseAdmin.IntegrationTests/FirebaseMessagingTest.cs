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
#pragma warning disable CS0618
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                    DirectBootOk = true,
                    BandwidthConstrainedOk = true,
                    RestrictedSatelliteOk = true,
                },
#pragma warning restore CS0618
            };
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendEach()
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
#pragma warning disable CS0618
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                    DirectBootOk = false,
                    BandwidthConstrainedOk = false,
                    RestrictedSatelliteOk = false,
                },
#pragma warning restore CS0618
            };
            var message2 = new Message()
            {
                Topic = "fiz-buz",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
#pragma warning disable CS0618
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                    DirectBootOk = true,
                    BandwidthConstrainedOk = true,
                    RestrictedSatelliteOk = true,
                },
#pragma warning restore CS0618
            };
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(new[] { message1, message2 }, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(2, response.SuccessCount);
            Assert.True(!string.IsNullOrEmpty(response.Responses[0].MessageId));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[0].MessageId);
            Assert.True(!string.IsNullOrEmpty(response.Responses[1].MessageId));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), response.Responses[1].MessageId);
        }

        [Fact]
        public async Task SendEachForMulticast()
        {
            var multicastMessage = new MulticastMessage
            {
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
#pragma warning disable CS0618
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
#pragma warning restore CS0618
            };
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(2, response.FailureCount);
            Assert.NotNull(response.Responses[0].Exception);
            Assert.NotNull(response.Responses[1].Exception);
        }

        [Fact]
        public async Task SendEachForMulticastFids()
        {
            var multicastMessage = new MulticastMessage
            {
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
#pragma warning disable CS0618
                Android = new AndroidConfig()
                {
                    Priority = Priority.Normal,
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
#pragma warning restore CS0618
                Fids = new[]
                {
                    "fid1",
                    "fid2",
                },
            };
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(multicastMessage, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(2, response.FailureCount);
            Assert.Equal(MessagingErrorCode.Unregistered, response.Responses[0].Exception.MessagingErrorCode);
            Assert.Equal(MessagingErrorCode.Unregistered, response.Responses[1].Exception.MessagingErrorCode);
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

        [Fact]
        public async Task SendAndroidV2RemoteNotification()
        {
            var message = CreateAndroidV2RemoteNotificationMessage();
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendAndroidV2MinimalRemoteNotification()
        {
            var message = CreateAndroidV2MinimalRemoteNotificationMessage();
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendAndroidV2BackgroundSync()
        {
            var message = CreateAndroidV2BackgroundSyncMessage();
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendAndroidV2MinimalBackgroundSync()
        {
            var message = CreateAndroidV2MinimalBackgroundSyncMessage();
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }

        [Fact]
        public async Task SendEachAndroidV2()
        {
            var messages = new[]
            {
                CreateAndroidV2RemoteNotificationMessage(),
                CreateAndroidV2MinimalRemoteNotificationMessage(),
                CreateAndroidV2BackgroundSyncMessage(),
                CreateAndroidV2MinimalBackgroundSyncMessage(),
            };
            var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages, dryRun: true);
            Assert.NotNull(response);
            Assert.Equal(4, response.SuccessCount);
            Assert.Equal(0, response.FailureCount);
            Assert.Equal(4, response.Responses.Count);
            foreach (var sendResponse in response.Responses)
            {
                Assert.True(sendResponse.IsSuccess);
                Assert.Null(sendResponse.Exception);
                Assert.Matches(new Regex("^projects/.*/messages/.*$"), sendResponse.MessageId);
            }
        }

        private static AndroidNotificationV2 CreateFullAndroidNotificationV2()
        {
            return new AndroidNotificationV2()
            {
                Title = "Title",
                Body = "Body",
                Icon = "stock_ticker_update",
                Color = "#f45342",
                Sound = "default",
                Tag = "test-tag",
                ImageUrl = "https://example.com/image.png",
                ClickAction = "TOP_STORY_ACTIVITY",
                TitleLocKey = "title_loc_key",
                TitleLocArgs = new List<string>() { "title_arg1", "title_arg2" },
                BodyLocKey = "body_loc_key",
                BodyLocArgs = new List<string>() { "body_arg1", "body_arg2" },
                ChannelId = "test-channel-id",
                Ticker = "test-ticker",
                Sticky = true,
                EventTimestamp = new DateTime(2026, 7, 8, 18, 0, 0, DateTimeKind.Utc),
                LocalOnly = true,
                Priority = NotificationPriority.HIGH,
                VibrateTimingsMillis = new List<long>() { 100, 200, 300 },
                DefaultVibrateTimings = false,
                DefaultSound = true,
                LightSettings = new LightSettings()
                {
                    Color = "#1A73E8",
                    LightOnDurationMillis = 100,
                    LightOffDurationMillis = 200,
                },
                DefaultLightSettings = false,
                Visibility = NotificationVisibility.PUBLIC,
                NotificationCount = 42,
                Id = 42,
            };
        }

        private static Message CreateAndroidV2RemoteNotificationMessage()
        {
            return new Message()
            {
                Topic = "foo-bar",
                AndroidV2 = new AndroidConfigV2()
                {
                    CollapseKey = "test-collapse-key",
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                    Data = new Dictionary<string, string>()
                    {
                        { "k1", "v1" },
                        { "k2", "v2" },
                    },
                    DirectBootOk = true,
                    BandwidthConstrainedOk = true,
                    RestrictedSatelliteOk = true,
                    FcmOptions = new AndroidFcmOptions()
                    {
                        AnalyticsLabel = "test-analytics-label",
                    },
                    RemoteNotification = new AndroidRemoteNotification()
                    {
                        MutableContent = true,
                        UseAsV1DataMessage = false,
                        Notification = CreateFullAndroidNotificationV2(),
                    },
                },
            };
        }

        private static Message CreateAndroidV2MinimalRemoteNotificationMessage()
        {
            return new Message()
            {
                Topic = "foo-bar",
                AndroidV2 = new AndroidConfigV2()
                {
                    RemoteNotification = new AndroidRemoteNotification()
                    {
                        Notification = new AndroidNotificationV2()
                        {
                            Title = "Title",
                            Body = "Body",
                        },
                    },
                },
            };
        }

        private static Message CreateAndroidV2BackgroundSyncMessage()
        {
            return new Message()
            {
                Topic = "foo-bar",
                AndroidV2 = new AndroidConfigV2()
                {
                    CollapseKey = "test-collapse-key",
                    TimeToLive = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                    Data = new Dictionary<string, string>()
                    {
                        { "k1", "v1" },
                        { "k2", "v2" },
                    },
                    DirectBootOk = true,
                    BandwidthConstrainedOk = true,
                    RestrictedSatelliteOk = true,
                    FcmOptions = new AndroidFcmOptions()
                    {
                        AnalyticsLabel = "test-analytics-label",
                    },
                    BackgroundSync = new AndroidBackgroundSyncMessage(),
                },
            };
        }

        private static Message CreateAndroidV2MinimalBackgroundSyncMessage()
        {
            return new Message()
            {
                Topic = "foo-bar",
                AndroidV2 = new AndroidConfigV2()
                {
                    BackgroundSync = new AndroidBackgroundSyncMessage(),
                },
            };
        }
    }
}
