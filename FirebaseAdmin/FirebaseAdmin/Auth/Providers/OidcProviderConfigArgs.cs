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
using Google.Apis.Json;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// Represents an OIDC auth provider configuration. See
    /// <a href="https://openid.net/specs/openid-connect-core-1_0-final.html">OpenID Connect</a>.
    /// </summary>
    public sealed class OidcProviderConfigArgs : AuthProviderConfigArgs<OidcProviderConfig>
    {
        /// <summary>
        /// Gets or sets the client ID used to confirm the audience of an OIDC provider's ID token.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the issuer used to match the issuer of the ID token and to determine the
        /// corresponding OIDC discovery document, eg. <c>/.well-known/openid-configuration</c>.
        /// This is needed for the following:
        /// <list type="bullet">
        /// <item>
        /// <description>To verify the provided issuer.</description>
        /// </item>
        /// <item>
        /// <description>To determine the authentication/authorization endpoint during the OAuth
        /// <c>id_token</c> authentication flow.</description>
        /// </item>
        /// <item>
        /// <description>To retrieve the public signing keys via <c>jwks_uri</c> to verify the
        /// OIDC provider's ID token signature.</description>
        /// </item>
        /// <item>
        /// <description>To determine the <c>claims_supported</c> to construct the user attributes
        /// to be returned in the additional user info response.</description>
        /// </item>
        /// </list>
        /// <para>
        /// ID token validation is performed as defined in
        /// <a href="https://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation">
        /// OpenID Connect</a>.
        /// </para>
        /// </summary>
        public string Issuer { get; set; }

        internal override string ValidateProviderId()
        {
            var providerId = this.ProviderId ?? string.Empty;
            if (!providerId.StartsWith("oidc."))
            {
                throw new ArgumentException("OIDC provider ID must have the prefix 'oidc.'.");
            }

            return providerId;
        }

        internal override AuthProviderConfig.Request ToCreateRequest()
        {
            var req = new OidcProviderConfig.Request()
            {
                DisplayName = this.DisplayName,
                Enabled = this.Enabled,
                ClientId = this.ClientId,
                Issuer = this.Issuer,
            };
            if (string.IsNullOrEmpty(req.ClientId))
            {
                throw new ArgumentException("Client ID must not be null or empty.");
            }

            if (string.IsNullOrEmpty(req.Issuer))
            {
                throw new ArgumentException("Issuer must not be null or empty.");
            }
            else if (!Uri.IsWellFormedUriString(req.Issuer, UriKind.Absolute))
            {
                throw new ArgumentException($"Malformed issuer string: {req.Issuer}");
            }

            return req;
        }

        internal override OidcProviderConfig CreateAuthProviderConfig(string json)
        {
            var model = NewtonsoftJsonSerializer.Instance
                .Deserialize<OidcProviderConfig.Request>(json);
            return new OidcProviderConfig(model);
        }
    }
}
