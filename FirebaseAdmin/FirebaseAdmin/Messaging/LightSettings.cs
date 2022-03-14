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
        private LightSettingsColor lightColor = new LightSettingsColor();

        /// <summary>
        /// Gets or sets the color value of the light settings.
        /// </summary>
        [JsonIgnore]
        public string Color
        {
            get
            {
                return this.lightColor.ColorString();
            }

            set
            {
                this.lightColor = LightSettingsColor.FromString(value);
            }
        }

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
        /// Gets or sets the light settings color representation as accepted by the FCM backend service.
        /// </summary>
        [JsonProperty("color")]
        private LightSettingsColor LightColor
        {
            get
            {
                return this.lightColor;
            }

            set
            {
                this.lightColor = value;
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

        /// <summary>
        /// Copies this <see cref="LightSettings"/> object, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal LightSettings CopyAndValidate()
        {
            // Copy and validate the leaf-level properties
            var copy = new LightSettings()
            {
                Color = this.Color,
                LightOnDurationMillis = this.LightOnDurationMillis,
                LightOffDurationMillis = this.LightOffDurationMillis,
            };

            return copy;
        }

        /// <summary>
        /// Represents the light settings color as expected by the FCM backend service.
        /// </summary>
        private class LightSettingsColor
        {
            /// <summary>
            /// Gets or sets the red component.
            /// </summary>
            [JsonProperty("red")]
            internal float Red { get; set; }

            /// <summary>
            /// Gets or sets the green component.
            /// </summary>
            [JsonProperty("green")]
            internal float Green { get; set; }

            /// <summary>
            /// Gets or sets the blue component.
            /// </summary>
            [JsonProperty("blue")]
            internal float Blue { get; set; }

            /// <summary>
            /// Gets or sets the alpha component.
            /// </summary>
            [JsonProperty("alpha")]
            internal float Alpha { get; set; }

            internal static LightSettingsColor FromString(string color)
            {
                if (string.IsNullOrEmpty(color))
                {
                    throw new ArgumentException("Light settings color must not be null or empty");
                }

                if (!Regex.Match(color, "^#[0-9a-fA-F]{6}$").Success && !Regex.Match(color, "^#[0-9a-fA-F]{8}$").Success)
                {
                    throw new ArgumentException($"Invalid Light Settings Color {color}. Must be in the form of #RRGGBB or #RRGGBBAA.");
                }

                var colorString = color.Length == 7 ? color + "FF" : color;

                return new LightSettingsColor()
                {
                    Red = Convert.ToInt32(colorString.Substring(1, 2), 16) / 255.0f,
                    Green = Convert.ToInt32(colorString.Substring(3, 2), 16) / 255.0f,
                    Blue = Convert.ToInt32(colorString.Substring(5, 2), 16) / 255.0f,
                    Alpha = Convert.ToInt32(colorString.Substring(7, 2), 16) / 255.0f,
                };
            }

            internal string ColorString()
            {
                var colorStringBuilder = new StringBuilder();

                colorStringBuilder
                    .Append("#")
                    .Append(Convert.ToInt32(this.Red * 255).ToString("X"))
                    .Append(Convert.ToInt32(this.Green * 255).ToString("X"))
                    .Append(Convert.ToInt32(this.Blue * 255).ToString("X"))
                    .Append(Convert.ToInt32(this.Alpha * 255).ToString("X"));

                return colorStringBuilder.ToString();
            }
        }
    }
}
