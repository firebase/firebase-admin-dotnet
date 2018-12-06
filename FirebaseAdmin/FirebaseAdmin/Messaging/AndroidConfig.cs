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
        public string CollapseKey { internal get; set; }

        /// <summary>
        /// The priority of the message.
        /// </summary>
        public Priority? Priority { internal get; set; }

        /// <summary>
        /// The time-to-live duration of the message.
        /// </summary>
        public TimeSpan TimeToLive { internal get; set; }

        /// <summary>
        /// The package name of the application where the registration tokens must match in order
        /// to receive the message.
        /// </summary>
        public string RestrictedPackageName { internal get; set; }

        /// <summary>
        /// A collection of key-value pairs that will be added to the message as data fields. Keys
        /// and the values must not be null. When set, overrides any data fields set on the top-level
        /// <see cref="Message"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { internal get; set; }

        internal ValidatedAndroidConfig Validate()
        {
            return new ValidatedAndroidConfig()
            {
                CollapseKey = this.CollapseKey,
                Priority = this.PriorityString,
                TimeToLive = this.TtlString,
                RestrictedPackageName = this.RestrictedPackageName,
                Data = this.Data,
            };
        }

        private string PriorityString
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
        }

        private string TtlString
        {
            get
            {
                if (TimeToLive == null)
                {
                    return null;
                }
                var totalSeconds = TimeToLive.TotalSeconds;
                if (totalSeconds < 0)
                {
                    throw new ArgumentException("TTL must not be negative.");
                }
                var seconds = (long) Math.Floor(totalSeconds);
                var subsecondNanos = (long) ((totalSeconds - seconds) * 1e9);
                if (subsecondNanos > 0)
                {
                    return String.Format("{0}.{1:D9}s", seconds, subsecondNanos);
                }
                return String.Format("{0}s", seconds);
            }
        }
    }

    internal sealed class ValidatedAndroidConfig
    {
        [JsonProperty("collapse_key")]
        internal string CollapseKey { get; set; }
        
        [JsonProperty("priority")]
        internal string Priority { get; set; }
        
        [JsonProperty("ttl")]
        internal string TimeToLive { get; set; }

        [JsonProperty("restricted_package_name")]
        internal string RestrictedPackageName { get; set; }

        [JsonProperty("data")]
        internal IReadOnlyDictionary<string, string> Data { get; set; }
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