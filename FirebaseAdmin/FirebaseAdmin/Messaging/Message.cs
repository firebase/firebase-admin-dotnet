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
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Google.Apis.Json;
using Google.Apis.Util;
using FirebaseAdmin;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents a message that can be sent via Firebase Cloud Messaging (FCM). Contains payload
    /// information as well as the recipient information. The recipient information must be
    /// specified by setting exactly one of the <see cref="Token"/>, <see cref="Topic"/> or
    /// <see cref="Condition"/> fields.
    /// </summary>
    public sealed class Message
    {
        /// <summary>
        /// The registration token of the device to which the message should be sent.
        /// </summary>
        [JsonProperty("token")]
        public string Token { internal get; set; }

        /// <summary>
        /// The name of the FCM topic to which the message should be sent. Topic names may
        /// contain the <c>/topics/</c> prefix.
        /// </summary>
        [JsonProperty("topic")]
        public string Topic { internal get; set; }

        /// <summary>
        /// The FCM condition to which the message should be sent. Must be a valid condition
        /// string such as <c>"'foo' in topics"</c>.
        /// </summary>
        [JsonProperty("condition")]
        public string Condition { internal get; set; }

        /// <summary>
        /// A collection of key-value pairs that will be added to the message as data fields. Keys
        /// and the values must not be null.
        /// </summary>
        [JsonProperty("data")]
        public IDictionary<string, string> Data { internal get; set; }

        /// <summary>
        /// The <see cref="FirebaseAdmin.Messaging.Notification"/> information to be included in
        /// the message.
        /// </summary>
        [JsonProperty("notification")]
        public Notification Notification { internal get; set; }

        internal Message Validate()
        {
            var list = new List<string>()
            {
                Token, Topic, Condition,
            };
            var targets = list.FindAll((target) => !string.IsNullOrEmpty(target));
            if (targets.Count != 1)
            {
                throw new ArgumentException(
                    "Exactly one of Token, Topic or Condition is required.");
            }
            return new Message()
            {
                Token = Token,
                Topic = ValidateTopic(Topic),
                Condition = Condition,
                Data = Data,
                Notification = Notification,
            };
        }

        private static string ValidateTopic(string topic)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return null;
            }
            if (topic.StartsWith("/topics/"))
            {
                topic = topic.Substring("/topics/".Length);
            }
            if (!Regex.IsMatch(topic, "^[a-zA-Z0-9-_.~%]+$"))
            {
                throw new ArgumentException("Malformed topic name.");
            }
            return topic;
        }
    }
}