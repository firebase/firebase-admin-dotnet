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

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// Contains a collection of tenants.
    /// </summary>
    public sealed class TenantsPage
    {
        /// <summary>
        /// Gets the token representing the next page of tenants. Null if there are no more pages.
        /// </summary>
        public string NextPageToken { get; internal set; }

        /// <summary>
        /// Gets the tenants included in the current page.
        /// </summary>
        public IEnumerable<Tenant> Tenants { get; internal set; }
    }
}
