// Copyright 2019, Google Inc. All rights reserved.
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
using FirebaseAdmin.Messaging.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents Apple Push Notification Service FCM options.
    /// </summary>
    public sealed class ApnsFcmOptions
    {
        /// <summary>
        /// Gets or sets analytics label.
        /// </summary>
        [JsonProperty("analytics_label")]
        public string AnalyticsLabel { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image to be displayed in the notification.
        /// </summary>
        [JsonProperty("image")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Copies this FCM options, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal ApnsFcmOptions CopyAndValidate()
        {
            var copy = new ApnsFcmOptions()
            {
                AnalyticsLabel = this.AnalyticsLabel,
                ImageUrl = this.ImageUrl,
            };
            AnalyticsLabelChecker.ValidateAnalyticsLabel(copy.AnalyticsLabel);

            if (copy.ImageUrl != null && !Uri.IsWellFormedUriString(copy.ImageUrl, UriKind.Absolute))
            {
                throw new ArgumentException($"Malformed image URL string: {copy.ImageUrl}.");
            }

            return copy;
        }
    }
}
