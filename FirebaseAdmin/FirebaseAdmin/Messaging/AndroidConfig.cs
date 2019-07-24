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
    /// Represents the Android-specific options that can be included in a <see cref="Message"/>.
    /// </summary>
    public sealed class AndroidConfig
    {
        /// <summary>
        /// Gets or sets a collapse key for the message. Collapse key serves as an identifier for a
        /// group of messages that can be collapsed, so that only the last message gets sent when
        /// delivery can be resumed. A maximum of 4 different collapse keys may be active at any
        /// given time.
        /// </summary>
        [JsonProperty("collapse_key")]
        public string CollapseKey { get; set; }

        /// <summary>
        /// Gets or sets the priority of the message.
        /// </summary>
        [JsonIgnore]
        public Priority? Priority { get; set; }

        /// <summary>
        /// Gets or sets the time-to-live duration of the message.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// Gets or sets the package name of the application where the registration tokens must
        /// match in order to receive the message.
        /// </summary>
        [JsonProperty("restricted_package_name")]
        public string RestrictedPackageName { get; set; }

        /// <summary>
        /// Gets or sets a collection of key-value pairs that will be added to the message as data
        /// fields. Keys and the values must not be null. When set, overrides any data fields set
        /// on the top-level
        /// <see cref="Message"/>.
        /// </summary>
        [JsonProperty("data")]
        public IReadOnlyDictionary<string, string> Data { get; set; }

        /// <summary>
        /// Gets or sets the Android notification to be included in the message.
        /// </summary>
        [JsonProperty("notification")]
        public AndroidNotification Notification { get; set; }

        /// <summary>
        /// Gets or sets the FCM options to be included in the message.
        /// </summary>
        [JsonProperty("fcm_options")]
        public AndroidFcmOptions FcmOptions { get; set; }

        /// <summary>
        /// Gets or sets the string representation of <see cref="Priority"/> as accepted by the FCM
        /// backend service.
        /// </summary>
        [JsonProperty("priority")]
        private string PriorityString
        {
            get
            {
                switch (this.Priority)
                {
                    case Messaging.Priority.High:
                        return "high";
                    case Messaging.Priority.Normal:
                        return "normal";
                    default:
                        return null;
                }
            }

            set
            {
                switch (value)
                {
                    case "high":
                        this.Priority = Messaging.Priority.High;
                        return;
                    case "normal":
                        this.Priority = Messaging.Priority.High;
                        return;
                    default:
                        throw new ArgumentException(
                            $"Invalid priority value: {value}. Only 'high' and 'normal'"
                            + " are allowed.");
                }
            }
        }

        /// <summary>
        /// Gets or sets the string representation of <see cref="TimeToLive"/> as accepted by the
        /// FCM backend service. The string ends in the suffix "s" (indicating seconds) and is
        /// preceded by the number of seconds, with nanoseconds expressed as fractional seconds.
        /// </summary>
        [JsonProperty("ttl")]
        private string TtlString
        {
            get
            {
                if (this.TimeToLive == null)
                {
                    return null;
                }

                var totalSeconds = this.TimeToLive.Value.TotalSeconds;
                var seconds = (long)Math.Floor(totalSeconds);
                var subsecondNanos = (long)((totalSeconds - seconds) * 1e9);
                if (subsecondNanos > 0)
                {
                    return string.Format("{0}.{1:D9}s", seconds, subsecondNanos);
                }

                return string.Format("{0}s", seconds);
            }

            set
            {
                var segments = value.TrimEnd('s').Split('.');
                var seconds = long.Parse(segments[0]);
                var ttl = TimeSpan.FromSeconds(seconds);
                if (segments.Length == 2)
                {
                    var subsecondNanos = long.Parse(segments[1].TrimStart('0'));
                    ttl = ttl.Add(TimeSpan.FromMilliseconds(subsecondNanos / 1e6));
                }

                this.TimeToLive = ttl;
            }
        }

        /// <summary>
        /// Copies this Android config, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal AndroidConfig CopyAndValidate()
        {
            // Copy and validate the leaf-level properties
            var copy = new AndroidConfig()
            {
                CollapseKey = this.CollapseKey,
                Priority = this.Priority,
                TimeToLive = this.TimeToLive,
                RestrictedPackageName = this.RestrictedPackageName,
                Data = this.Data?.Copy(),
                FcmOptions = this.FcmOptions?.CopyAndValidate(),
            };
            var totalSeconds = copy.TimeToLive?.TotalSeconds ?? 0;
            if (totalSeconds < 0)
            {
                throw new ArgumentException("TTL must not be negative.");
            }

            // Copy and validate the child properties
            copy.Notification = this.Notification?.CopyAndValidate();
            return copy;
        }
    }
}
