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
        public string Body { get; set; }

        /// <summary>
        /// The URL to the icon of the notification.
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The URL of the image used to represent the notification when there is not enough space
        /// to display the notification itself.
        /// </summary>
        public string Badge { get; set; }

        /// <summary>
        /// Any arbitrary data that should be associated with the notification.
        /// </summary>
        public object Data { get; set; }

        /// <summary>
        /// The direction in which to display the notification.
        /// </summary>
        public Direction? Direction { get; set; }

        /// <summary>
        /// Converts the <see cref="Direction"/> property into a string value that can be included
        /// in the json output.
        /// </summary>
        internal string DirectionString
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
        }

        /// <summary>
        /// The URL of an image to be displayed in the notification.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// The language of the notification.
        /// </summary>
        public string Language { get; set; }

        /// <summary>
        /// Whether the user should be notified after a new notification replaces an old one.
        /// </summary>
        public bool? Renotify { get; set; }

        /// <summary>
        /// Whether a notification should remain active until the user clicks or dismisses it,
        /// rather than closing automatically.
        /// </summary>
        public bool? RequireInteraction { get; set; }

        /// <summary>
        /// Whether the notification should be silent.
        /// </summary>
        public bool? Silent { get; set; }

        /// <summary>
        /// An identifying tag for the notification.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// A timestamp value in milliseconds on the notification.
        /// </summary>
        public long? TimestampMillis { get; set; }

        /// <summary>
        /// A vibration pattern for the receiving device's vibration hardware to emit when the
        /// notification fires.
        /// </summary>
        public int[] Vibrate { get; set; }

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the notification.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomData;

        /// <summary>
        /// A collection of notification actions to be associated with the notification.
        /// </summary>
        [JsonProperty("actions")]
        public IEnumerable<Action> Actions;

        private delegate void AddString(string key, string value);
        private delegate void AddObject(string key, object value);

        /// <summary>
        /// Validates the content and structure of this Webpush notification, and converts it into
        /// a dictionary.
        /// </summary>
        internal IReadOnlyDictionary<string, object> Validate()
        {
            var result = new Dictionary<string, object>();
            AddString addString = delegate(string key, string value)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    result[key] = value;
                }
            };
            AddObject addObject = delegate(string key, object value)
            {
                if (value != null)
                {
                    result[key] = value;
                }
            };
            addString("title", Title);
            addString("body", Body);
            addString("icon", Icon);
            addString("image", Image);
            addString("lang", Language);
            addString("tag", Tag);
            addString("dir", DirectionString);
            addString("badge", Badge);
            addObject("renotify", Renotify);
            addObject("requireInteraction", RequireInteraction);
            addObject("silent", Silent);
            addObject("actions", Actions);
            addObject("vibrate", Vibrate);
            addObject("timestamp", TimestampMillis);
            addObject("data", Data);
            if (CustomData != null)
            {
                foreach (var entry in CustomData)
                {
                    if (result.ContainsKey(entry.Key))
                    {
                        throw new ArgumentException($"Multiple specification for key {entry.Key}");
                    }
                    addObject(entry.Key, entry.Value);
                }
            }
            return result;
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
