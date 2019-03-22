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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents a message that can be sent to multiple devices via Firebase Cloud Messaging (FCM).
    /// Contains payload information as well as the list of device registration tokens to which the
    /// message should be sent. A single <c>MulticastMessage</c> may contain up to 100 registration
    /// tokens.
    /// </summary>
    public sealed class MulticastMessage
    {
        /// <summary>
        /// Gets or sets the registration tokens for the devices to which the message should be distributed.
        /// </summary>
        [JsonProperty("tokens")]
        public IReadOnlyList<string> Tokens { get; set; }

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
        /// Copies this message, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service. Each property is copied
        /// before validation to guard against the original being modified in the user code
        /// post-validation.
        /// </summary>
        internal MulticastMessage CopyAndValidate()
        {
            // Copy and validate the leaf-level properties
            var copy = new MulticastMessage
            {
                Tokens = this.Tokens?.Copy(),
                Data = this.Data?.Copy(),
            };

            if (copy.Tokens == null || copy.Tokens.Count < 1)
            {
                throw new ArgumentException("At least one token is required.");
            }

            if (copy.Tokens.Count > 100)
            {
                throw new ArgumentException("At most 100 tokens are allowed.");
            }

            // Copy and validate the child properties
            copy.Notification = this.Notification?.CopyAndValidate();
            copy.Android = this.Android?.CopyAndValidate();
            copy.Webpush = this.Webpush?.CopyAndValidate();
            copy.Apns = this.Apns?.CopyAndValidate();
            return copy;
        }

        internal List<Message> GetMessageList()
        {
            var templateMessage = new Message
            {
                Android = this.Android?.CopyAndValidate(),
                Apns = this.Apns?.CopyAndValidate(),
                Data = this.Data?.Copy(),
                Notification = this.Notification?.CopyAndValidate(),
                Webpush = this.Webpush?.CopyAndValidate(),
            };

            var messages = new List<Message>(this.Tokens.Count);

            foreach (var token in this.Tokens)
            {
                templateMessage.Token = token;
                var message = templateMessage.CopyAndValidate();
                messages.Add(message);
            }

            return messages;
        }
    }
}
