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
using FirebaseAdmin.Messaging;
using Google.Apis.Json;

namespace FirebaseAdmin.Messaging.Tests
{
    public class MessageTest
    {
        [Fact]
        public void MessageWithoutTarget()
        {
            Assert.Throws<ArgumentException>(() => new Message().Validate());
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
            Assert.Throws<ArgumentException>(() => message.Validate());

            message = new Message()
            {
                Token = "test-token",
                Condition = "test-condition",
            };
            Assert.Throws<ArgumentException>(() => message.Validate());

            message = new Message()
            {
                Condition = "test-condition",
                Topic = "test-topic",
            };
            Assert.Throws<ArgumentException>(() => message.Validate());

            message = new Message()
            {
                Token = "test-token",
                Topic = "test-topic",
                Condition = "test-condition",
            };
            Assert.Throws<ArgumentException>(() => message.Validate());
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
                Assert.Throws<ArgumentException>(() => message.Validate());
            }
        }

        [Fact]
        public void PrefixedTopicName()
        {
            var message = new Message(){Topic = "/topics/test-topic"};
            AssertJsonEquals(new JObject(){{"topic", "test-topic"}}, message);
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

        private void AssertJsonEquals(JObject expected, Message actual)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(actual.Validate());
            var parsed = JObject.Parse(json);
            Assert.True(
                JToken.DeepEquals(expected, parsed),
                $"Expected: {expected.ToString()}\nActual: {parsed.ToString()}");
        }
    }
}