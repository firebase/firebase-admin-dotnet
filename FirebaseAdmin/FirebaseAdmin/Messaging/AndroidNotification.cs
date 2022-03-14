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
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using FirebaseAdmin.Messaging.Util;
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
        /// Gets or sets the "ticker" text which is sent to accessibility services. Prior to API level 21
        /// (Lollipop), gets or sets the text that is displayed in the status bar when the notification
        /// first arrives.
        /// </summary>
        [JsonProperty("ticker")]
        public string Ticker { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the notification is automatically dismissed
        /// or persists when the user clicks it in the panel. When set to false,
        /// the notification is automatically dismissed. When set to true, the notification persists.
        /// </summary>
        [JsonProperty("sticky")]
        public bool Sticky { get; set; }

        /// <summary>
        /// Gets or sets the time that the event in the notification occurred for notifications
        /// that inform users about events with an absolute time reference. Notifications in the panel
        /// are sorted by this time.
        /// </summary>
        [JsonIgnore]
        public DateTime EventTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not this notification is relevant only to
        /// the current device. Some notifications can be bridged to other devices for remote display,
        /// such as a Wear OS watch. This hint can be set to recommend this notification not be bridged.
        /// See <a href="https://developer.android.com/training/wearables/notifications/bridger#existing-method-of-preventing-bridging">Wear OS guides</a>.
        /// </summary>
        [JsonProperty("local_only")]
        public bool LocalOnly { get; set; }

        /// <summary>
        /// Gets or sets the relative priority for this notification. Priority is an indication of how much of
        /// the user's attention should be consumed by this notification. Low-priority notifications
        /// may be hidden from the user in certain situations, while the user might be interrupted
        /// for a higher-priority notification.
        /// </summary>
        [JsonIgnore]
        public NotificationPriority? Priority { get; set; }

        /// <summary>
        /// Gets or sets a list of vibration timings in milliseconds in the array to use. The first value in the
        /// array indicates the duration to wait before turning the vibrator on. The next value
        /// indicates the duration to keep the vibrator on. Subsequent values alternate between
        /// duration to turn the vibrator off and to turn the vibrator on. If <see cref="VibrateTimingsMillis"/> is set and
        /// <see cref="DefaultVibrateTimings"/> is set to true, the default value is used instead of
        /// the user-specified <see cref="VibrateTimingsMillis"/>. A duration in seconds with up to nine fractional digits,
        /// terminated by 's'.Example: "3.5s".
        /// </summary>
        [JsonIgnore]
        public long[] VibrateTimingsMillis { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the default vibration timings. If set to true, use the Android
        /// Sets the whether to use the default vibration timings. If set to true, use the Android
        ///  in <a href="https://android.googlesource.com/platform/frameworks/base/+/master/core/res/res/values/config.xml">config.xml</a>.
        ///  If <see cref="DefaultVibrateTimings"/> is set to true and <see cref="VibrateTimingsMillis"/> is also set,
        ///  the default value is used instead of the user-specified <see cref="VibrateTimingsMillis"/>.
        /// </summary>
        [JsonProperty("default_vibrate_timings")]
        public bool DefaultVibrateTimings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the default sound. If set to true, use the Android framework's
        /// default sound for the notification. Default values are specified in config.xml.
        /// </summary>
        [JsonProperty("default_sound")]
        public bool DefaultSound { get; set; }

        /// <summary>
        /// Gets or sets the settings to control the notification's LED blinking rate and color if LED is
        /// available on the device. The total blinking time is controlled by the OS.
        /// </summary>
        [JsonProperty("light_settings")]
        public LightSettings LightSettings { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not to use the default light settings.
        /// If set to true, use the Android framework's default LED light settings for the notification. Default values are
        /// specified in config.xml. If <see cref="DefaultLightSettings"/> is set to true and <see cref="LightSettings"/> is also set,
        /// the user-specified <see cref="LightSettings"/> is used instead of the default value.
        /// </summary>
        [JsonProperty("default_light_settings")]
        public bool DefaultLightSettings { get; set; }

        /// <summary>
        /// Gets or sets the visibility of this notification.
        /// </summary>
        [JsonIgnore]
        public NotificationVisibility? Visibility { get; set; }

        /// <summary>
        /// Gets or sets the number of items this notification represents. May be displayed as a badge
        /// count for launchers that support badging. If not invoked then notification count is left unchanged.
        /// For example, this might be useful if you're using just one notification to represent
        /// multiple new messages but you want the count here to represent the number of total
        /// new messages.If zero or unspecified, systems that support badging use the default,
        /// which is to increment a number displayed on the long-press menu each time a new notification arrives.
        /// </summary>
        [JsonProperty("notification_count")]
        public int? NotificationCount { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the <see cref="NotificationPriority"/> property.
        /// </summary>
        [JsonProperty("notification_priority")]
        private string PriorityString
        {
            get
            {
                switch (this.Priority)
                {
                    case NotificationPriority.MIN:
                        return "PRIORITY_MIN";
                    case NotificationPriority.LOW:
                        return "PRIORITY_LOW";
                    case NotificationPriority.DEFAULT:
                        return "PRIORITY_DEFAULT";
                    case NotificationPriority.HIGH:
                        return "PRIORITY_HIGH";
                    case NotificationPriority.MAX:
                        return "PRIORITY_MAX";
                    default:
                        return null;
                }
            }

            set
            {
                switch (value)
                {
                    case "PRIORITY_MIN":
                        this.Priority = NotificationPriority.MIN;
                        return;
                    case "PRIORITY_LOW":
                        this.Priority = NotificationPriority.LOW;
                        return;
                    case "PRIORITY_DEFAULT":
                        this.Priority = NotificationPriority.DEFAULT;
                        return;
                    case "PRIORITY_HIGH":
                        this.Priority = NotificationPriority.HIGH;
                        return;
                    case "PRIORITY_MAX":
                        this.Priority = NotificationPriority.MAX;
                        return;
                    default:
                        throw new ArgumentException(
                            $"Invalid priority value: {value}. Only 'PRIORITY_MIN', 'PRIORITY_LOW', ''PRIORITY_DEFAULT' "
                            + "'PRIORITY_HIGH','PRIORITY_MAX' are allowed.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the string representation of the <see cref="NotificationVisibility"/> property.
        /// </summary>
        [JsonProperty("visibility")]
        private string VisibilityString
        {
            get
            {
                switch (this.Visibility)
                {
                    case NotificationVisibility.PUBLIC:
                        return "PUBLIC";
                    case NotificationVisibility.PRIVATE:
                        return "PRIVATE";
                    case NotificationVisibility.SECRET:
                        return "SECRET";
                    default:
                        return null;
                }
            }

            set
            {
                switch (value)
                {
                    case "PUBLIC":
                        this.Visibility = NotificationVisibility.PUBLIC;
                        return;
                    case "PRIVATE":
                        this.Visibility = NotificationVisibility.PRIVATE;
                        return;
                    case "SECRET":
                        this.Visibility = NotificationVisibility.SECRET;
                        return;
                    default:
                        throw new ArgumentException(
                            $"Invalid visibility value: {value}. Only 'PUBLIC', 'PRIVATE', ''SECRET' are allowed.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the string representation of the <see cref="VibrateTimingsMillis"/> property.
        /// </summary>
        [JsonProperty("vibrate_timings")]
        private List<string> VibrateTimingsString
        {
            get
            {
                var timingsStringList = new List<string>();
                if (this.VibrateTimingsMillis == null)
                {
                    return null;
                }

                foreach (var value in this.VibrateTimingsMillis)
                {
                    timingsStringList.Add(TimeConverter.LongMillisToString(value));
                }

                return timingsStringList;
            }

            set
            {
                if (value.Count == 0)
                {
                    throw new ArgumentException("Invalid VibrateTimingsMillis. VibrateTimingsMillis should be a non-empty list of strings");
                }

                var timingsLongList = new List<long>();

                foreach (var timingString in value)
                {
                    timingsLongList.Add(TimeConverter.StringToLongMillis(timingString));
                }

                this.VibrateTimingsMillis = timingsLongList.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets the string representation of the <see cref="EventTimestamp"/> property.
        /// </summary>
        [JsonProperty("event_time")]
        private string EventTimeString
        {
            get
            {
                return this.EventTimestamp.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.ffffff000'Z'");
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Invalid event timestamp. Event timestamp should be a non-empty string");
                }

                this.EventTimestamp = DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.None);
            }
        }

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
                Ticker = this.Ticker,
                Sticky = this.Sticky,
                EventTimestamp = this.EventTimestamp,
                LocalOnly = this.LocalOnly,
                Priority = this.Priority,
                VibrateTimingsMillis = this.VibrateTimingsMillis,
                DefaultVibrateTimings = this.DefaultVibrateTimings,
                DefaultSound = this.DefaultSound,
                DefaultLightSettings = this.DefaultLightSettings,
                Visibility = this.Visibility,
                NotificationCount = this.NotificationCount,
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

            copy.LightSettings = this.LightSettings?.CopyAndValidate();

            return copy;
        }
    }
}
