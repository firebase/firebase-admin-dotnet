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

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// Options for listing tenants.
    /// </summary>
    public sealed class ListTenantsOptions
    {
        /// <summary>
        /// Gets or sets the number of results to fetch in a single API call. This does not affect
        /// the total number of results returned. Must not exceed 100.
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Gets or sets the page token. If set, this token is used to indicate a continued list
        /// operation.
        /// </summary>
        public string PageToken { get; set; }
    }
}
