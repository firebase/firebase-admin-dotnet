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
using Newtonsoft.Json.Linq;
using Xunit;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging.Tests
{
    public class MessageTest
    {
        [Fact]
        public void MessageWithoutTarget()
        {
            Assert.Throws<ArgumentException>(() => new Message().CopyAndValidate());
        }

        [Fact]
        public void EmptyMessage()
        {
            var message = new Message(){Token = "test-token"};
            AssertJsonEquals(new JObject(){{"token", "test-token"}}, message);

            message = new Message(){Topic = "test-topic"};
            AssertJsonEquals(new JObject(){{"topic", "test-topic"}}, message);

            message = new Message(){Condition = "test-condition"};
            AssertJsonEquals(new JObject(){{"condition", "test-condition"}}, message);
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
            AssertJsonEquals(new JObject()
                {
                    {"topic", "test-topic"},
                    {"data", new JObject(){{"k1", "v1"}, {"k2", "v2"}}},
                }, message);
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
                var message = new Message(){Topic = topic};
                Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
            }
        }

        [Fact]
        public void PrefixedTopicName()
        {
            var message = new Message(){Topic = "/topics/test-topic"};
            AssertJsonEquals(new JObject(){{"topic", "test-topic"}}, message);
        }

        [Fact]
        public void MessageDeserialization()
        {
            var original = new Message()
            {
                Topic = "test-topic",
                Data = new Dictionary<string, string>()
                {
                    { "key", "value" },
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<Message>(json);
            Assert.Equal(original.Topic, copy.Topic);
            Assert.Equal(original.Data, copy.Data);
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
                },
            };
            var expected = new JObject()
            {
                {"topic", "test-topic"},
                {
                    "notification", new JObject()
                    {
                        {"title", "title"},
                        {"body", "body"},
                    }
                },
            };
            AssertJsonEquals(expected, message);
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
                        ClickAction = "click-action",
                        TitleLocKey = "title-loc-key",
                        TitleLocArgs = new List<string>(){ "arg1", "arg2" },
                        BodyLocKey = "body-loc-key",
                        BodyLocArgs = new List<string>(){ "arg3", "arg4" },
                        ChannelId = "channel-id",
                    },
                },
            };
            var expected = new JObject()
            {
                {"topic", "test-topic"},
                {
                    "android", new JObject()
                    {
                        { "collapse_key", "collapse-key" },
                        { "priority", "high" },
                        { "ttl", "0.010000000s" },
                        { "restricted_package_name", "test-pkg-name" },
                        { "data", new JObject(){{"k1", "v1"}, {"k2", "v2"}} },
                        {
                            "notification", new JObject()
                            {
                                { "title", "title" },
                                { "body", "body" },
                                { "icon", "icon" },
                                { "color", "#112233" },
                                { "sound", "sound" },
                                { "tag", "tag" },
                                { "click_action", "click-action" },
                                { "title_loc_key", "title-loc-key" },
                                { "title_loc_args", new JArray(){"arg1", "arg2"} },
                                { "body_loc_key", "body-loc-key" },
                                { "body_loc_args", new JArray(){"arg3", "arg4"} },
                                { "channel_id", "channel-id" },
                            }
                        },
                    }
                },
            };
            AssertJsonEquals(expected, message);
        }

        [Fact]
        public void AndroidConfigMinimal()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Android = new AndroidConfig()
                {
                    RestrictedPackageName = "test-pkg-name",
                },
            };
            var expected = new JObject()
            {
                {"topic", "test-topic"},
                {
                    "android", new JObject()
                    {
                        { "restricted_package_name", "test-pkg-name" },
                    }
                },
            };
            AssertJsonEquals(expected, message);
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
                {"topic", "test-topic"},
                {
                    "android", new JObject()
                    {
                        { "ttl", "3600s" },
                    }
                },
            };
            AssertJsonEquals(expected, message);
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
            var expected = new JObject()
            {
                {"topic", "test-topic"},
                {
                    "android", new JObject()
                    {
                        { "ttl", "3600s" },
                    }
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
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
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<AndroidConfig>(json);
            Assert.Equal(original.Priority, copy.Priority);
            Assert.Equal(original.TimeToLive, copy.TimeToLive);
            Assert.Equal(original.Data, copy.Data);
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
                        Color = "not-a-color"
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
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
                        TitleLocArgs = new List<string>(){"arg"},
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
                        BodyLocArgs = new List<string>(){"arg"},
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
                        {"header1", "header-value1"},
                        {"header2", "header-value2"},
                    },
                    Data = new Dictionary<string, string>()
                    {
                        {"key1", "value1"},
                        {"key2", "value2"},
                    },
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        Body = "body",
                        Icon = "icon",
                        Badge = "badge",
                        Data = new Dictionary<string, object>()
                        {
                            {"some", "data"},
                        },
                        Direction = Direction.LeftToRight,
                        Image = "image",
                        Language = "language",
                        Tag = "tag",
                        Silent = true,
                        RequireInteraction = true,
                        Renotify = true,
                        TimestampMillis = 100,
                        Vibrate = new int[]{10, 5, 10},
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
                            {"custom-key1", "custom-data"},
                            {"custom-key2", true},
                        },
                    },
                },
            };
            var expected = new JObject()
            {
                {"topic", "test-topic"},
                {
                    "webpush", new JObject()
                    {
                        {
                            "headers", new JObject()
                            {
                                {"header1", "header-value1"},
                                {"header2", "header-value2"},
                            }
                        },
                        {
                            "data", new JObject()
                            {
                                {"key1", "value1"},
                                {"key2", "value2"},
                            }
                        },
                        {
                            "notification", new JObject()
                            {
                                {"title", "title"},
                                {"body", "body"},
                                {"icon", "icon"},
                                {"badge", "badge"},
                                {
                                    "data", new JObject()
                                    {
                                        {"some", "data"},
                                    }
                                },
                                {"dir", "ltr"},
                                {"image", "image"},
                                {"lang", "language"},
                                {"renotify", true},
                                {"requireInteraction", true},
                                {"silent", true},
                                {"tag", "tag"},
                                {"timestamp", 100},
                                {"vibrate", new JArray(){10, 5, 10}},
                                {
                                    "actions", new JArray()
                                    {
                                        new JObject()
                                        {
                                            {"action", "Accept"},
                                            {"title", "Ok"},
                                            {"icon", "ok-button"},
                                        },
                                        new JObject()
                                        {
                                            {"action", "Reject"},
                                            {"title", "Cancel"},
                                            {"icon", "cancel-button"},
                                        },
                                    }
                                },
                                {"custom-key1", "custom-data"},
                                {"custom-key2", true},
                            }
                        },
                    }
                },
            };
            AssertJsonEquals(expected, message);
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
                {"topic", "test-topic"},
                {
                    "webpush", new JObject()
                    {
                        {
                            "notification", new JObject()
                            {
                                {"title", "title"},
                                {"body", "body"},
                                {"icon", "icon"},
                            }
                        },
                    }
                },
            };
            AssertJsonEquals(expected, message);
        }

        [Fact]
        public void WebpushConfigDuplicateKeys()
        {
            var message = new Message()
            {
                Topic = "test-topic",
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = "title",
                        CustomData = new Dictionary<string, object>(){{"title", "other"}},
                    },
                },
            };
            Assert.Throws<ArgumentException>(() => message.CopyAndValidate());
        }

        [Fact]
        public void WebpushNotificationDeserialization()
        {
            var original = new WebpushNotification()
            {
                CustomData = new Dictionary<string, object>()
                {
                    {"custom-key1", "custom-data"},
                    {"custom-key2", true},
                },
            };
            var json = NewtonsoftJsonSerializer.Instance.Serialize(original);
            var copy = NewtonsoftJsonSerializer.Instance.Deserialize<WebpushNotification>(json);
            Assert.Equal(original.CustomData, copy.CustomData);
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
