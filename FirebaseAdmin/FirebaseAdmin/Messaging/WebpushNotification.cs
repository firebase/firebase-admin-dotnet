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
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/notification/Notification">
    /// Web Notification specification</see>
    /// </summary>
    public sealed class WebpushNotification
    {
        /// <summary>
        /// Title text of the notification.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Body text of the notification.
        /// </summary>
        [JsonProperty("body")]
        public string Body { get; set; }

        /// <summary>
        /// The URL to the icon of the notification.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// The URL of the image used to represent the notification when there is not enough space
        /// to display the notification itself.
        /// </summary>
        [JsonProperty("badge")]
        public string Badge { get; set; }

        /// <summary>
        /// Any arbitrary data that should be associated with the notification.
        /// </summary>
        [JsonProperty("data")]
        public object Data { get; set; }

        /// <summary>
        /// The direction in which to display the notification.
        /// </summary>
        [JsonIgnore]
        public Direction? Direction { get; set; }

        /// <summary>
        /// Converts the <see cref="Direction"/> property into a string value that can be included
        /// in the json output.
        /// </summary>
        [JsonProperty("dir")]
        private string DirectionString
        {
            get
            {
                switch (Direction)
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
                        Direction = Messaging.Direction.Auto;
                        return;
                    case "ltr":
                        Direction = Messaging.Direction.LeftToRight;
                        return;
                    case "rtl":
                        Direction = Messaging.Direction.RightToLeft;
                        return;
                    default:
                        throw new FirebaseException($"Invalid direction value: {value}. Only "
                        + "'auto', 'rtl' and 'ltr' are allowed.");
                }
            }
        }

        /// <summary>
        /// The URL of an image to be displayed in the notification.
        /// </summary>
        [JsonProperty("image")]
        public string Image { get; set; }

        /// <summary>
        /// The language of the notification.
        /// </summary>
        [JsonProperty("lang")]
        public string Language { get; set; }

        /// <summary>
        /// Whether the user should be notified after a new notification replaces an old one.
        /// </summary>
        [JsonProperty("renotify")]
        public bool? Renotify { get; set; }

        /// <summary>
        /// Whether a notification should remain active until the user clicks or dismisses it,
        /// rather than closing automatically.
        /// </summary>
        [JsonProperty("requireInteraction")]
        public bool? RequireInteraction { get; set; }

        /// <summary>
        /// Whether the notification should be silent.
        /// </summary>
        [JsonProperty("silent")]
        public bool? Silent { get; set; }

        /// <summary>
        /// An identifying tag for the notification.
        /// </summary>
        [JsonProperty("tag")]
        public string Tag { get; set; }

        /// <summary>
        /// A timestamp value in milliseconds on the notification.
        /// </summary>
        [JsonProperty("timestamp")]
        public long? TimestampMillis { get; set; }

        /// <summary>
        /// A vibration pattern for the receiving device's vibration hardware to emit when the
        /// notification fires.
        /// </summary>
        [JsonProperty("vibrate")]
        public int[] Vibrate { get; set; }

        /// <summary>
        /// A collection of notification actions to be associated with the notification.
        /// </summary>
        [JsonProperty("actions")]
        public IEnumerable<Action> Actions;

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the notification. This is
        /// exposed as an <see cref="IDictionary{TKey, TValue}"/> to support correct
        /// deserialization of custom properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

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

            var customData = this.CustomData;
            if (customData?.Count > 0)
            {
                var serializer = NewtonsoftJsonSerializer.Instance;
                // Serialize the notification without CustomData for validation.
                var json = serializer.Serialize(copy);
                var dict = serializer.Deserialize<Dictionary<string, object>>(json);
                customData = new Dictionary<string, object>(customData);
                foreach (var entry in customData)
                {
                    if (dict.ContainsKey(entry.Key))
                    {
                        throw new ArgumentException(
                            $"Multiple specifications for WebpushNotification key: {entry.Key}");
                    }
                }
                copy.CustomData = customData;
            }
            return copy;
        }
    }

    /// <summary>
    /// Represents an action available to users when the notification is presented.
    /// </summary>
    public sealed class Action
    {
        /// <summary>
        /// Action name.
        /// </summary>
        [JsonProperty("action")]
        public string ActionName { get; set; }

        /// <summary>
        /// Title text.
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        /// <summary>
        /// Icon URL.
        /// </summary>
        [JsonProperty("icon")]
        public string Icon { get; set; }

        /// <summary>
        /// Creates a new Action instance.
        /// </summary>
        public Action() { }

        internal Action(Action action)
        {
            ActionName = action.ActionName;
            Title = action.Title;
            Icon = action.Icon;
        }
    }

    /// <summary>
    /// Different directions a notification can be displayed in.
    /// </summary>
    public enum Direction
    {
        /// <summary>
        /// Direction automatically determined.
        /// </summary>
        Auto,

        /// <summary>
        /// Left to right.
        /// </summary>
        LeftToRight,

        /// <summary>
        /// Right to left.
        /// </summary>
        RightToLeft,
    }
}
