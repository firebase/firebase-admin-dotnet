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
    /// Represents the Webpush protocol options that can be included in a <see cref="Message"/>.
    /// </summary>
    public sealed class WebpushConfig
    {
        /// <summary>
        /// Webpush HTTP headers. Refer <see href="https://tools.ietf.org/html/rfc8030#section-5">
        /// Webpush specification</see> for supported headers.
        /// </summary>
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Webpush data fields. When set, overrides any data fields set via
        /// <see cref="Message.Data"/>.
        /// </summary>
        public IReadOnlyDictionary<string, string> Data { get; set; }
        
        /// <summary>
        /// The Webpush notification to be included in the message.
        /// </summary>
        public WebpushNotification Notification { get; set; }

        /// <summary>
        /// Validates the content and structure of this Webpush configuration, and converts it into
        /// the <see cref="ValidatedWebpushConfig"/> type. This return type can be safely
        /// serialized into a JSON string that is acceptable to the FCM backend service.
        /// </summary>
        internal ValidatedWebpushConfig Validate()
        {
            return new ValidatedWebpushConfig()
            {
                Headers = this.Headers,
                Data = this.Data,
                Notification = this.Notification?.Validate(),
            };
        }
    }

    /// <summary>
    /// Represents a validated Webpush configuration that can be serialized into the JSON format
    /// accepted by the FCM backend service.
    /// </summary>
    internal sealed class ValidatedWebpushConfig
    {
        [JsonProperty("headers")]
        public IReadOnlyDictionary<string, string> Headers { get; set; }

        [JsonProperty("data")]
        public IReadOnlyDictionary<string, string> Data { get; set; }

        [JsonProperty("notification")]
        internal IReadOnlyDictionary<string, object> Notification { get; set; }   
    }
}
