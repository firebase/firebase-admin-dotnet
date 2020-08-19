// Copyright 2020, Google Inc. All rights reserved.
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
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth.Users
{
    /// <summary>
    /// JSON data binding for `BatchDeleteResponse` messages sent by Google Identity Toolkit
    /// service.
    /// </summary>
    internal sealed class BatchDeleteResponse
    {
        /// <summary>
        /// Gets or sets a list of errors.
        /// </summary>
        [JsonProperty(PropertyName = "errors")]
        public List<ErrorInfo> Errors { get; set; }

        internal sealed class ErrorInfo
        {
            [JsonProperty(PropertyName = "index")]
            public int Index { get; set; }

            [JsonProperty(PropertyName = "message")]
            public string Message { get; set; }

            // A 'localId' field also exists here, but is not currently exposed in the Admin SDK.
        }
    }
}
