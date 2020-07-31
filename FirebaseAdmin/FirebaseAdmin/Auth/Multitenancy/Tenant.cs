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

using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// Represents a tenant in a multi-tenant application..
    ///
    /// <para>Multitenancy support requires Google Cloud Identity Platform (GCIP). To learn more
    /// about GCIP, including pricing and features, see the
    /// <a href="https://cloud.google.com/identity-platform">GCIP documentation</a>.</para>
    ///
    /// <para>Before multitenancy can be used in a Google Cloud Identity Platform project, tenants
    /// must be allowed on that project via the Cloud Console UI.</para>
    ///
    /// <para>A tenant configuration provides information such as the display name, tenant
    /// identifier and email authentication configuration. For OIDC/SAML provider configuration
    /// management, TenantAwareFirebaseAuth instances should be used instead of a Tenant to
    /// retrieve the list of configured IdPs on a tenant. When configuring these providers, note
    /// that tenants will inherit whitelisted domains and authenticated redirect URIs of their
    /// parent project.</para>
    ///
    /// <para>All other settings of a tenant will also be inherited. These will need to be managed
    /// from the Cloud Console UI.</para>
    /// </summary>
    public sealed class Tenant
    {
        private readonly TenantArgs args;

        internal Tenant(TenantArgs args)
        {
            this.args = args.ThrowIfNull(nameof(args));
        }

        /// <summary>
        /// Gets the tenant identifier.
        /// </summary>
        public string TenantId => this.ExtractResourceId(this.args.Name);

        /// <summary>
        /// Gets the tenant display name.
        /// </summary>
        public string DisplayName => args.DisplayName;

        /// <summary>
        /// Gets a value indicating whether the email sign-in provider is enabled.
        /// </summary>
        public bool PasswordSignUpAllowed => args.PasswordSignUpAllowed ?? false;

        /// <summary>
        /// Gets a value indicating whether the email link sign-in is enabled.
        /// </summary>
        public bool EmailLinkSignInEnabled => args.EmailLinkSignInEnabled ?? false;

        private string ExtractResourceId(string resourceName)
        {
            var segments = resourceName.Split('/');
            return segments[segments.Length - 1];
        }
    }
}
