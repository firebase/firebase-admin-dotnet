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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the Android-specific notification options that can be included in a
    /// <see cref="Message"/>.
    /// </summary>
    public sealed class AndroidNotification
    {
        /// <summary>
        /// Gets or sets the title of the Android notification. When provided, overrides the title
        /// set via <see cref="Notification.Title"/>.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the title of the Android notification. When provided, overrides the title
        /// set via <see cref="Notification.Body"/>.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the icon of the Android notification.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the notification icon color. Must be of the form <c>#RRGGBB</c>.
        /// </summary>
        [JsonProperty("color")]
        public string Color { get; set; }

        /// <summary>
        /// Gets or sets the sound to be played when the device receives the notification.
        /// </summary>
        [JsonProperty("sound")]
        public string Sound { get; set; }

        /// <summary>
        /// Gets or sets the notification tag. This is an identifier used to replace existing
        /// notifications in the notification drawer. If not specified, each request creates a new
        /// notification.
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image to be displayed in the notification.
        /// </summary>
        [JsonProperty("image")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the action associated with a user click on the notification. If specified,
        /// an activity with a matching Intent Filter is launched when a user clicks on the
        /// notification.
        /// </summary>
        [JsonProperty("click_action")]
        public string ClickAction { get; set; }

        /// <summary>
        /// Gets or sets the key of the title string in the app's string resources to use to
        /// localize the title text.
        /// </summary>
        [JsonProperty("title_loc_key")]
        public string TitleLocKey { get; set; }

        /// <summary>
        /// Gets or sets the collection of resource key strings that will be used in place of the
        /// format specifiers in <see cref="TitleLocKey"/>.
        /// </summary>
        [JsonProperty("title_loc_args")]
        public IEnumerable<string> TitleLocArgs { get; set; }

        /// <summary>
        /// Gets or sets the key of the body string in the app's string resources to use to
        /// localize the body text.
        /// </summary>
        [JsonProperty("body_loc_key")]
        public string BodyLocKey { get; set; }

        /// <summary>
        /// Gets or sets the collection of resource key strings that will be used in place of the
        /// format specifiers in <see cref="BodyLocKey"/>.
        /// </summary>
        [JsonProperty("body_loc_args")]
        public IEnumerable<string> BodyLocArgs { get; set; }

        /// <summary>
        /// Gets or sets the Android notification channel ID (new in Android O). The app must
        /// create a channel with this channel ID before any notification with this channel ID is
        /// received. If you don't send this channel ID in the request, or if the channel ID
        /// provided has not yet been created by the app, FCM uses the channel ID specified in the
        /// app manifest.
        /// </summary>
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }

        /// <summary>
        /// Copies this notification, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal AndroidNotification CopyAndValidate()
        {
            var copy = new AndroidNotification()
            {
                Title = this.Title,
                Body = this.Body,
                Icon = this.Icon,
                Color = this.Color,
                Sound = this.Sound,
                Tag = this.Tag,
                ImageUrl = this.ImageUrl,
                ClickAction = this.ClickAction,
                TitleLocKey = this.TitleLocKey,
                TitleLocArgs = this.TitleLocArgs?.ToList(),
                BodyLocKey = this.BodyLocKey,
                BodyLocArgs = this.BodyLocArgs?.ToList(),
                ChannelId = this.ChannelId,
            };
            if (copy.Color != null && !Regex.Match(copy.Color, "^#[0-9a-fA-F]{6}$").Success)
            {
                throw new ArgumentException("Color must be in the form #RRGGBB.");
            }

            if (copy.TitleLocArgs?.Any() == true && string.IsNullOrEmpty(copy.TitleLocKey))
            {
                throw new ArgumentException("TitleLocKey is required when specifying TitleLocArgs.");
            }

            if (copy.BodyLocArgs?.Any() == true && string.IsNullOrEmpty(copy.BodyLocKey))
            {
                throw new ArgumentException("BodyLocKey is required when specifying BodyLocArgs.");
            }

            if (copy.ImageUrl != null && !Uri.IsWellFormedUriString(copy.ImageUrl, UriKind.Absolute))
            {
                throw new ArgumentException($"Malformed image URL string: {copy.ImageUrl}.");
            }

            return copy;
        }
    }
}
