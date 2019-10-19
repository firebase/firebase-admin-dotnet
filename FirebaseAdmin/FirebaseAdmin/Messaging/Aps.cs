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
    /// Represents the <a href="https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/PayloadKeyReference.html">
    /// aps dictionary</a> that is part of every APNs message.
    /// </summary>
    public sealed class Aps
    {
        private static readonly NewtonsoftJsonSerializer Serializer = NewtonsoftJsonSerializer.Instance;

        /// <summary>
        /// Gets or sets an advanced alert configuration to be included in the message. It is an
        /// error to set both <see cref="Alert"/> and <see cref="AlertString"/> properties
        /// together.
        /// </summary>
        [JsonIgnore]
        public ApsAlert Alert { get; set; }

        /// <summary>
        /// Gets or sets the alert text to be included in the message. To specify a more advanced
        /// alert configuration, use the <see cref="Alert"/> property instead. It is an error to
        /// set both <see cref="Alert"/> and <see cref="AlertString"/> properties together.
        /// </summary>
        [JsonIgnore]
        public string AlertString { get; set; }

        /// <summary>
        /// Gets or sets the badge to be displayed with the message. Set to 0 to remove the badge.
        /// When not specified, the badge will remain unchanged.
        /// </summary>
        [JsonProperty("badge")]
        public int? Badge { get; set; }

        /// <summary>
        /// Gets or sets the name of a sound file in your app's main bundle or in the
        /// <c>Library/Sounds</c> folder of your app's container directory. Specify the
        /// string <c>default</c> to play the system sound. It is an error to set both
        /// <see cref="Sound"/> and <see cref="CriticalSound"/> properties together.
        /// </summary>
        [JsonIgnore]
        public string Sound { get; set; }

        /// <summary>
        /// Gets or sets the critical alert sound to be played with the message. It is an error to
        /// set both <see cref="Sound"/> and <see cref="CriticalSound"/> properties together.
        /// </summary>
        [JsonIgnore]
        public CriticalSound CriticalSound { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to configure a background update notification.
        /// </summary>
        [JsonIgnore]
        public bool ContentAvailable { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to include the <c>mutable-content</c> property
        /// in the message. When set, this property allows clients to modify the notification via
        /// app extensions.
        /// </summary>
        [JsonIgnore]
        public bool MutableContent { get; set; }

        /// <summary>
        /// Gets or sets the type of the notification.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// Gets or sets the app-specific identifier for grouping notifications.
        /// </summary>
        [JsonProperty("thread-id")]
        public string ThreadId { get; set; }

        /// <summary>
        /// Gets or sets a collection of arbitrary key-value data to be included in the <c>aps</c>
        /// dictionary. This is exposed as an <see cref="IDictionary{TKey, TValue}"/> to support
        /// correct deserialization of custom properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// Gets or sets the alert configuration of the <c>aps</c> dictionary. Read from either
        /// <see cref="Alert"/> or <see cref="AlertString"/> property.
        /// </summary>
        [JsonProperty("alert")]
        private object AlertObject
        {
            get
            {
                object alert = this.AlertString;
                if (string.IsNullOrEmpty(alert as string))
                {
                    alert = this.Alert;
                }
                else if (this.Alert != null)
                {
                    throw new ArgumentException(
                        "Multiple specifications for alert (Alert and AlertString");
                }

                return alert;
            }

            set
            {
                if (value == null)
                {
                    return;
                }
                else if (value.GetType() == typeof(string))
                {
                    this.AlertString = (string)value;
                }
                else if (value.GetType() == typeof(ApsAlert))
                {
                    this.Alert = (ApsAlert)value;
                }
                else
                {
                    var json = Serializer.Serialize(value);
                    this.Alert = Serializer.Deserialize<ApsAlert>(json);
                }
            }
        }

        /// <summary>
        /// Gets or sets the sound configuration of the <c>aps</c> dictionary. Read from either
        /// <see cref="Sound"/> or <see cref="CriticalSound"/> property.
        /// </summary>
        [JsonProperty("sound")]
        private object SoundObject
        {
            get
            {
                object sound = this.Sound;
                if (string.IsNullOrEmpty(sound as string))
                {
                    sound = this.CriticalSound;
                }
                else if (this.CriticalSound != null)
                {
                    throw new ArgumentException(
                        "Multiple specifications for sound (CriticalSound and Sound");
                }

                return sound;
            }

            set
            {
                if (value == null)
                {
                    return;
                }
                else if (value.GetType() == typeof(string))
                {
                    this.Sound = (string)value;
                }
                else if (value.GetType() == typeof(CriticalSound))
                {
                    this.CriticalSound = (CriticalSound)value;
                }
                else
                {
                    var json = Serializer.Serialize(value);
                    this.CriticalSound = Serializer.Deserialize<CriticalSound>(json);
                }
            }
        }

        /// <summary>
        /// Gets or sets the integer representation of the <see cref="ContentAvailable"/> property,
        /// which is how APNs expects it.
        /// </summary>
        [JsonProperty("content-available")]
        private int? ContentAvailableInt
        {
            get
            {
                return this.ContentAvailable ? 1 : (int?)null;
            }

            set
            {
                this.ContentAvailable = value == 1;
            }
        }

        /// <summary>
        /// Gets or sets the integer representation of the <see cref="MutableContent"/> property,
        /// which is how APNs expects it.
        /// </summary>
        [JsonProperty("mutable-content")]
        private int? MutableContentInt
        {
            get
            {
                return this.MutableContent ? 1 : (int?)null;
            }

            set
            {
                this.MutableContent = value == 1;
            }
        }

        /// <summary>
        /// Copies this Aps dictionary, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM and APNs services.
        /// </summary>
        internal Aps CopyAndValidate()
        {
            var copy = new Aps
            {
                AlertObject = this.AlertObject,
                Badge = this.Badge,
                ContentAvailable = this.ContentAvailable,
                MutableContent = this.MutableContent,
                Category = this.Category,
                SoundObject = this.SoundObject,
                ThreadId = this.ThreadId,
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
                        $"Multiple specifications for Aps keys: {string.Join(",", duplicates)}");
                }

                copy.CustomData = customData;
            }

            copy.Alert = copy.Alert?.CopyAndValidate();
            copy.CriticalSound = copy.CriticalSound?.CopyAndValidate();
            return copy;
        }
    }
}
