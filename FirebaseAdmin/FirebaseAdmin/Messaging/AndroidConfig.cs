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
        /// A collapse key for the message. Collapse key serves as an identifier for a group of
        /// messages that can be collapsed, so that only the last message gets sent when delivery can be
        /// resumed. A maximum of 4 different collapse keys may be active at any given time.
        /// </summary>
        [JsonProperty("collapse_key")]
        public string CollapseKey { get; set; }

        /// <summary>
        /// The priority of the message.
        /// </summary>
        [JsonIgnore]
        public Priority? Priority { get; set; }

        /// <summary>
        /// String representation of the <see cref="Priority"/> as accepted by the FCM backend
        /// service.
        /// </summary>
        [JsonProperty("priority")]
        internal string PriorityString
        {
            get
            {
                switch (Priority)
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
                        Priority = Messaging.Priority.High;
                        return;
                    case "normal":
                        Priority = Messaging.Priority.High;
                        return;
                }
            }
        }

        /// <summary>
        /// The time-to-live duration of the message.
        /// </summary>
        [JsonIgnore]
        public TimeSpan? TimeToLive { get; set; }

        /// <summary>
        /// String representation of <see cref="TimeToLive"/> as accepted by the FCM backend
        /// service. The string ends in the suffix "s" (indicating seconds) and is preceded
        /// by the number of seconds, with nanoseconds expressed as fractional seconds.
        /// </summary>
        [JsonProperty("ttl")]
        internal string TtlString
        {
            get
            {
                if (TimeToLive == null)
                {
                    return null;
                }
                var totalSeconds = TimeToLive.Value.TotalSeconds;
                var seconds = (long) Math.Floor(totalSeconds);
                var subsecondNanos = (long) ((totalSeconds - seconds) * 1e9);
                if (subsecondNanos > 0)
                {
                    return String.Format("{0}.{1:D9}s", seconds, subsecondNanos);
                }
                return String.Format("{0}s", seconds);
            }
            set
            {
                var segments = value.TrimEnd('s').Split('.');
                var seconds = Int64.Parse(segments[0]);
                var ttl = TimeSpan.FromSeconds(seconds);
                if (segments.Length == 2)
                {
                    var subsecondNanos = Int64.Parse(segments[1].TrimStart('0'));
                    ttl = ttl.Add(TimeSpan.FromMilliseconds(subsecondNanos / 1e6));
                }
                TimeToLive = ttl;
            }
        }

        /// <summary>
        /// The package name of the application where the registration tokens must match in order
        /// to receive the message.
        /// </summary>
        [JsonProperty("restricted_package_name")]
        public string RestrictedPackageName { get; set; }

        /// <summary>
        /// A collection of key-value pairs that will be added to the message as data fields. Keys
        /// and the values must not be null. When set, overrides any data fields set on the top-level
        /// <see cref="Message"/>.
        /// </summary>
        [JsonProperty("data")]
        public IReadOnlyDictionary<string, string> Data { get; set; }

        /// <summary>
        /// The Android notification to be included in the message.
        /// </summary>
        [JsonProperty("notification")]
        public AndroidNotification Notification { get; set; }

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

    /// <summary>
    /// Priority levels that can be set on an <see cref="AndroidConfig"/>.
    /// </summary>
    public enum Priority
    {
        /// <summary>
        /// High priority message.
        /// </summary>
        High,

        /// <summary>
        /// Normal priority message.
        /// </summary>
        Normal,
    }
}
