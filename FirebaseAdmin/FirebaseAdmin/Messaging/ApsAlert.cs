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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the <a href="https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/PayloadKeyReference.html#//apple_ref/doc/uid/TP40008194-CH17-SW5">
    /// alert property</a> that can be included in the <c>aps</c> dictionary of an APNs
    /// payload.
    /// </summary>
    public sealed class ApsAlert
    {
        /// <summary>
        /// Gets or sets the title of the alert. When provided, overrides the title set via
        /// <see cref="Notification.Title"/>.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the subtitle of the alert.
        /// </summary>
        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        /// <summary>
        /// Gets or sets the body of the alert. When provided, overrides the body set via
        /// <see cref="Notification.Body"/>.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the key of the body string in the app's string resources to use to
        /// localize the body text.
        /// </summary>
        [JsonProperty("loc-key")]
        public string LocKey { get; set; }

        /// <summary>
        /// Gets or sets the resource key strings that will be used in place of the format
        /// specifiers in <see cref="LocKey"/>.
        /// </summary>
        [JsonProperty("loc-args")]
        public IEnumerable<string> LocArgs { get; set; }

        /// <summary>
        /// Gets or sets the key of the title string in the app's string resources to use to
        /// localize the title text.
        /// </summary>
        [JsonProperty("title-loc-key")]
        public string TitleLocKey { get; set; }

        /// <summary>
        /// Gets or sets the resource key strings that will be used in place of the format
        /// specifiers in <see cref="TitleLocKey"/>.
        /// </summary>
        [JsonProperty("title-loc-args")]
        public IEnumerable<string> TitleLocArgs { get; set; }

        /// <summary>
        /// Gets or sets the key of the subtitle string in the app's string resources to use to
        /// localize the subtitle text.
        /// </summary>
        [JsonProperty("subtitle-loc-key")]
        public string SubtitleLocKey { get; set; }

        /// <summary>
        /// Gets or sets the resource key strings that will be used in place of the format
        /// specifiers in <see cref="SubtitleLocKey"/>.
        /// </summary>
        [JsonProperty("subtitle-loc-args")]
        public IEnumerable<string> SubtitleLocArgs { get; set; }

        /// <summary>
        /// Gets or sets the key of the text in the app's string resources to use to localize the
        /// action button text.
        /// </summary>
        [JsonProperty("action-loc-key")]
        public string ActionLocKey { get; set; }

        /// <summary>
        /// Gets or sets the launch image for the notification action.
        /// </summary>
        [JsonProperty("launch-image")]
        public string LaunchImage { get; set; }

        /// <summary>
        /// Copies this alert dictionary, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM and APNs services.
        /// </summary>
        internal ApsAlert CopyAndValidate()
        {
            var copy = new ApsAlert()
            {
                Title = this.Title,
                Subtitle = this.Subtitle,
                Body = this.Body,
                LocKey = this.LocKey,
                LocArgs = this.LocArgs?.ToList(),
                TitleLocKey = this.TitleLocKey,
                TitleLocArgs = this.TitleLocArgs?.ToList(),
                SubtitleLocKey = this.SubtitleLocKey,
                SubtitleLocArgs = this.SubtitleLocArgs?.ToList(),
                ActionLocKey = this.ActionLocKey,
                LaunchImage = this.LaunchImage,
            };
            if (copy.TitleLocArgs?.Any() == true && string.IsNullOrEmpty(copy.TitleLocKey))
            {
                throw new ArgumentException("TitleLocKey is required when specifying TitleLocArgs.");
            }

            if (copy.SubtitleLocArgs?.Any() == true && string.IsNullOrEmpty(copy.SubtitleLocKey))
            {
                throw new ArgumentException("SubtitleLocKey is required when specifying SubtitleLocArgs.");
            }

            if (copy.LocArgs?.Any() == true && string.IsNullOrEmpty(copy.LocKey))
            {
                throw new ArgumentException("LocKey is required when specifying LocArgs.");
            }

            return copy;
        }
    }
}
