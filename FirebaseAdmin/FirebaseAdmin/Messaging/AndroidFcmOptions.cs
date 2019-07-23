// Copyright 2019, Google Inc. All rights reserved.
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

using FirebaseAdmin.Messaging.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents Android FCM options.
    /// </summary>
    public sealed class AndroidFcmOptions
    {
        /// <summary>
        /// Gets or sets analytics label.
        /// </summary>
        [JsonProperty("analytics_label")]
        public string AnalyticsLabel { get; set; }

        /// <summary>
        /// Copies this FCM options, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal AndroidFcmOptions CopyAndValidate()
        {
            var copy = new AndroidFcmOptions()
            {
                AnalyticsLabel = this.AnalyticsLabel,
            };
            AnalyticsLabelChecker.ValidateAnalyticsLabel(copy.AnalyticsLabel);

            return copy;
        }
    }
}
