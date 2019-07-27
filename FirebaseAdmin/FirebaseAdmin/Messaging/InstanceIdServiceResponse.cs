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

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Response from an operation that subscribes or unsubscribes registration tokens to a topic.
    /// See <see cref="FirebaseMessaging.SubscribeToTopicAsync(string, List{string})"/> and <see cref="FirebaseMessaging.UnsubscribeFromTopicAsync(string, List{string})"/>.
    /// </summary>
    public sealed class InstanceIdServiceResponse
    {
        /// <summary>
        /// Gets the errors returned by the operation.
        /// </summary>
        [JsonProperty("results")]
        public List<InstanceIdServiceResponseElement> Results { get; private set; }

        /// <summary>
        /// Gets the number of errors returned by the operation.
        /// </summary>
        public int ErrorCount => Results?.Count(results => results.HasError) ?? 0;

        /// <summary>
        /// Gets the number of results returned by the operation.
        /// </summary>
        public int ResultCount => Results?.Count() ?? 0;

        /// <summary>
        /// An instance Id response error.
        /// </summary>
        public class InstanceIdServiceResponseElement
        {
            /// <summary>
            /// Gets a value indicating the error in this element of the response array. If this is empty this indicates success.
            /// </summary>
            [JsonProperty("error")]
            public string Error { get; private set; }

            /// <summary>
            /// Gets a value indicating whether this response element in the response array is an error, as an empty element indicates success.
            /// </summary>
            public bool HasError => !string.IsNullOrEmpty(Error);
        }
    }
}
