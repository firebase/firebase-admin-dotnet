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
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the APNS-specific options that can be included in a <see cref="Message"/>. Refer
    /// to <see href="https://developer.apple.com/library/content/documentation/NetworkingInternet/Conceptual/RemoteNotificationsPG/APNSOverview.html">
    /// APNs documentation</see> for various headers and payload fields supported by APNS.
    /// </summary>
    public sealed class ApnsConfig
    {
        private ApnsPayload _payload = new ApnsPayload();

        /// <summary>
        /// A collection of APNs headers.
        /// </summary>
        [JsonProperty("headers")]
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// The <code>aps</code> dictionary to be included in the APNs payload.
        /// </summary>
        [JsonIgnore]
        public Aps Aps
        {
            get
            {
                return Payload.Aps;
            }
            set
            {
                Payload.Aps = value;
            }
        }

        /// <summary>
        /// APNs payload as accepted by the FCM backend servers.
        /// </summary>
        [JsonProperty("payload")]
        private ApnsPayload Payload
        {
            get
            {
                if (_payload.Aps != null && _payload.CustomData?.ContainsKey("aps") == true)
                {
                    throw new ArgumentException("Multiple specifications for ApnsConfig key: aps");
                }
                return _payload;
            }
            set
            {
                _payload = value;
            }
        }

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the APNs payload.
        /// </summary>
        [JsonIgnore]
        public IDictionary<string, object> CustomData
        {
            get
            {
                return Payload.CustomData;
            }
            set
            {
                Payload.CustomData = value;
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
            };
            return copy;
        }
    }

    internal sealed class ApnsPayload
    {
        [JsonProperty("aps")]
        public Aps Aps { get; set; }

        [JsonExtensionData]
        public IDictionary<string, object> CustomData { get; set; }

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