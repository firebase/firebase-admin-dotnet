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

using System;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// The base auth provider configuration interface.
    /// <para>
    /// Auth provider configuration support requires Google Cloud's Identity Platform (GCIP). To
    /// learn more about GCIP, including pricing and features, see the
    /// <a href="https://cloud.google.com/identity-platform">GCIP documentation</a>.
    /// </para>
    /// </summary>
    /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> that can be created or
    /// updated using this argument type.</typeparam>
    public abstract class AuthProviderConfigArgs<T>
    where T : AuthProviderConfig
    {
        /// <summary>
        /// Gets or sets the provider ID defined by the developer. For an OIDC provider, this is
        /// always prefixed by <c>oidc.</c>. For a SAML provider, this is always prefixed by
        /// <c>saml.</c>.
        /// </summary>
        public string ProviderId { get; set; }

        /// <summary>
        /// Gets or sets the user-friendly display name of the configuration. This name is
        /// also used as the provider label in the Cloud Console.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the provider configuration is enabled or
        /// disabled. A user cannot sign in using a disabled provider.
        /// </summary>
        public bool? Enabled { get; set; }

        internal static bool IsWellFormedUriString(string uri)
        {
            return Uri.IsWellFormedUriString(uri, UriKind.Absolute);
        }

        internal abstract AuthProviderConfig.Request ToCreateRequest();

        internal abstract AuthProviderConfig.Request ToUpdateRequest();

        internal abstract ProviderConfigClient<T> GetClient();
    }
}
