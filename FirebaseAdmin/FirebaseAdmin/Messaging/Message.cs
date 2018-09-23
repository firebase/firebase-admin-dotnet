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
    /// Something.
    /// </summary>
    public sealed class Message
    {
        /// <summary>
        /// Something.
        /// </summary>
        [JsonProperty("token")]
        public string Token { internal get; set; }

        /// <summary>
        /// Something.
        /// </summary>
        [JsonProperty("topic")]
        public string Topic { internal get; set; }

        /// <summary>
        /// Something.
        /// </summary>
        [JsonProperty("condition")]
        public string Condition { internal get; set; }

        /// <summary>
        /// Something.
        /// </summary>
        [JsonProperty("data")]
        public IDictionary<string, string> Data { internal get; set; }

        internal Message Validate()
        {
            var list = new List<string>()
            {
                Token, Topic, Condition,
            };
            var targets = list.FindAll((target) => !string.IsNullOrEmpty(target));
            if (targets.Count != 1)
            {
                throw new ArgumentException("Exactly one of Token, Topic or Condition is required.");
            }
            return new Message()
            {
                Token = Token,
                Topic = ValidateTopic(Topic),
                Condition = Condition,
                Data = Data,
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