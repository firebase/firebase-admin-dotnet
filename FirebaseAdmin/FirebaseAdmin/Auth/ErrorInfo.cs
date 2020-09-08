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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents an error encountered while performing a batch operation such as
    /// <see cref="AbstractFirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs})"/> or
    /// <see cref="AbstractFirebaseAuth.DeleteUsersAsync(IReadOnlyList{string})"/>.
    /// </summary>
    public sealed class ErrorInfo
    {
        internal ErrorInfo(int index, string reason)
        {
            this.Index = index;
            this.Reason = reason;
        }

        internal ErrorInfo() { }

        /// <summary>
        /// Gets the index of the entry that caused the error.
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; internal set; }

        /// <summary>
        /// Gets a string describing the error.
        /// </summary>
        [JsonProperty("message")]
        public string Reason { get; internal set; }
    }
}
