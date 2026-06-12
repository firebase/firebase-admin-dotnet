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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents a message that can be sent to multiple devices via Firebase Cloud Messaging (FCM).
    /// Contains payload information as well as the list of device registration tokens and/or
    /// Firebase Installation IDs (FIDs) to which the message should be sent. A single
    /// <c>MulticastMessage</c> may contain up to 500 tokens and FIDs combined.
    /// </summary>
    public sealed class MulticastMessage
    {
        /// <summary>
        /// Gets or sets the registration tokens for the devices to which the message should be distributed.
        /// </summary>
        [Obsolete("Deprecated. Use Fids instead.")]
        public IReadOnlyList<string> Tokens { get; set; }

        /// <summary>
        /// Gets or sets the installation IDs (FIDs) for the devices to which the message should be distributed.
        /// </summary>
        public IReadOnlyList<string> Fids { get; set; }

        /// <summary>
        /// Gets or sets a collection of key-value pairs that will be added to the message as data
        /// fields. Keys and the values must not be null.
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { get; set; }

        /// <summary>
        /// Gets or sets the notification information to be included in the message.
        /// </summary>
        public Notification Notification { get; set; }

        /// <summary>
        /// Gets or sets the Android-specific information to be included in the message.
        /// </summary>
        public AndroidConfig Android { get; set; }

        /// <summary>
        /// Gets or sets the Webpush-specific information to be included in the message.
        /// </summary>
        public WebpushConfig Webpush { get; set; }

        /// <summary>
        /// Gets or sets the APNs-specific information to be included in the message.
        /// </summary>
        public ApnsConfig Apns { get; set; }

        internal List<Message> GetMessageList()
        {
#pragma warning disable CS0618
            var tokens = this.Tokens;
#pragma warning restore CS0618
            var fids = this.Fids;

            var tokensCopy = tokens != null ? new List<string>(tokens) : null;
            var fidsCopy = fids != null ? new List<string>(fids) : null;

            var tokensCount = tokensCopy?.Count ?? 0;
            var fidsCount = fidsCopy?.Count ?? 0;
            var totalCount = tokensCount + fidsCount;

            if (totalCount == 0)
            {
                throw new ArgumentException("Tokens and FIDs cannot be both null or empty.");
            }

            if (totalCount > 500)
            {
                throw new ArgumentException("Total number of Tokens and FIDs must not exceed 500.");
            }

            var templateMessage = new Message
            {
                Android = this.Android?.CopyAndValidate(),
                Apns = this.Apns?.CopyAndValidate(),
                Data = this.Data?.Copy(),
                Notification = this.Notification?.CopyAndValidate(),
                Webpush = this.Webpush?.CopyAndValidate(),
            };

            var messages = new List<Message>(totalCount);

            if (tokensCopy != null)
            {
                foreach (var token in tokensCopy)
                {
                    var message = new Message
                    {
                        Android = templateMessage.Android,
                        Apns = templateMessage.Apns,
                        Data = templateMessage.Data,
                        Notification = templateMessage.Notification,
                        Webpush = templateMessage.Webpush,
                    };

#pragma warning disable CS0618
                    message.Token = token;
#pragma warning restore CS0618

                    messages.Add(message);
                }
            }

            if (fidsCopy != null)
            {
                foreach (var fid in fidsCopy)
                {
                    var message = new Message
                    {
                        Android = templateMessage.Android,
                        Apns = templateMessage.Apns,
                        Data = templateMessage.Data,
                        Notification = templateMessage.Notification,
                        Webpush = templateMessage.Webpush,
                        Fid = fid,
                    };

                    messages.Add(message);
                }
            }

            return messages;
        }
    }
}
