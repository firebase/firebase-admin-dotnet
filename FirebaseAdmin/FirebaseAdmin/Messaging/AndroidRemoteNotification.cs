// Copyright 2026, Google Inc. All rights reserved.
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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the remote notification configurations for Android V2 config.
    /// </summary>
    public sealed class AndroidRemoteNotification
    {
        /// <summary>
        /// Gets or sets a value indicating whether to invoke the app's code to modify the notification before displaying on device.
        /// </summary>
        [JsonProperty("mutable_content")]
        public bool? MutableContent { get; set; }

        /// <summary>
        /// Gets or sets the Android V2 notification details.
        /// </summary>
        [JsonProperty("notification")]
        public AndroidNotificationV2 Notification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether legacy clients should treat this message as a data message.
        /// </summary>
        [JsonProperty("use_as_v1_data_message")]
        public bool? UseAsV1DataMessage { get; set; }

        internal AndroidRemoteNotification CopyAndValidate()
        {
            if (this.Notification == null)
            {
                throw new ArgumentException("Notification must be specified on AndroidRemoteNotification.");
            }

            var copy = new AndroidRemoteNotification()
            {
                MutableContent = this.MutableContent,
                UseAsV1DataMessage = this.UseAsV1DataMessage,
                Notification = this.Notification.CopyAndValidate(),
            };

            return copy;
        }
    }
}
