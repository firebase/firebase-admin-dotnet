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
        /// <description>To retrieve the public signing keys from <c>jwks_uri</c>, which is used
        /// to verify the signature of the ID token.</description>
        /// </item>
        /// <item>
        /// <description>To determine the <c>claims_supported</c>, which are passed through in
        /// the additional user info response.</description>
        /// </item>
        /// </list>
        /// <para>
        /// ID token validation is performed as defined in
        /// <a href="https://openid.net/specs/openid-connect-core-1_0.html#IDTokenValidation">
        /// OpenID Connect</a>.
        /// </para>
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the Client Secret used to verify code based response types.
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this OIDC provider uses an ID-token based response type.
        /// </summary>
        public bool? IDTokenResponseType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this OIDC provider uses a code based response type.
        /// </summary>
        public bool? CodeResponseType { get; set; }

        internal override AuthProviderConfig.Request ToCreateRequest()
        {
            var req = new OidcProviderConfig.Request()
            {
                DisplayName = this.DisplayName,
                Enabled = this.Enabled,
                ClientId = this.ClientId,
                Issuer = this.Issuer,
                ClientSecret = this.ClientSecret,
            };
            if (this.CodeResponseType != null || this.IDTokenResponseType != null)
            {
                req.ResponseType = new OidcProviderConfig.ResponseTypeInfo()
                {
                    Code = this.CodeResponseType,
                    IDToken = this.IDTokenResponseType,
                };
            }

            if (string.IsNullOrEmpty(req.ClientId))
            {
                throw new ArgumentException("Client ID must not be null or empty.");
            }

            if (string.IsNullOrEmpty(req.Issuer))
            {
                throw new ArgumentException("Issuer must not be null or empty.");
            }
            else if (!IsWellFormedUriString(req.Issuer))
            {
                throw new ArgumentException($"Malformed issuer string: {req.Issuer}");
            }

            if (req.ResponseType?.Code == true && string.IsNullOrEmpty(req.ClientSecret))
            {
                throw new ArgumentException("Client Secret must not be null or empty for code response type");
            }

            return req;
        }

        internal override AuthProviderConfig.Request ToUpdateRequest()
        {
            var req = new OidcProviderConfig.Request()
            {
                DisplayName = this.DisplayName,
                Enabled = this.Enabled,
                ClientId = this.ClientId,
                Issuer = this.Issuer,
                ClientSecret = this.ClientSecret,
            };
            if (this.CodeResponseType != null || this.IDTokenResponseType != null)
            {
                req.ResponseType = new OidcProviderConfig.ResponseTypeInfo()
                {
                    Code = this.CodeResponseType,
                    IDToken = this.IDTokenResponseType,
                };
            }

            if (req.ClientId == string.Empty)
            {
                throw new ArgumentException("Client ID must not be empty.");
            }

            if (req.Issuer == string.Empty)
            {
                throw new ArgumentException("Issuer must not be empty.");
            }
            else if (req.Issuer != null && !IsWellFormedUriString(req.Issuer))
            {
                throw new ArgumentException($"Malformed issuer string: {req.Issuer}");
            }

            if (req.ResponseType?.Code == true && string.IsNullOrEmpty(req.ClientSecret))
            {
                throw new ArgumentException("Client Secret must not be null or empty for code response type");
            }

            if (req.ResponseType?.Code == false && req.ResponseType?.IDToken == false)
            {
                throw new ArgumentException("At least one response type must be returned.");
            }

            return req;
        }

        internal override ProviderConfigClient<OidcProviderConfig> GetClient()
        {
            return OidcProviderConfigClient.Instance;
        }
    }
}
