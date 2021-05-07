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
    /// Represents an OIDC auth provider configuration. See
    /// <a href="https://openid.net/specs/openid-connect-core-1_0-final.html">OpenID Connect</a>.
    /// </summary>
    public sealed class OidcProviderConfig : AuthProviderConfig
    {
        internal OidcProviderConfig(Request request)
        : base(request)
        {
            this.ClientId = request.ClientId;
            this.Issuer = request.Issuer;
            this.ClientSecret = request.ClientSecret;
            this.ResponseType = new ResponseTypeArgs(request.ResponseType);
        }

        /// <summary>
        /// Gets the client ID used to confirm the audience of an OIDC provider's ID token.
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// Gets the issuer used to match the issuer of the ID token and to determine the
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
        public string Issuer { get; }

        /// <summary>
        /// Gets the Client Secret used to verify code based response types.
        /// </summary>
        public string ClientSecret { get; }

        /// <summary>
        /// Gets what response types are being used for this OIDC config.
        /// </summary>
        public ResponseTypeArgs ResponseType { get; }

        /// <summary>
        /// Represents the response types beinf used for this OIDC config.
        /// </summary>
        public class ResponseTypeArgs
        {
            internal ResponseTypeArgs(ResponseTypeJson request)
            {
                this.Code = request.Code;
                this.IDToken = request.IDToken;
            }

            /// <summary>
            /// Gets a value indicating whether this OIDC provider uses a code based response type.
            /// </summary>
            public bool? Code { get; }

            /// <summary>
            /// Gets a value indicating whether this OIDC provider uses an ID-token based response type.
            /// </summary>
            public bool? IDToken { get; }
        }

        internal sealed class ResponseTypeJson
        {
            [JsonProperty("code")]
            internal bool? Code { get; set; }

            [JsonProperty("idToken")]
            internal bool? IDToken { get; set; }
        }

        internal sealed new class Request : AuthProviderConfig.Request
        {
            [JsonProperty("clientId")]
            internal string ClientId { get; set; }

            [JsonProperty("issuer")]
            internal string Issuer { get; set; }

            [JsonProperty("clientSecret")]
            internal string ClientSecret { get; set; }

            [JsonProperty("responseType")]
            internal ResponseTypeJson ResponseType { get; set; }
        }
    }
}
