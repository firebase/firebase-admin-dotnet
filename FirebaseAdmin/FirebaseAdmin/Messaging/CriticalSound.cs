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
    /// The sound configuration for APNs critical alerts.
    /// </summary>
    public sealed class CriticalSound
    {
        /// <summary>
        /// Sets the critical alert flag on the sound configuration.
        /// </summary>
        [JsonIgnore]
        public bool Critical { get; set; }

        /// <summary>
        /// Integer representation of the <see cref="Critical"/> property, which is how
        /// APNs expects it.
        /// </summary>
        [JsonProperty("critical")]
        private int? CriticalInt
        {
            get
            {
                if (Critical)
                {
                    return 1;
                }
                return null;
            }
            set
            {
                Critical = (value == 1);
            }
        }

        /// <summary>
        /// The name of a sound file in your app's main bundle or in the
        /// <code>Library/Sounds</code> folder of your app's container directory. Specify the
        /// string <code>default</code> to play the system sound.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The volume for the critical alert's sound. Must be a value between 0.0 (silent) and
        /// 1.0 (full volume).
        /// </summary>
        [JsonProperty("volume")]
        public double Volume { get; set; }

        /// <summary>
        /// Copies this critical sound configuration, and validates the content of it to ensure
        /// that it can be serialized into the JSON format expected by the FCM and APNs services.
        /// </summary>
        internal CriticalSound CopyAndValidate()
        {
            if (Volume < 0 || Volume > 1)
            {
                throw new ArgumentException("Volume must be in the interval [0, 1]");
            }
            return new CriticalSound()
            {
                Critical = this.Critical,
                Name = this.Name,
                Volume = this.Volume,
            };
        }
    }
}