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
        public string Token { private get; set; }

        /// <summary>
        /// The name of the FCM topic to which the message should be sent. Topic names may
        /// contain the <c>/topics/</c> prefix.
        /// </summary>
        public string Topic { private get; set; }

        /// <summary>
        /// The FCM condition to which the message should be sent. Must be a valid condition
        /// string such as <c>"'foo' in topics"</c>.
        /// </summary>
        public string Condition { private get; set; }

        /// <summary>
        /// A collection of key-value pairs that will be added to the message as data fields. Keys
        /// and the values must not be null.
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { private get; set; }

        /// <summary>
        /// The notification information to be included in the message.
        /// </summary>
        public Notification Notification { private get; set; }

        /// <summary>
        /// The Android-specific information to be included in the message.
        /// </summary>
        public AndroidConfig AndroidConfig { private get; set; }

        internal ValidatedMessage Validate()
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
            return new ValidatedMessage()
            {
                Token = this.Token,
                Topic = this.ValidatedTopic,
                Condition = this.Condition,
                Data = this.Data,
                Notification = this.Notification,
                AndroidConfig = this.AndroidConfig?.Validate(),
            };
        }

        private string ValidatedTopic
        {
            get
            {
                if (string.IsNullOrEmpty(Topic))
                {
                    return null;
                }
                var topic = Topic;
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

    internal sealed class ValidatedMessage
    {
        [JsonProperty("token")]
        internal string Token { get; set; }

        [JsonProperty("topic")]
        internal string Topic { get; set; }

        [JsonProperty("condition")]
        internal string Condition { get; set; }

        [JsonProperty("data")]
        internal IReadOnlyDictionary<string, string> Data { get; set; }

        [JsonProperty("notification")]
        internal Notification Notification { get; set; }

        [JsonProperty("android")]
        internal ValidatedAndroidConfig AndroidConfig { get; set; }
    }
}
