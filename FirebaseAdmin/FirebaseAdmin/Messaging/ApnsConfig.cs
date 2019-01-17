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
        /// <summary>
        /// A collection of APNs headers.
        /// </summary>
        [JsonProperty("headers")]
        public IReadOnlyDictionary<string, string> Headers;

        /// <summary>
        /// The <code>aps</code> dictionary to be included in the APNs payload.
        /// </summary>
        [JsonIgnore]
        public Aps Aps;

        /// <summary>
        /// APNs payload as accepted by the FCM backend servers.
        /// </summary>
        [JsonProperty("payload")]
        private IReadOnlyDictionary<string, object> Payload
        {
            get
            {
                var aps = this.Aps;
                if (aps == null)
                {
                    throw new ArgumentException("Aps must not be null in ApnsConfig.");
                }
                var payload = new Dictionary<string, object>()
                {
                    { "aps", aps },
                };
                var customData = this.CustomData;
                if (customData != null)
                {
                    if (customData.ContainsKey("aps"))
                    {
                        throw new ArgumentException(
                            "Multiple specifications for Apns payload key: aps");
                    }
                    payload = payload.Concat(customData).ToDictionary(x=>x.Key, x=>x.Value);
                }
                return payload;
            }
            set
            {
                var copy = value.ToDictionary(x=>x.Key, x=>x.Value);
                object aps;
                if (copy.TryGetValue("aps", out aps))
                {
                    copy.Remove("aps");
                    if (aps.GetType() == typeof(Aps))
                    {
                        this.Aps = (Aps) aps;
                    }
                    else
                    {
                        var json = NewtonsoftJsonSerializer.Instance.Serialize(aps);
                        this.Aps = NewtonsoftJsonSerializer.Instance.Deserialize<Aps>(json);
                    }
                }
                this.CustomData = copy;
            }
        }

        /// <summary>
        /// A collection of arbitrary key-value data to be included in the APNs payload.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, object> CustomData { get; set; }

        /// <summary>
        /// Copies this APNs config, and validates the content of it to ensure that it can be
        /// serialized into the JSON format expected by the FCM service.
        /// </summary>

        internal ApnsConfig CopyAndValidate()
        {
            var copy = new ApnsConfig()
            {
                Headers = this.Headers.Copy(),
                Payload = this.Payload,
            };
            copy.Aps = copy.Aps?.CopyAndValidate();
            return copy;
        }
    }
}