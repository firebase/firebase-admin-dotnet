// Copyright 2026, Google Inc. All rights reserved.
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
using System.Globalization;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the Android-specific options that can be included in a <see cref="Message"/> for V2 configuration.
    /// </summary>
    public sealed class AndroidConfigV2
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
        /// Gets or sets a boolean indicating whether messages will be allowed to be delivered to
        /// the app while the device is in direct boot mode.
        /// </summary>
        [JsonProperty("direct_boot_ok")]
        public bool? DirectBootOk { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether messages will be allowed to be delivered to
        /// the app while the device is on a bandwidth constrained network.
        /// </summary>
        [JsonProperty("bandwidth_constrained_ok")]
        public bool? BandwidthConstrainedOk { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating whether messages will be allowed to be delivered to
        /// the app while the device is on a restricted satellite network.
        /// </summary>
        [JsonProperty("restricted_satellite_ok")]
        public bool? RestrictedSatelliteOk { get; set; }

        /// <summary>
        /// Gets or sets the RemoteNotification payload configuration.
        /// </summary>
        [JsonProperty("remote_notification")]
        public AndroidRemoteNotification RemoteNotification { get; set; }

        /// <summary>
        /// Gets or sets the BackgroundSync payload configuration.
        /// </summary>
        [JsonProperty("background_sync")]
        public AndroidBackgroundSyncMessage BackgroundSync { get; set; }

        /// <summary>
        /// Gets or sets the FCM options to be included in the message.
        /// </summary>
        [JsonProperty("fcm_options")]
        public AndroidFcmOptions FcmOptions { get; set; }

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

                var ticks = this.TimeToLive.Value.Ticks;
                var seconds = ticks / TimeSpan.TicksPerSecond;
                var subsecondNanos = (ticks % TimeSpan.TicksPerSecond) * 100;
                if (subsecondNanos > 0)
                {
                    return string.Format(CultureInfo.InvariantCulture, "{0}.{1:D9}s", seconds, subsecondNanos);
                }

                return string.Format(CultureInfo.InvariantCulture, "{0}s", seconds);
            }

            set
            {
                if (value == null)
                {
                    this.TimeToLive = null;
                    return;
                }

                var segments = value.TrimEnd('s').Split('.');
                var seconds = long.Parse(segments[0], CultureInfo.InvariantCulture);
                var ttl = TimeSpan.FromSeconds(seconds);
                if (segments.Length == 2)
                {
                    var fractionStr = segments[1].PadRight(9, '0').Substring(0, 9);
                    var nanoseconds = long.Parse(fractionStr, CultureInfo.InvariantCulture);
                    ttl = ttl.Add(TimeSpan.FromTicks(nanoseconds / 100));
                }

                this.TimeToLive = ttl;
            }
        }

        /// <summary>
        /// Copies this Android V2 config, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal AndroidConfigV2 CopyAndValidate()
        {
            var copy = new AndroidConfigV2()
            {
                CollapseKey = this.CollapseKey,
                TimeToLive = this.TimeToLive,
                RestrictedPackageName = this.RestrictedPackageName,
                Data = this.Data?.Copy(),
                DirectBootOk = this.DirectBootOk,
                BandwidthConstrainedOk = this.BandwidthConstrainedOk,
                RestrictedSatelliteOk = this.RestrictedSatelliteOk,
                RemoteNotification = this.RemoteNotification?.CopyAndValidate(),
                BackgroundSync = this.BackgroundSync?.CopyAndValidate(),
                FcmOptions = this.FcmOptions?.CopyAndValidate(),
            };

            var totalSeconds = copy.TimeToLive?.TotalSeconds ?? 0;
            if (totalSeconds < 0)
            {
                throw new ArgumentException("TTL must not be negative.");
            }

            var hasRemote = copy.RemoteNotification != null;
            var hasSync = copy.BackgroundSync != null;
            if (hasRemote == hasSync)
            {
                throw new ArgumentException("Exactly one of RemoteNotification or BackgroundSync must be specified on AndroidConfigV2.");
            }

            return copy;
        }
    }
}
