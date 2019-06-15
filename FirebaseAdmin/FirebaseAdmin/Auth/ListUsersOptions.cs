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

using Google.Api.Gax;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Options for <see cref="FirebaseAuth.ListUsersAsync(ListUsersOptions)"/> API.
    /// </summary>
    public sealed class ListUsersOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListUsersOptions"/> class.
        /// </summary>
        public ListUsersOptions() { }

        internal ListUsersOptions(ListUsersOptions source)
        {
            this.PageSize = source?.PageSize;
            this.PageToken = source?.PageToken;
        }

        /// <summary>
        /// Gets or sets the number of results to return per page. This modifies the per-request page size.
        /// It does not affect the total number of results returned. Must not exceed 1000.
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the page token. If set, this token is used to indicate a continued list operation.
        /// </summary>
        public string PageToken { get; set; }
    }
}
