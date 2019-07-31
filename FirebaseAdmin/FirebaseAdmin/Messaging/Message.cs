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
        /// Gets or sets the registration token of the device to which the message should be sent.
        /// </summary>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the name of the FCM topic to which the message should be sent. Topic names
        /// may contain the <c>/topics/</c> prefix.
        /// </summary>
        [JsonIgnore]
        public string Topic { get; set; }

        /// <summary>
        /// Gets or sets the FCM condition to which the message should be sent. Must be a valid
        /// condition string such as <c>"'foo' in topics"</c>.
        /// </summary>
        [JsonProperty("condition")]
        public string Condition { get; set; }

        /// <summary>
        /// Gets or sets a collection of key-value pairs that will be added to the message as data
        /// fields. Keys and the values must not be null.
        /// </summary>
        [JsonProperty("data")]
        public IReadOnlyDictionary<string, string> Data { get; set; }

        /// <summary>
        /// Gets or sets the notification information to be included in the message.
        /// </summary>
        [JsonProperty("notification")]
        public Notification Notification { get; set; }

        /// <summary>
        /// Gets or sets the Android-specific information to be included in the message.
        /// </summary>
        [JsonProperty("android")]
        public AndroidConfig Android { get; set; }

        /// <summary>
        /// Gets or sets the Webpush-specific information to be included in the message.
        /// </summary>
        [JsonProperty("webpush")]
        public WebpushConfig Webpush { get; set; }

        /// <summary>
        /// Gets or sets the APNs-specific information to be included in the message.
        /// </summary>
        [JsonProperty("apns")]
        public ApnsConfig Apns { get; set; }

        /// <summary>
        /// Gets or sets the FCM options to be included in the message.
        /// </summary>
        [JsonProperty("fcm_options")]
        public FcmOptions FcmOptions { get; set; }

        /// <summary>
        /// Gets or sets the formatted representation of the <see cref="Topic"/>. Removes the
        /// <c>/topics/</c> prefix if present. This is what's ultimately sent to the FCM
        /// service.
        /// </summary>
        [JsonProperty("topic")]
        private string UnprefixedTopic
        {
            get
            {
                if (this.Topic != null && this.Topic.StartsWith("/topics/"))
                {
                    return this.Topic.Substring("/topics/".Length);
                }

                return this.Topic;
            }

            set
            {
                this.Topic = value;
            }
        }

        /// <summary>
        /// Copies this message, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service. Each property is copied
        /// before validation to guard against the original being modified in the user code
        /// post-validation.
        /// </summary>
        internal Message CopyAndValidate()
        {
            // Copy and validate the leaf-level properties
            var copy = new Message()
            {
                Token = this.Token,
                Topic = this.Topic,
                Condition = this.Condition,
                Data = this.Data?.Copy(),
                FcmOptions = this.FcmOptions?.CopyAndValidate(),
            };
            var list = new List<string>()
            {
                copy.Token, copy.Topic, copy.Condition,
            };
            var targets = list.FindAll((target) => !string.IsNullOrEmpty(target));
            if (targets.Count != 1)
            {
                throw new ArgumentException(
                    "Exactly one of Token, Topic or Condition is required.");
            }

            var topic = copy.UnprefixedTopic;
            if (topic != null && !Regex.IsMatch(topic, "^[a-zA-Z0-9-_.~%]+$"))
            {
                throw new ArgumentException("Malformed topic name.");
            }

            // Copy and validate the child properties
            copy.Notification = this.Notification?.CopyAndValidate();
            copy.Android = this.Android?.CopyAndValidate();
            copy.Webpush = this.Webpush?.CopyAndValidate();
            copy.Apns = this.Apns?.CopyAndValidate();
            return copy;
        }
    }
}
