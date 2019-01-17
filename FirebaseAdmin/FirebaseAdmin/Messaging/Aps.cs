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
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the <see href="https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/PayloadKeyReference.html">
    /// aps dictionary</see> that is part of every APNs message.
    /// </summary>
    public sealed class Aps
    {
        /// <summary>
        /// An advanced alert configuration to be included in the message. It is an error to set
        /// both <see cref="Alert"/> and <see cref="AlertString"/> properties together.
        /// </summary>
        [JsonIgnore]
        public ApsAlert Alert { get; set; }

        /// <summary>
        /// The alert text to be included in the message. To specify a more advanced alert
        /// configuration, use the <see cref="Alert"/> property instead. It is an error to set
        /// both <see cref="Alert"/> and <see cref="AlertString"/> properties together.
        /// </summary>
        [JsonIgnore]
        public string AlertString { get; set; }

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
                    AlertString = (string) value;
                }
                else if (value.GetType() == typeof(ApsAlert))
                {
                    Alert = (ApsAlert) value;
                }
                else
                {
                    var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
                    Alert = NewtonsoftJsonSerializer.Instance.Deserialize<ApsAlert>(json);
                }
            }
        }

        /// <summary>
        /// The badge to be displayed with the message. Set to 0 to remove the badge. When not
        /// specified, the badge will remain unchanged.
        /// </summary>
        [JsonProperty("badge")]
        public int? Badge { get; set; }

        /// <summary>
        /// The name of a sound file in your app's main bundle or in the
        /// <code>Library/Sounds</code> folder of your app's container directory. Specify the
        /// string <code>default</code> to play the system sound. It is an error to set both
        /// <see cref="Sound"/> and <see cref="CriticalSound"/> properties together.
        /// </summary>
        [JsonIgnore]
        public string Sound { get; set; }

        /// <summary>
        /// The critical alert sound to be played with the message. It is an error to set both
        /// <see cref="Sound"/> and <see cref="CriticalSound"/> properties together.
        /// </summary>
        [JsonIgnore]
        public CriticalSound CriticalSound { get; set; }

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
                    Sound = (string) value;
                }
                else if (value.GetType() == typeof(CriticalSound))
                {
                    CriticalSound = (CriticalSound) value;
                }
                else
                {
                    var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
                    CriticalSound = NewtonsoftJsonSerializer.Instance.Deserialize<CriticalSound>(json);
                }
            }
        }

        /// <summary>
        /// Specifies whether to configure a background update notification.
        /// </summary>
        [JsonIgnore]
        public bool ContentAvailable { get; set; }

        /// <summary>
        /// Integer representation of the <see cref="ContentAvailable"/> property, which is how
        /// APNs expects it.
        /// </summary>
        [JsonProperty("content-available")]
        private int? ContentAvailableInt
        {
            get
            {
                if (ContentAvailable)
                {
                    return 1;
                }
                return null;
            }
            set
            {
                ContentAvailable = (value == 1);
            }
        }

        /// <summary>
        /// Specifies whether to set the <code>mutable-content</code> property on the message. When
        /// set, this property allows clients to modify the notification via app extensions.
        /// </summary>
        [JsonIgnore]
        public bool MutableContent { get; set; }

        /// <summary>
        /// Integer representation of the <see cref="MutableContent"/> property, which is how
        /// APNs expects it.
        /// </summary>
        [JsonProperty("mutable-content")]
        private int? MutableContentInt
        {
            get
            {
                if (MutableContent)
                {
                    return 1;
                }
                return null;
            }
            set
            {
                MutableContent = (value == 1);
            }
        }

        /// <summary>
        /// The type of the notification.
        /// </summary>
        [JsonProperty("category")]
        public string Category { get; set; }

        /// <summary>
        /// An app-specific identifier for grouping notifications.
        /// </summary>
        [JsonProperty("thread-id")]
        public string ThreadId { get; set; }

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the <code>aps</code>
        /// dictionary. This is exposed as an <see cref="IDictionary{TKey, TValue}"/> to support
        /// correct deserialization of custom properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

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
                SoundObject = this.SoundObject,
                ContentAvailable = this.ContentAvailable,
                MutableContent = this.MutableContent,
                Category = this.Category,
                ThreadId = this.ThreadId,
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
                            $"Multiple specifications for Aps key: {entry.Key}");
                    }
                }
                copy.CustomData = customData;
            }

            // Copy and validate the child properties
            copy.Alert = copy.Alert?.CopyAndValidate();
            copy.CriticalSound = copy.CriticalSound?.CopyAndValidate();
            return copy;
        }
    }
}