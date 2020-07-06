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

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// A page of auth provider configurations.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> that is included.</typeparam>
    public sealed class AuthProviderConfigs<T>
    where T : AuthProviderConfig
    {
        /// <summary>
        /// Gets the token representing the next page of auth provider configurations. Null if
        /// there are no more pages.
        /// </summary>
        public string NextPageToken { get; internal set; }

        /// <summary>
        /// Gets the auth provider configurations included in the current page.
        /// </summary>
        public IEnumerable<T> ProviderConfigs { get; internal set; }
    }
}
