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
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the APNS-specific options that can be included in a <see cref="Message"/>. Refer
    /// to <a href="https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/APNSOverview.html">
    /// APNs documentation</a> for various headers and payload fields supported by APNS.
    /// </summary>
    public sealed class ApnsConfig
    {
        private ApnsPayload payload = new ApnsPayload();

        /// <summary>
        /// Gets or sets the APNs headers.
        /// </summary>
        [JsonProperty("headers")]
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Gets or sets the FCM options to be included in the message.
        /// </summary>
        [JsonProperty("fcm_options")]
        public ApnsFcmOptions FcmOptions { get; set; }

        /// <summary>
        /// Gets or sets the <c>aps</c> dictionary to be included in the APNs payload.
        /// </summary>
        [JsonIgnore]
        public Aps Aps
        {
            get
            {
                return this.Payload.Aps;
            }

            set
            {
                this.Payload.Aps = value;
            }
        }

        /// <summary>
        /// Gets or sets a collection of arbitrary key-value data that will be included in the APNs
        /// payload.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, object> CustomData
        {
            get
            {
                return this.Payload.CustomData;
            }

            set
            {
                this.Payload.CustomData = value;
            }
        }

        /// <summary>
        /// Gets or sets the APNs payload as accepted by the FCM backend servers.
        /// </summary>
        [JsonProperty("payload")]
        private ApnsPayload Payload
        {
            get
            {
                if (this.payload.Aps != null && this.payload.CustomData?.ContainsKey("aps") == true)
                {
                    throw new ArgumentException("Multiple specifications for ApnsConfig key: aps");
                }

                return this.payload;
            }

            set
            {
                this.payload = value;
            }
        }

        /// <summary>
        /// Copies this APNs config, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal ApnsConfig CopyAndValidate()
        {
            var copy = new ApnsConfig()
            {
                Headers = this.Headers?.Copy(),
                Payload = this.Payload.CopyAndValidate(),
                FcmOptions = this.FcmOptions?.CopyAndValidate(),
            };
            return copy;
        }

        /// <summary>
        /// The APNs payload object as expected by the FCM backend service.
        /// </summary>
        private class ApnsPayload
        {
            [JsonProperty("aps")]
            internal Aps Aps { get; set; }

            [JsonExtensionData]
            internal IDictionary<string, object> CustomData { get; set; }

            /// <summary>
            /// Copies this APNs payload, and validates the content of it to ensure that it can be
            /// serialized into the JSON format expected by the FCM service.
            /// </summary>
            internal ApnsPayload CopyAndValidate()
            {
                var copy = new ApnsPayload()
                {
                    CustomData = this.CustomData?.ToDictionary(e => e.Key, e => e.Value),
                };
                var aps = this.Aps;
                if (aps == null && copy.CustomData?.ContainsKey("aps") == false)
                {
                    throw new ArgumentException("Aps dictionary is required in ApnsConfig");
                }

                copy.Aps = aps?.CopyAndValidate();
                return copy;
            }
        }
    }
}
