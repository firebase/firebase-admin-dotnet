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
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the Webpush-specific notification options that can be included in a
    /// <see cref="Message"/>. Supports most standard options defined in the
    /// <a href="https://developer.mozilla.org/en-US/docs/Web/API/notification/Notification">
    /// Web Notification specification</a>.
    /// </summary>
    public sealed class WebpushNotification
    {
        /// <summary>
        /// Gets or sets the title text of the notification.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the body text of the notification.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// Gets or sets the URL to the icon of the notification.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the URL of the image used to represent the notification when there is not
        /// enough space to display the notification itself.
        /// </summary>
        [JsonProperty("badge")]
        public string Badge { get; set; }

        /// <summary>
        /// Gets or sets some arbitrary data that will be included in the notification.
        /// </summary>
        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the direction in which to display the notification.
        /// </summary>
        [JsonIgnore]
        public Direction? Direction { get; set; }

        /// <summary>
        /// Gets or sets the URL of an image to be displayed in the notification.
        /// </summary>
        [JsonProperty("image")]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the language of the notification.
        /// </summary>
        [JsonProperty("lang")]
        public string Language { get; set; }

        /// <summary>
        /// Gets or sets whether the user should be notified after a new notification replaces an
        /// old one.
        /// </summary>
        [JsonProperty("renotify")]
        public bool? Renotify { get; set; }

        /// <summary>
        /// Gets or sets whether the notification should remain active until the user clicks or
        /// dismisses it, rather than closing it automatically.
        /// </summary>
        [JsonProperty("requireInteraction")]
        public bool? RequireInteraction { get; set; }

        /// <summary>
        /// Gets or sets whether the notification should be silent.
        /// </summary>
        [JsonProperty("silent")]
        public bool? Silent { get; set; }

        /// <summary>
        /// Gets or sets an identifying tag for the notification.
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the notification's timestamp value in milliseconds.
        /// </summary>
        [JsonProperty("timestamp")]
        public long? TimestampMillis { get; set; }

        /// <summary>
        /// Gets or sets a vibration pattern for the receiving device's vibration hardware.
        /// </summary>
        [JsonProperty("vibrate")]
        public int[] Vibrate { get; set; }

        /// <summary>
        /// Gets or sets a collection of Webpush notification actions.
        /// </summary>
        [JsonProperty("actions")]
        public IEnumerable<Action> Actions { get; set; }

        /// <summary>
        /// Gets or sets the custom key-value pairs that will be included in the
        /// notification. This is exposed as an <see cref="IDictionary{TKey, TValue}"/> to support
        /// correct deserialization of custom properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// Gets or sets the string representation of the <see cref="Direction"/> property.
        /// </summary>
        [JsonProperty("dir")]
        private string DirectionString
        {
            get
            {
                switch (this.Direction)
                {
                    case Messaging.Direction.Auto:
                        return "auto";
                    case Messaging.Direction.LeftToRight:
                        return "ltr";
                    case Messaging.Direction.RightToLeft:
                        return "rtl";
                    default:
                        return null;
                }
            }

            set
            {
                switch (value)
                {
                    case "auto":
                        this.Direction = Messaging.Direction.Auto;
                        return;
                    case "ltr":
                        this.Direction = Messaging.Direction.LeftToRight;
                        return;
                    case "rtl":
                        this.Direction = Messaging.Direction.RightToLeft;
                        return;
                    default:
                        throw new ArgumentException(
                            $"Invalid direction value: {value}. Only 'auto', 'rtl' and 'ltr' "
                            + "are allowed.");
                }
            }
        }

        /// <summary>
        /// Copies this Webpush notification, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal WebpushNotification CopyAndValidate()
        {
            var copy = new WebpushNotification()
            {
                Title = this.Title,
                Body = this.Body,
                Icon = this.Icon,
                Image = this.Image,
                Language = this.Language,
                Tag = this.Tag,
                Direction = this.Direction,
                Badge = this.Badge,
                Renotify = this.Renotify,
                RequireInteraction = this.RequireInteraction,
                Silent = this.Silent,
                Actions = this.Actions?.Select((item, _) => new Action(item)).ToList(),
                Vibrate = this.Vibrate,
                TimestampMillis = this.TimestampMillis,
                Data = this.Data,
            };

            var customData = this.CustomData?.ToDictionary(e => e.Key, e => e.Value);
            if (customData?.Count > 0)
            {
                var serializer = NewtonsoftJsonSerializer.Instance;
                var json = serializer.Serialize(copy);
                var standardProperties = serializer.Deserialize<Dictionary<string, object>>(json);
                var duplicates = customData.Keys
                    .Where(customKey => standardProperties.ContainsKey(customKey))
                    .ToList();
                if (duplicates.Any())
                {
                    throw new ArgumentException(
                        "Multiple specifications for WebpushNotification keys: "
                        + string.Join(",", duplicates));
                }

                copy.CustomData = customData;
            }

            return copy;
        }
    }
}
