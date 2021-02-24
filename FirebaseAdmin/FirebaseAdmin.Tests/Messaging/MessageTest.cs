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
using System.Linq;
using Google.Apis.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Messaging.Tests
{
    public class MessageTest
    {
        [Fact]
        public void EmptyMessage()
        {
            var message = new Message() { Token = "test-token" };
            this.AssertJsonEquals(new JObject() { { "token", "test-token" } }, message);

            message = new Message() { Topic = "test-topic" };
            this.AssertJsonEquals(new JObject() { { "topic", "test-topic" } }, message);

            message = new Message() { Condition = "test-condition" };
            this.AssertJsonEquals(new JObject() { { "condition", "test-condition" } }, message);
        }

        [Fact]
        public void PrefixedTopicName()
        {
            var message = new Message() { Topic = "/topics/test-topic" };
            this.AssertJsonEquals(new JObject() { { "topic", "test-topic" } }, message);
        }

        [Fact]
        public void DataMessage()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Data = new Dictionary<string, string>()
                {
                    { "k1", "v1" },
                    { "k2", "v2" },
                },
            };
            this.AssertJsonEquals(
                new JObject()
                {
                    { "topic", "test-topic" },
                    { "data", new JObject() { { "k1", "v1" }, { "k2", "v2" } } },
                }, message);
        }

        [Fact]
        public void Notification()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Notification = new Notification()
                {
                    Title = "title",
                    Body = "body",
                    ImageUrl = "https://example.com/image.png",
                },
                FcmOptions = new FcmOptions()
                {
                    AnalyticsLabel = "label",
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "notification", new JObject()
                    {
                        { "title", "title" },
                        { "body", "body" },
                        { "image", "https://example.com/image.png" },
                    }
                },
                {
                    "fcm_options", new JObject()
                    {
                        { "analytics_label", "label" },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void MessageDeserialization()
        {
            var original = new Message()
            {
                Topic = "test-topic",
                Data = new Dictionary<string, string>() { { "key", "value" } },
                Notification = new Notification()
                {
                    Title = "title",
                    Body = "body",
                },
                Android = new AndroidConfig()
                {
                    RestrictedPackageName = "test-pkg-name",
                },
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        AlertString = "test-alert",
                    },
                },
                Webpush = new WebpushConfig()
                {
                    Data = new Dictionary<string, string>() { { "key", "value" } },
                },
                FcmOptions = new FcmOptions()
                {
                    AnalyticsLabel = "label",
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<Message>(json);
            Assert.Equal(original.Topic, copy.Topic);
            Assert.Equal(original.Data, copy.Data);
            Assert.Equal(original.Notification.Title, copy.Notification.Title);
            Assert.Equal(original.Notification.Body, copy.Notification.Body);
            Assert.Equal(
                original.Android.RestrictedPackageName, copy.Android.RestrictedPackageName);
            Assert.Equal(original.Apns.Aps.AlertString, copy.Apns.Aps.AlertString);
            Assert.Equal(original.Webpush.Data, copy.Webpush.Data);
            Assert.Equal(original.FcmOptions.AnalyticsLabel, copy.FcmOptions.AnalyticsLabel);
        }

        [Fact]
        public void MessageCopy()
        {
            var original = new Message()
            {
                Topic = "test-topic",
                Data = new Dictionary<string, string>(),
                Notification = new Notification(),
                Android = new AndroidConfig(),
                Apns = new ApnsConfig(),
                Webpush = new WebpushConfig(),
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.Data, copy.Data);
            Assert.NotSame(original.Notification, copy.Notification);
            Assert.NotSame(original.Android, copy.Android);
            Assert.NotSame(original.Apns, copy.Apns);
            Assert.NotSame(original.Webpush, copy.Webpush);
        }

        [Fact]
        public void MessageWithoutTarget()
        {
            Assert.Throws<ArgumentException>(() => new Message().CopyAndValidate());
        }

        [Fact]
        public void MultipleTargets()
        {
            var message = new Message()
            {
                Token = "test-token",
                Topic = "test-topic",
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());

            message = new Message()
            {
                Token = "test-token",
                Condition = "test-condition",
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());

            message = new Message()
            {
                Condition = "test-condition",
                Topic = "test-topic",
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());

            message = new Message()
            {
                Token = "test-token",
                Topic = "test-topic",
                Condition = "test-condition",
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void InvalidTopicNames()
        {
            var topics = new List<string>()
            {
                "/topics/", "/foo/bar", "foo bar",
            };
            foreach (var topic in topics)
            {
                var message = new Message() { Topic = topic };
                Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
            }
        }

        [Fact]
        public void NotificationInvalidImageUrls()
        {
            var imageUrls = new List<string>()
            {
                string.Empty, "image.png", "invalid-image", "foo bar",
            };
            foreach (var imageUrl in imageUrls)
            {
                var message = new Message()
                {
                    Topic = "test-topic",
                    Notification = new Notification()
                    {
                        ImageUrl = imageUrl,
                    },
                };

                Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
            }
        }

        [Fact]
        public void AndroidConfig()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    CollapseKey = "collapse-key",
                    Priority = Priority.High,
                    TimeToLive = TimeSpan.FromMilliseconds(10),
                    RestrictedPackageName = "test-pkg-name",
                    Data = new Dictionary<string, string>()
                    {
                        { "k1", "v1" },
                        { "k2", "v2" },
                    },
                    Notification = new AndroidNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                        Color = "#112233",
                        Sound = "sound",
                        Tag = "tag",
                        ImageUrl = "https://example.com/image.png",
                        ClickAction = "click-action",
                        TitleLocKey = "title-loc-key",
                        TitleLocArgs = new List<string>() { "arg1", "arg2" },
                        BodyLocKey = "body-loc-key",
                        BodyLocArgs = new List<string>() { "arg3", "arg4" },
                        ChannelId = "channel-id",
                    },
                    FcmOptions = new AndroidFcmOptions()
                    {
                        AnalyticsLabel = "label",
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "android", new JObject()
                    {
                        { "collapse_key", "collapse-key" },
                        { "priority", "high" },
                        { "ttl", "0.010000000s" },
                        { "restricted_package_name", "test-pkg-name" },
                        { "data", new JObject() { { "k1", "v1" }, { "k2", "v2" } } },
                        {
                            "notification", new JObject()
                            {
                                { "title", "title" },
                                { "body", "body" },
                                { "icon", "icon" },
                                { "color", "#112233" },
                                { "sound", "sound" },
                                { "tag", "tag" },
                                { "image", "https://example.com/image.png" },
                                { "click_action", "click-action" },
                                { "title_loc_key", "title-loc-key" },
                                { "title_loc_args", new JArray() { "arg1", "arg2" } },
                                { "body_loc_key", "body-loc-key" },
                                { "body_loc_args", new JArray() { "arg3", "arg4" } },
                                { "channel_id", "channel-id" },
                            }
                        },
                        {
                            "fcm_options", new JObject()
                            {
                                { "analytics_label", "label" },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void AndroidConfigMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig(),
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                { "android", new JObject() },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void AndroidConfigFullSecondsTTL()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    TimeToLive = TimeSpan.FromHours(1),
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "android", new JObject()
                    {
                        { "ttl", "3600s" },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void AndroidConfigDeserialization()
        {
            var original = new AndroidConfig()
            {
                CollapseKey = "collapse-key",
                RestrictedPackageName = "test-pkg-name",
                TimeToLive = TimeSpan.FromSeconds(10.5),
                Priority = Priority.High,
                Data = new Dictionary<string, string>()
                {
                    { "key", "value" },
                },
                Notification = new AndroidNotification()
                {
                    Title = "title",
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<AndroidConfig>(json);
            Assert.Equal(original.CollapseKey, copy.CollapseKey);
            Assert.Equal(original.RestrictedPackageName, copy.RestrictedPackageName);
            Assert.Equal(original.Priority, copy.Priority);
            Assert.Equal(original.TimeToLive, copy.TimeToLive);
            Assert.Equal(original.Data, copy.Data);
            Assert.Equal(original.Notification.Title, copy.Notification.Title);
        }

        [Fact]
        public void AndroidConfigCopy()
        {
            var original = new AndroidConfig()
            {
                Data = new Dictionary<string, string>(),
                Notification = new AndroidNotification(),
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.Data, copy.Data);
            Assert.NotSame(original.Notification, copy.Notification);
        }

        [Fact]
        public void AndroidNotificationDeserialization()
        {
            var original = new AndroidNotification()
            {
                Title = "title",
                Body = "body",
                Icon = "icon",
                Color = "#112233",
                Sound = "sound",
                Tag = "tag",
                ImageUrl = "https://example.com/image.png",
                ClickAction = "click-action",
                TitleLocKey = "title-loc-key",
                TitleLocArgs = new List<string>() { "arg1", "arg2" },
                BodyLocKey = "body-loc-key",
                BodyLocArgs = new List<string>() { "arg3", "arg4" },
                ChannelId = "channel-id",
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<AndroidNotification>(json);
            Assert.Equal(original.Title, copy.Title);
            Assert.Equal(original.Body, copy.Body);
            Assert.Equal(original.Icon, copy.Icon);
            Assert.Equal(original.Color, copy.Color);
            Assert.Equal(original.Sound, copy.Sound);
            Assert.Equal(original.Tag, copy.Tag);
            Assert.Equal(original.ImageUrl, copy.ImageUrl);
            Assert.Equal(original.ClickAction, copy.ClickAction);
            Assert.Equal(original.TitleLocKey, copy.TitleLocKey);
            Assert.Equal(original.TitleLocArgs, copy.TitleLocArgs);
            Assert.Equal(original.BodyLocKey, copy.BodyLocKey);
            Assert.Equal(original.BodyLocArgs, copy.BodyLocArgs);
            Assert.Equal(original.ChannelId, copy.ChannelId);
        }

        [Fact]
        public void AndroidNotificationCopy()
        {
            var original = new AndroidNotification()
            {
                TitleLocKey = "title-loc-key",
                TitleLocArgs = new List<string>() { "arg1", "arg2" },
                BodyLocKey = "body-loc-key",
                BodyLocArgs = new List<string>() { "arg3", "arg4" },
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.TitleLocArgs, copy.TitleLocArgs);
            Assert.NotSame(original.BodyLocArgs, copy.BodyLocArgs);
        }

        [Fact]
        public void AndroidConfigInvalidTTL()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    TimeToLive = TimeSpan.FromHours(-1),
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void AndroidNotificationInvalidColor()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        Color = "not-a-color",
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void AndroidNotificationInvalidImageUrls()
        {
            var imageUrls = new List<string>()
            {
                string.Empty, "image.png", "invalid-image", "foo bar",
            };
            foreach (var imageUrl in imageUrls)
            {
                var message = new Message()
                {
                    Topic = "test-topic",
                    Android = new AndroidConfig()
                    {
                        Notification = new AndroidNotification()
                        {
                            ImageUrl = imageUrl,
                        },
                    },
                };
                Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
            }
        }

        [Fact]
        public void AndroidNotificationInvalidTitleLocArgs()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        TitleLocArgs = new List<string>() { "arg" },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void AndroidNotificationInvalidBodyLocArgs()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    Notification = new AndroidNotification()
                    {
                        BodyLocArgs = new List<string>() { "arg" },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void WebpushConfig()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "header1", "header-value1" },
                        { "header2", "header-value2" },
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "key1", "value1" },
                        { "key2", "value2" },
                    },
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                        Badge = "badge",
                        Data = new Dictionary<string, object>()
                        {
                            { "some", "data" },
                        },
                        Direction = Direction.LeftToRight,
                        Image = "image",
                        Language = "language",
                        Tag = "tag",
                        Silent = true,
                        RequireInteraction = true,
                        Renotify = true,
                        TimestampMillis = 100,
                        Vibrate = new int[] { 10, 5, 10 },
                        Actions = new List<Action>()
                        {
                            new Action()
                            {
                                ActionName = "Accept",
                                Title = "Ok",
                                Icon = "ok-button",
                            },
                            new Action()
                            {
                                ActionName = "Reject",
                                Title = "Cancel",
                                Icon = "cancel-button",
                            },
                        },
                        CustomData = new Dictionary<string, object>()
                        {
                            { "custom-key1", "custom-data" },
                            { "custom-key2", true },
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "webpush", new JObject()
                    {
                        {
                            "headers", new JObject()
                            {
                                { "header1", "header-value1" },
                                { "header2", "header-value2" },
                            }
                        },
                        {
                            "data", new JObject()
                            {
                                { "key1", "value1" },
                                { "key2", "value2" },
                            }
                        },
                        {
                            "notification", new JObject()
                            {
                                { "title", "title" },
                                { "body", "body" },
                                { "icon", "icon" },
                                { "badge", "badge" },
                                {
                                    "data", new JObject()
                                    {
                                        { "some", "data" },
                                    }
                                },
                                { "dir", "ltr" },
                                { "image", "image" },
                                { "lang", "language" },
                                { "renotify", true },
                                { "requireInteraction", true },
                                { "silent", true },
                                { "tag", "tag" },
                                { "timestamp", 100 },
                                { "vibrate", new JArray() { 10, 5, 10 } },
                                {
                                    "actions", new JArray()
                                    {
                                        new JObject()
                                        {
                                            { "action", "Accept" },
                                            { "title", "Ok" },
                                            { "icon", "ok-button" },
                                        },
                                        new JObject()
                                        {
                                            { "action", "Reject" },
                                            { "title", "Cancel" },
                                            { "icon", "cancel-button" },
                                        },
                                    }
                                },
                                { "custom-key1", "custom-data" },
                                { "custom-key2", true },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void WebpushConfigMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig(),
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                { "webpush", new JObject() },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void WebpushConfigMinimalNotification()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "webpush", new JObject()
                    {
                        {
                            "notification", new JObject()
                            {
                                { "title", "title" },
                                { "body", "body" },
                                { "icon", "icon" },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void WebpushConfigDeserialization()
        {
            var original = new WebpushConfig()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "header1", "header-value1" },
                    { "header2", "header-value2" },
                },
                Data = new Dictionary<string, string>()
                {
                    { "key1", "value1" },
                    { "key2", "value2" },
                },
                Notification = new WebpushNotification()
                {
                    Title = "title",
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<WebpushConfig>(json);
            Assert.Equal(original.Headers, copy.Headers);
            Assert.Equal(original.Data, copy.Data);
            Assert.Equal(original.Notification.Title, copy.Notification.Title);
        }

        [Fact]
        public void WebpushConfigCopy()
        {
            var original = new WebpushConfig()
            {
                Headers = new Dictionary<string, string>(),
                Data = new Dictionary<string, string>(),
                Notification = new WebpushNotification(),
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.Headers, copy.Headers);
            Assert.NotSame(original.Data, copy.Data);
            Assert.NotSame(original.Notification, copy.Notification);
        }

        [Fact]
        public void WebpushNotificationDeserialization()
        {
            var original = new WebpushNotification()
            {
                Title = "title",
                Body = "body",
                Icon = "icon",
                Badge = "badge",
                Data = new Dictionary<string, object>()
                {
                    { "some", "data" },
                },
                Direction = Direction.LeftToRight,
                Image = "image",
                Language = "language",
                Tag = "tag",
                Silent = true,
                RequireInteraction = true,
                Renotify = true,
                TimestampMillis = 100,
                Vibrate = new int[] { 10, 5, 10 },
                Actions = new List<Action>()
                {
                    new Action()
                    {
                        ActionName = "Accept",
                        Title = "Ok",
                        Icon = "ok-button",
                    },
                    new Action()
                    {
                        ActionName = "Reject",
                        Title = "Cancel",
                        Icon = "cancel-button",
                    },
                },
                CustomData = new Dictionary<string, object>()
                {
                    { "custom-key1", "custom-data" },
                    { "custom-key2", true },
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<WebpushNotification>(json);
            Assert.Equal(original.Title, copy.Title);
            Assert.Equal(original.Body, copy.Body);
            Assert.Equal(original.Icon, copy.Icon);
            Assert.Equal(original.Badge, copy.Badge);
            Assert.Equal(new JObject() { { "some", "data" } }, copy.Data);
            Assert.Equal(original.Direction, copy.Direction);
            Assert.Equal(original.Image, copy.Image);
            Assert.Equal(original.Language, copy.Language);
            Assert.Equal(original.Tag, copy.Tag);
            Assert.Equal(original.Silent, copy.Silent);
            Assert.Equal(original.RequireInteraction, copy.RequireInteraction);
            Assert.Equal(original.Renotify, copy.Renotify);
            Assert.Equal(original.TimestampMillis, copy.TimestampMillis);
            Assert.Equal(original.Vibrate, copy.Vibrate);
            var originalActions = original.Actions.ToList();
            var copyActions = original.Actions.ToList();
            Assert.Equal(originalActions.Count, copyActions.Count);
            for (int i = 0; i < originalActions.Count; i++)
            {
                Assert.Equal(originalActions[i].ActionName, copyActions[i].ActionName);
                Assert.Equal(originalActions[i].Title, copyActions[i].Title);
                Assert.Equal(originalActions[i].Icon, copyActions[i].Icon);
            }

            Assert.Equal(original.CustomData, copy.CustomData);
        }

        [Fact]
        public void WebpushNotificationCopy()
        {
            var original = new WebpushNotification()
            {
                Actions = new List<Action>()
                {
                    new Action()
                    {
                        ActionName = "Accept",
                        Title = "Ok",
                        Icon = "ok-button",
                    },
                },
                CustomData = new Dictionary<string, object>()
                {
                    { "custom-key1", "custom-data" },
                },
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.Actions, copy.Actions);
            Assert.NotSame(original.Actions.First(), copy.Actions.First());
            Assert.NotSame(original.CustomData, copy.CustomData);
            Assert.Equal(original.CustomData, copy.CustomData);
        }

        [Fact]
        public void WebpushNotificationDuplicateKeys()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        CustomData = new Dictionary<string, object>() { { "title", "other" } },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsConfig()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Headers = new Dictionary<string, string>()
                    {
                        { "k1", "v1" },
                        { "k2", "v2" },
                    },
                    Aps = new Aps()
                    {
                        AlertString = "alert-text",
                        Badge = 0,
                        Category = "test-category",
                        ContentAvailable = true,
                        MutableContent = true,
                        Sound = "sound-file",
                        ThreadId = "test-thread",
                        CustomData = new Dictionary<string, object>()
                        {
                            { "custom-key1", "custom-data" },
                            { "custom-key2", true },
                        },
                    },
                    CustomData = new Dictionary<string, object>()
                    {
                        { "custom-key3", "custom-data" },
                        { "custom-key4", true },
                    },
                    FcmOptions = new ApnsFcmOptions()
                    {
                        AnalyticsLabel = "label",
                        ImageUrl = "https://example.com/image.png",
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "headers", new JObject()
                            {
                                { "k1", "v1" },
                                { "k2", "v2" },
                            }
                        },
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        { "alert", "alert-text" },
                                        { "badge", 0 },
                                        { "category", "test-category" },
                                        { "content-available", 1 },
                                        { "mutable-content", 1 },
                                        { "sound", "sound-file" },
                                        { "thread-id", "test-thread" },
                                        { "custom-key1", "custom-data" },
                                        { "custom-key2", true },
                                    }
                                },
                                { "custom-key3", "custom-data" },
                                { "custom-key4", true },
                            }
                        },
                        {
                            "fcm_options", new JObject()
                            {
                                { "analytics_label", "label" },
                                { "image", "https://example.com/image.png" },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsConfigMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps(),
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                { "aps", new JObject() },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsConfigDeserialization()
        {
            var original = new ApnsConfig()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "k1", "v1" },
                    { "k2", "v2" },
                },
                Aps = new Aps()
                {
                    AlertString = "alert-text",
                },
                CustomData = new Dictionary<string, object>()
                {
                    { "custom-key3", "custom-data" },
                    { "custom-key4", true },
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<ApnsConfig>(json);
            Assert.Equal(original.Headers, copy.Headers);
            Assert.Equal(original.CustomData, copy.CustomData);
            Assert.Equal(original.Aps.AlertString, copy.Aps.AlertString);
        }

        [Fact]
        public void ApnsConfigCopy()
        {
            var original = new ApnsConfig()
            {
                Headers = new Dictionary<string, string>(),
                Aps = new Aps(),
                CustomData = new Dictionary<string, object>(),
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.Headers, copy.Headers);
            Assert.NotSame(original.Aps, copy.Aps);
            Assert.NotSame(original.CustomData, copy.CustomData);
        }

        [Fact]
        public void ApnsConfigCustomApsDeserialization()
        {
            var original = new ApnsConfig()
            {
                Headers = new Dictionary<string, string>()
                {
                    { "k1", "v1" },
                    { "k2", "v2" },
                },
                CustomData = new Dictionary<string, object>()
                {
                    {
                        "aps", new Dictionary<string, object>()
                        {
                            { "alert", "alert-text" },
                            { "custom-key1", "custom-data" },
                            { "custom-key2", true },
                        }
                    },
                    { "custom-key3", "custom-data" },
                    { "custom-key4", true },
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<ApnsConfig>(json);
            Assert.Equal(original.Headers, copy.Headers);
            original.CustomData.Remove("aps");
            Assert.Equal(original.CustomData, copy.CustomData);
            Assert.Equal("alert-text", copy.Aps.AlertString);
            var customApsData = new Dictionary<string, object>()
            {
                { "custom-key1", "custom-data" },
                { "custom-key2", true },
            };
            Assert.Equal(customApsData, copy.Aps.CustomData);
        }

        [Fact]
        public void ApnsCriticalSound()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        CriticalSound = new CriticalSound()
                        {
                            Name = "default",
                            Critical = true,
                            Volume = 0.5,
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        {
                                            "sound", new JObject()
                                            {
                                                { "name", "default" },
                                                { "critical", 1 },
                                                { "volume", 0.5 },
                                            }
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsCriticalSoundMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        CriticalSound = new CriticalSound() { Name = "default" },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        {
                                            "sound", new JObject()
                                            {
                                                { "name", "default" },
                                            }
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsCriticalSoundDeserialization()
        {
            var original = new CriticalSound()
            {
                Name = "default",
                Volume = 0.5,
                Critical = true,
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<CriticalSound>(json);
            Assert.Equal(original.Name, copy.Name);
            Assert.Equal(original.Volume.Value, copy.Volume.Value);
            Assert.Equal(original.Critical, copy.Critical);
        }

        [Fact]
        public void ApnsApsAlert()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            ActionLocKey = "action-key",
                            Body = "test-body",
                            LaunchImage = "test-image",
                            LocArgs = new List<string>() { "arg1", "arg2" },
                            LocKey = "loc-key",
                            Subtitle = "test-subtitle",
                            SubtitleLocArgs = new List<string>() { "arg3", "arg4" },
                            SubtitleLocKey = "subtitle-key",
                            Title = "test-title",
                            TitleLocArgs = new List<string>() { "arg5", "arg6" },
                            TitleLocKey = "title-key",
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        {
                                            "alert", new JObject()
                                            {
                                                { "action-loc-key", "action-key" },
                                                { "body", "test-body" },
                                                { "launch-image", "test-image" },
                                                { "loc-args", new JArray() { "arg1", "arg2" } },
                                                { "loc-key", "loc-key" },
                                                { "subtitle", "test-subtitle" },
                                                { "subtitle-loc-args", new JArray() { "arg3", "arg4" } },
                                                { "subtitle-loc-key", "subtitle-key" },
                                                { "title", "test-title" },
                                                { "title-loc-args", new JArray() { "arg5", "arg6" } },
                                                { "title-loc-key", "title-key" },
                                            }
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsApsAlertMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert(),
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        {
                                            "alert", new JObject()
                                        },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApsAlertDeserialization()
        {
            var original = new ApsAlert()
            {
                ActionLocKey = "action-key",
                Body = "test-body",
                LaunchImage = "test-image",
                LocArgs = new List<string>() { "arg1", "arg2" },
                LocKey = "loc-key",
                Subtitle = "test-subtitle",
                SubtitleLocArgs = new List<string>() { "arg3", "arg4" },
                SubtitleLocKey = "subtitle-key",
                Title = "test-title",
                TitleLocArgs = new List<string>() { "arg5", "arg6" },
                TitleLocKey = "title-key",
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<ApsAlert>(json);
            Assert.Equal(original.ActionLocKey, copy.ActionLocKey);
            Assert.Equal(original.Body, copy.Body);
            Assert.Equal(original.LaunchImage, copy.LaunchImage);
            Assert.Equal(original.LocArgs, copy.LocArgs);
            Assert.Equal(original.LocKey, copy.LocKey);
            Assert.Equal(original.Subtitle, copy.Subtitle);
            Assert.Equal(original.SubtitleLocArgs, copy.SubtitleLocArgs);
            Assert.Equal(original.SubtitleLocKey, copy.SubtitleLocKey);
            Assert.Equal(original.Title, copy.Title);
            Assert.Equal(original.TitleLocArgs, copy.TitleLocArgs);
            Assert.Equal(original.TitleLocKey, copy.TitleLocKey);
        }

        [Fact]
        public void ApsAlertCopy()
        {
            var original = new ApsAlert()
            {
                LocArgs = new List<string>() { "arg1", "arg2" },
                LocKey = "loc-key",
                SubtitleLocArgs = new List<string>() { "arg3", "arg4" },
                SubtitleLocKey = "subtitle-key",
                TitleLocArgs = new List<string>() { "arg5", "arg6" },
                TitleLocKey = "title-key",
            };
            var copy = original.CopyAndValidate();
            Assert.NotSame(original, copy);
            Assert.NotSame(original.LocArgs, copy.LocArgs);
            Assert.NotSame(original.SubtitleLocArgs, copy.SubtitleLocArgs);
            Assert.NotSame(original.TitleLocArgs, copy.TitleLocArgs);
        }

        [Fact]
        public void ApnsApsAlertInvalidTitleLocArgs()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            TitleLocArgs = new List<string>() { "arg1", "arg2" },
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsApsAlertInvalidSubtitleLocArgs()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            SubtitleLocArgs = new List<string>() { "arg1", "arg2" },
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsApsAlertInvalidLocArgs()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Alert = new ApsAlert()
                        {
                            LocArgs = new List<string>() { "arg1", "arg2" },
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsCustomApsWithStandardProperties()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    CustomData = new Dictionary<string, object>()
                    {
                        {
                            "aps", new Dictionary<string, object>()
                            {
                                { "alert", "alert-text" },
                                { "badge", 42 },
                            }
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        { "alert", "alert-text" },
                                        { "badge", 42 },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsCustomApsWithCustomProperties()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    CustomData = new Dictionary<string, object>()
                    {
                        {
                            "aps", new Dictionary<string, object>()
                            {
                                { "custom-key1", "custom-data" },
                                { "custom-key2", true },
                            }
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "apns", new JObject()
                    {
                        {
                            "payload", new JObject()
                            {
                                {
                                    "aps", new JObject()
                                    {
                                        { "custom-key1", "custom-data" },
                                        { "custom-key2", true },
                                    }
                                },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void ApnsNoAps()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    CustomData = new Dictionary<string, object>()
                    {
                        { "test", "custom-data" },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsDuplicateAps()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        AlertString = "alert-text",
                    },
                    CustomData = new Dictionary<string, object>()
                    {
                        { "aps", "custom-data" },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApsDuplicateKeys()
        {
            var aps = new Aps()
            {
                AlertString = "alert-text",
                CustomData = new Dictionary<string, object>()
                {
                    { "alert", "other-alert-text" },
                },
            };
            Assert.Throws<ArgumentException>(() => aps.CopyAndValidate());
        }

        [Fact]
        public void ApnsDuplicateApsAlerts()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        AlertString = "alert-text",
                        Alert = new ApsAlert()
                        {
                            Body = "other-alert-text",
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsDuplicateApsSounds()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        Sound = "default",
                        CriticalSound = new CriticalSound()
                        {
                            Name = "other=sound",
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsInvalidCriticalSoundNoName()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        CriticalSound = new CriticalSound(),
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsInvalidCriticalSoundVolumeTooLow()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        CriticalSound = new CriticalSound()
                        {
                            Name = "default",
                            Volume = -0.1,
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsInvalidCriticalSoundVolumeTooHigh()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Apns = new ApnsConfig()
                {
                    Aps = new Aps()
                    {
                        CriticalSound = new CriticalSound()
                        {
                            Name = "default",
                            Volume = 1.1,
                        },
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void ApnsFcmOptionsInvalidImageUrls()
        {
            var imageUrls = new List<string>()
            {
                string.Empty, "image.png", "invalid-image", "foo bar",
            };
            foreach (var imageUrl in imageUrls)
            {
                var message = new Message()
                {
                    Topic = "test-topic",
                    Apns = new ApnsConfig()
                    {
                        FcmOptions = new ApnsFcmOptions()
                        {
                            ImageUrl = imageUrl,
                        },
                    },
                };
                Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
            }
        }

        [Fact]
        public void WebpushNotificationWithLinkUrl()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                    },
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = "https://www.firebase.io/",
                    },
                },
            };
            var expected = new JObject()
            {
                { "topic", "test-topic" },
                {
                    "webpush", new JObject()
                    {
                        {
                            "notification", new JObject()
                            {
                                { "title", "title" },
                                { "body", "body" },
                                { "icon", "icon" },
                            }
                        },
                        {
                            "fcm_options", new JObject()
                            {
                                { "link", "https://www.firebase.io/" },
                            }
                        },
                    }
                },
            };
            this.AssertJsonEquals(expected, message);
        }

        [Fact]
        public void WebpushNotificationWithInvalidHttpLinkUrl()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                    },
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = "http://www.firebase.io/",
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void WebpushNotificationWithInvalidHttpsLinkUrl()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                    },
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = "https whatever",
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void AnalyticsLabelInvalid()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Notification = new Notification()
                {
                    Title = "title",
                    Body = "body",
                },
                FcmOptions = new FcmOptions()
                {
                    AnalyticsLabel = "label!",
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        private void AssertJsonEquals(JObject expected, Message actual)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(actual.CopyAndValidate());
            var parsed = JObject.Parse(json);
            Assert.True(
                JToken.DeepEquals(expected, parsed),
                $"Expected: {expected.ToString()}\nActual: {parsed.ToString()}");
        }
    }
}
