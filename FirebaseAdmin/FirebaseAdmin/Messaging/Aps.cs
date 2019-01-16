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
    public sealed class Aps
    {
        [JsonIgnore]
        public ApsAlert Alert { get; set; }

        [JsonIgnore]
        public string AlertString { get; set; }

        [JsonProperty("alert")]
        private object AlertObject
        {
            get
            {
                if (Alert != null)
                {
                    return Alert;
                }
                else if (!string.IsNullOrEmpty(AlertString))
                {
                    return AlertString;
                }
                return null;
            }
            set
            {
                if (value.GetType() == typeof(string))
                {
                    AlertString = (string) value;
                }
                else
                {
                    var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
                    Alert = NewtonsoftJsonSerializer.Instance.Deserialize<ApsAlert>(json);
                }
            }
        }

        [JsonProperty("badge")]
        public int Badge { get; set; }

        [JsonIgnore]
        public string Sound { get; set; }

        [JsonIgnore]
        public CriticalSound CriticalSound { get; set; }

        [JsonProperty("sound")]
        private object SoundObject
        {
            get
            {
                if (CriticalSound != null)
                {
                    return CriticalSound;
                }
                else if (!string.IsNullOrEmpty(Sound))
                {
                    return Sound;
                }
                return null;
            }
            set
            {
                if (value.GetType() == typeof(string))
                {
                    Sound = (string) value;
                }
                else
                {
                    var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
                    CriticalSound = NewtonsoftJsonSerializer.Instance.Deserialize<CriticalSound>(json);
                }
            }
        }

        [JsonIgnore]
        public bool ContentAvailable { get; set; }

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

        [JsonIgnore]
        public bool MutableContent { get; set; }

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

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("thread-id")]
        public string ThreadId { get; set; }

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the Aps dictionary. This is
        /// exposed as an <see cref="IDictionary{TKey, TValue}"/> to support correct
        /// deserialization of custom properties.
        /// </summary>
        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

        internal Aps CopyAndValidate()
        {
            var copy = new Aps
            {
                AlertString = this.AlertString,
                Badge = this.Badge,
                Sound = this.Sound,
                ContentAvailable = this.ContentAvailable,
                MutableContent = this.MutableContent,
                Category = this.Category,
                ThreadId = this.ThreadId,
            };
            var apsAlert = this.Alert;
            if (apsAlert != null && !string.IsNullOrEmpty(copy.AlertString))
            {
                throw new ArgumentException("Multiple specifications for alert (Alert and AlertString");
            }
            var criticalSound = this.CriticalSound;
            if (criticalSound != null && !string.IsNullOrEmpty(copy.Sound))
            {
                throw new ArgumentException("Multiple specifications for sound (CriticalSound and Sound");
            }

            // Copy and validate the child properties
            copy.Alert = apsAlert?.CopyAndValidate();
            copy.CriticalSound = criticalSound?.CopyAndValidate();
            return copy;
        }
    }
}