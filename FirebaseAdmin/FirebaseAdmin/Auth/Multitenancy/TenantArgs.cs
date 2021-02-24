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

using Newtonsoft.Json;

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// Arguments for creating and updating tenants.
    /// </summary>
    public sealed class TenantArgs
    {
        /// <summary>
        /// Gets or sets the tenant display name.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email sign-in provider is enabled.
        /// </summary>
        [JsonProperty("allowPasswordSignup")]
        public bool? PasswordSignUpAllowed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the email link sign-in is enabled.
        /// </summary>
        [JsonProperty("enableEmailLinkSignin")]
        public bool? EmailLinkSignInEnabled { get; set; }

        [JsonProperty("name")]
        internal string Name { get; set; }
    }
}
