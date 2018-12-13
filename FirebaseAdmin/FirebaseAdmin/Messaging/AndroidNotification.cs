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
        /// The title of the Android notification. When provided, overrides the title set
        /// via <see cref="Notification.Title"/>.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// The title of the Android notification. When provided, overrides the title set
        /// via <see cref="Notification.Body"/>.
        /// </summary>
        public string Body { get; set; }

        /// <summary>
        /// The icon of the Android notification.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The notification icon color. Must be of the form <code>#RRGGBB</code>.
        /// </summary>
        public string Color { get; set; }

        /// <summary>
        /// The sound to be played when the device receives the notification.
        /// </summary>
        public string Sound { get; set; }

        /// <summary>
        /// The notification tag. This is an identifier used to replace existing notifications in
        /// the notification drawer. If not specified, each request creates a new notification.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// The action associated with a user click on the notification. If specified, an activity
        /// with a matching Intent Filter is launched when a user clicks on the notification.
        /// </summary>
        public string ClickAction { get; set; }

        /// <summary>
        /// Sets the key of the title string in the app's string resources to use to localize the
        /// title text.
        /// <see cref="Message"/>.
        /// </summary>
        public string TitleLocKey { get; set; }

        /// <summary>
        /// The collection of resource key strings that will be used in place of the format
        /// specifiers in <see cref="TitleLocKey"/>.
        /// </summary>
        public IEnumerable<string> TitleLocArgs { get; set; }

        /// <summary>
        /// Sets the key of the body string in the app's string resources to use to localize the
        /// body text.
        /// <see cref="Message"/>.
        /// </summary>
        public string BodyLocKey { get; set; }

        /// <summary>
        /// The collection of resource key strings that will be used in place of the format
        /// specifiers in <see cref="BodyLocKey"/>.
        /// </summary>
        public IEnumerable<string> BodyLocArgs { get; set; }

        /// <summary>
        /// Sets the Android notification channel ID (new in Android O). The app must create a
        /// channel with this channel ID before any notification with this channel ID is received.
        /// If you don't send this channel ID in the request, or if the channel ID provided has
        /// not yet been created by the app, FCM uses the channel ID specified in the app manifest.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Validates the content and structure of this notification, and converts it into the
        /// <see cref="ValidatedAndroidNotification"/> type. This return type can be safely
        /// serialized into a JSON string that is acceptable to the FCM backend service.
        /// </summary>
        internal ValidatedAndroidNotification Validate()
        {
            if (Color != null) {
                if (!Regex.Match(Color, "^#[0-9a-fA-F]{6}$").Success)
                {
                    throw new ArgumentException("Color must be in the form #RRGGBB");
                }
            }
            if (TitleLocArgs != null && TitleLocArgs.Any()) {
                if (string.IsNullOrEmpty(TitleLocKey))
                {
                    throw new ArgumentException("TitleLocKey is required when specifying TitleLocArgs");
                }
            }
            if (BodyLocArgs != null && BodyLocArgs.Any()) {
                if (string.IsNullOrEmpty(BodyLocKey))
                {
                    throw new ArgumentException("BodyLocKey is required when specifying BodyLocArgs");
                }
            }
            return new ValidatedAndroidNotification()
            {
                Title = this.Title,
                Body = this.Body,
                Icon = this.Icon,
                Color = this.Color,
                Sound = this.Sound,
                Tag = this.Tag,
                ClickAction = this.ClickAction,
                TitleLocKey = this.TitleLocKey,
                TitleLocArgs = this.TitleLocArgs,
                BodyLocKey = this.BodyLocKey,
                BodyLocArgs = this.BodyLocArgs,
                ChannelId = this.ChannelId,
            };
        }
    }

    internal sealed class ValidatedAndroidNotification
    {
        [JsonProperty("title")]
        internal string Title { get; set; }

        [JsonProperty("body")]
        internal string Body { get; set; }

        [JsonProperty("icon")]
        internal string Icon { get; set; }

        [JsonProperty("color")]
        internal string Color { get; set; }

        [JsonProperty("sound")]
        internal string Sound { get; set; }

        [JsonProperty("tag")]
        internal string Tag { get; set; }

        [JsonProperty("click_action")]
        internal string ClickAction { get; set; }

        [JsonProperty("title_loc_key")]
        internal string TitleLocKey { get; set; }

        [JsonProperty("title_loc_args")]
        internal IEnumerable<string> TitleLocArgs { get; set; }

        [JsonProperty("body_loc_key")]
        internal string BodyLocKey { get; set; }

        [JsonProperty("body_loc_args")]
        internal IEnumerable<string> BodyLocArgs { get; set; }

        [JsonProperty("channel_id")]
        internal string ChannelId { get; set; }
    }
}
