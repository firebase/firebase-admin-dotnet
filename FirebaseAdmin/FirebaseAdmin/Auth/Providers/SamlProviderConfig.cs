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
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// Represents a SAML auth provider configuration. See
    /// <a href="http://docs.oasis-open.org/security/saml/Post2.0/sstc-saml-tech-overview-2.0.html">
    /// SAML technical overview</a>.
    /// </summary>
    public sealed class SamlProviderConfig : AuthProviderConfig
    {
        internal SamlProviderConfig(Request request)
        : base(request)
        {
            this.IdpEntityId = request.IdpConfig.IdpEntityId;
            this.SsoUrl = request.IdpConfig.SsoUrl;
            this.X509Certificates = request.IdpConfig.IdpCertificates
                .Select((c) => c.X509Certificate);
            this.RpEntityId = request.SpConfig.SpEntityId;
            this.CallbackUrl = request.SpConfig.CallbackUri;
        }

        /// <summary>
        /// Gets the SAML IdP entity identifier.
        /// </summary>
        public string IdpEntityId { get; }

        /// <summary>
        /// Gets the SAML IdP SSO URL.
        /// </summary>
        public string SsoUrl { get; }

        /// <summary>
        /// Gets the collection of SAML IdP X.509 certificates issued by CA for this provider.
        /// Multiple certificates are accepted to prevent outages during IdP key rotation (for
        /// example ADFS rotates every 10 days). When the Auth server receives a SAML response,
        /// it will match the SAML response with the certificate on record. Otherwise the response
        /// is rejected. Developers are expected to manage the certificate updates as keys are
        /// rotated.
        /// </summary>
        public IEnumerable<string> X509Certificates { get; }

        /// <summary>
        /// Gets the SAML relying party (service provider) entity ID. This is defined by the
        /// developer but needs to be provided to the SAML IdP.
        /// </summary>
        public string RpEntityId { get; }

        /// <summary>
        /// Gets the SAML callback URL. This is fixed and must always be the same as the OAuth
        /// redirect URL provisioned by Firebase Auth,
        /// <c>https://project-id.firebaseapp.com/__/auth/handler</c> unless a custom
        /// <c>authDomain</c> is used. The callback URL should also be provided to the SAML IdP
        /// during configuration.
        /// </summary>
        public string CallbackUrl { get; }

        internal sealed new class Request : AuthProviderConfig.Request
        {
            [JsonProperty("idpConfig")]
            internal IdpConfig IdpConfig { get; set; }

            [JsonProperty("spConfig")]
            internal SpConfig SpConfig { get; set; }
        }

        internal sealed class IdpConfig
        {
            [JsonProperty("idpEntityId")]
            internal string IdpEntityId { get; set; }

            [JsonProperty("ssoUrl")]
            internal string SsoUrl { get; set; }

            [JsonProperty("idpCertificates")]
            internal IEnumerable<IdpCertificate> IdpCertificates { get; set; }

            internal bool HasValues
            {
                get => this.IdpEntityId != null || this.SsoUrl != null ||
                    this.IdpCertificates != null;
            }
        }

        internal sealed class IdpCertificate
        {
            [JsonProperty("x509Certificate")]
            internal string X509Certificate { get; set; }
        }

        internal sealed class SpConfig
        {
            [JsonProperty("spEntityId")]
            internal string SpEntityId { get; set; }

            [JsonProperty("callbackUri")]
            internal string CallbackUri { get; set; }

            internal bool HasValues
            {
                get => this.SpEntityId != null || this.CallbackUri != null;
            }
        }
    }
}
