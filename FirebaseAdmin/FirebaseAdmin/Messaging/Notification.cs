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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the notification parameters that can be included in a <see cref="Message"/>.
    /// </summary>
    public sealed class Notification
    {
        /// <summary>
        /// Gets or sets the title of the notification.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body of the notification.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image to be displayed in the notification.
        /// </summary>
        [JsonProperty("image")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Copies this notification, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal Notification CopyAndValidate()
        {
            var copy = new Notification()
            {
                Title = this.Title,
                Body = this.Body,
                ImageUrl = this.ImageUrl,
            };

            if (copy.ImageUrl != null && !Uri.IsWellFormedUriString(copy.ImageUrl, UriKind.Absolute))
            {
                throw new ArgumentException($"Malformed image URL string: {copy.ImageUrl}.");
            }

            return copy;
        }
    }
}
