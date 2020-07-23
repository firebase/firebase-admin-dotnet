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

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// The base Auth provider configuration interface.
    /// <para>
    /// Auth provider configuration support requires Google Cloud's Identity Platform (GCIP). To
    /// learn more about GCIP, including pricing and features, see the
    /// <a href="https://cloud.google.com/identity-platform">GCIP documentation</a>.
    /// </para>
    /// </summary>
    public abstract class AuthProviderConfig
    {
        internal AuthProviderConfig(Request request)
        {
            var segments = request.Name.Split('/');
            this.ProviderId = segments[segments.Length - 1];
            this.DisplayName = request.DisplayName;
            this.Enabled = request.Enabled ?? false;
        }

        /// <summary>
        /// Gets the provider ID defined by the developer. For an OIDC provider, this is always
        /// prefixed by <c>oidc.</c>. For a SAML provider, this is always prefixed by <c>saml.</c>.
        /// </summary>
        public string ProviderId { get; }

        /// <summary>
        /// Gets the user-friendly display name of the configuration. This name is also used
        /// as the provider label in the Cloud Console.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets a value indicating whether the provider configuration is enabled or disabled. A
        /// user cannot sign in using a disabled provider.
        /// </summary>
        public bool Enabled { get; }

        internal abstract class Request
        {
            [JsonProperty("name")]
            internal string Name { get; set; }

            [JsonProperty("displayName")]
            internal string DisplayName { get; set; }

            [JsonProperty("enabled")]
            internal bool? Enabled { get; set; }
        }
    }
}
