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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// The sound configuration for APNs critical alerts.
    /// </summary>
    public sealed class CriticalSound
    {
        /// <summary>
        /// Gets or sets a value indicating whether to set the critical alert flag on the sound
        /// configuration.
        /// </summary>
        [JsonIgnore]
        public bool Critical { get; set; }

        /// <summary>
        /// Gets or sets the name of the sound to be played. This should be a sound file in your
        /// app's main bundle or in the <c>Library/Sounds</c> folder of your app's container
        /// directory. Specify the string <c>default</c> to play the system sound.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the volume for the critical alert's sound. Must be a value between 0.0
        /// (silent) and 1.0 (full volume).
        /// </summary>
        [JsonProperty("volume")]
        public double? Volume { get; set; }

        /// <summary>
        /// Gets or sets the integer representation of the <see cref="Critical"/> property, which
        /// is how APNs expects it.
        /// </summary>
        [JsonProperty("critical")]
        private int? CriticalInt
        {
            get
            {
                if (this.Critical)
                {
                    return 1;
                }

                return null;
            }

            set
            {
                this.Critical = value == 1;
            }
        }

        /// <summary>
        /// Copies this critical sound configuration, and validates the content of it to ensure
        /// that it can be serialized into the JSON format expected by the FCM and APNs services.
        /// </summary>
        internal CriticalSound CopyAndValidate()
        {
            var copy = new CriticalSound()
            {
                Critical = this.Critical,
                Name = this.Name,
                Volume = this.Volume,
            };
            if (string.IsNullOrEmpty(copy.Name))
            {
                throw new ArgumentException("Name must be specified for CriticalSound");
            }

            if (copy.Volume < 0 || copy.Volume > 1)
            {
                throw new ArgumentException("Volume of CriticalSound must be in the interval [0, 1]");
            }

            return copy;
        }
    }
}
