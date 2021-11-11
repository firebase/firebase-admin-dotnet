// Copyright 2020, Google Inc. All rights reserved.
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
using System.Text;
using System.Text.RegularExpressions;
using FirebaseAdmin.Messaging.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents light settings in an Android Notification.
    /// </summary>
    public sealed class LightSettings
    {
        /// <summary>
        /// Gets or sets the lightSettingsColor value in the light settings.
        /// </summary>
        [JsonIgnore]
        public LightSettingsColor Color { get; set; }

        /// <summary>
        /// Gets or sets the light on duration in milliseconds.
        /// </summary>
        [JsonIgnore]
        public long LightOnDurationMillis { get; set; }

        /// <summary>
        /// Gets or sets the light off duration in milliseconds.
        /// </summary>
        [JsonIgnore]
        public long LightOffDurationMillis { get; set; }

        /// <summary>
        /// Gets or sets a string representation of <see cref="LightSettingsColor"/>.
        /// </summary>
        [JsonProperty("color")]
        private string LightSettingsColorString
        {
            get
            {
                var colorStringBuilder = new StringBuilder();

                colorStringBuilder
                    .Append("#")
                    .Append(Convert.ToInt32(this.Color.Red * 255).ToString("X"))
                    .Append(Convert.ToInt32(this.Color.Green * 255).ToString("X"))
                    .Append(Convert.ToInt32(this.Color.Blue * 255).ToString("X"));

                return colorStringBuilder.ToString();
            }

            set
            {
                var pattern = new Regex("^#[0-9a-fA-F]{6}$");

                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Invalid LightSettingsColor. LightSettingsColor annot be null or empty");
                }

                if (!pattern.IsMatch(value))
                {
                    throw new ArgumentException($"Invalid LightSettingsColor {value}. LightSettingsColor must be in the form #RRGGBB");
                }

                this.Color = new LightSettingsColor
                {
                    Red = Convert.ToInt32(value.Substring(1, 2), 16) / 255.0f,
                    Green = Convert.ToInt32(value.Substring(3, 2), 16) / 255.0f,
                    Blue = Convert.ToInt32(value.Substring(5, 2), 16) / 255.0f,
                    Alpha = 1.0f,
                };
            }
        }

        /// <summary>
        /// Gets or sets the string representation of <see cref="LightOnDurationMillis"/>.
        /// </summary>
        [JsonProperty("light_on_duration")]
        private string LightOnDurationMillisString
        {
            get
            {
                return TimeConverter.LongMillisToString(this.LightOnDurationMillis);
            }

            set
            {
                this.LightOnDurationMillis = TimeConverter.StringToLongMillis(value);
            }
        }

        /// <summary>
        /// Gets or sets the string representation of <see cref="LightOffDurationMillis"/>.
        /// </summary>
        [JsonProperty("light_off_duration")]
        private string LightOffDurationMillisString
        {
            get
            {
                return TimeConverter.LongMillisToString(this.LightOffDurationMillis);
            }

            set
            {
                this.LightOffDurationMillis = TimeConverter.StringToLongMillis(value);
            }
        }
    }
}
