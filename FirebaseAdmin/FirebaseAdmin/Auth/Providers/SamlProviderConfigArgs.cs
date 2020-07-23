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
using System.Collections.Generic;
using System.Linq;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// Represents a SAML auth provider configuration. See
    /// <a href="http://docs.oasis-open.org/security/saml/Post2.0/sstc-saml-tech-overview-2.0.html">
    /// SAML technical overview</a>.
    /// </summary>
    public sealed class SamlProviderConfigArgs : AuthProviderConfigArgs<SamlProviderConfig>
    {
        /// <summary>
        /// Gets or sets the SAML IdP entity identifier.
        /// </summary>
        public string IdpEntityId { get; set; }

        /// <summary>
        /// Gets or sets the SAML IdP SSO URL.
        /// </summary>
        public string SsoUrl { get; set; }

        /// <summary>
        /// Gets or sets the collection of SAML IdP X.509 certificates issued by CA for this
        /// provider. Multiple certificates are accepted to prevent outages during IdP key
        /// rotation (for example ADFS rotates every 10 days). When the Auth server receives a SAML
        /// response, it will match the SAML response with the certificate on record. Otherwise the
        /// response is rejected. Developers are expected to manage the certificate updates as keys
        /// are rotated.
        /// </summary>
        public IEnumerable<string> X509Certificates { get; set; }

        /// <summary>
        /// Gets or sets the SAML relying party (service provider) entity ID. This is defined by
        /// the developer but needs to be provided to the SAML IdP.
        /// </summary>
        public string RpEntityId { get; set; }

        /// <summary>
        /// Gets or sets the SAML callback URL. This is fixed and must always be the same as the
        /// OAuth redirect URL provisioned by Firebase Auth,
        /// <c>https://project-id.firebaseapp.com/__/auth/handler</c> unless a custom
        /// <c>authDomain</c> is used. The callback URL should also be provided to the SAML IdP
        /// during configuration.
        /// </summary>
        public string CallbackUrl { get; set; }

        internal override AuthProviderConfig.Request ToCreateRequest()
        {
            var req = this.ToRequest();
            if (string.IsNullOrEmpty(req.IdpConfig.IdpEntityId))
            {
                throw new ArgumentException("IDP entity ID must not be null or empty.");
            }

            if (string.IsNullOrEmpty(req.IdpConfig.SsoUrl))
            {
                throw new ArgumentException("SSO URL must not be null or empty.");
            }
            else if (!IsWellFormedUriString(req.IdpConfig.SsoUrl))
            {
                throw new ArgumentException($"Malformed SSO URL: {req.IdpConfig.SsoUrl}");
            }

            var certs = req.IdpConfig.IdpCertificates;
            if (certs == null || certs.Count() == 0)
            {
                throw new ArgumentException("X509 certificates must not be null or empty.");
            }
            else if (certs.Any((cert) => string.IsNullOrEmpty(cert.X509Certificate)))
            {
                throw new ArgumentException(
                    "X509 certificates must not contain null or empty values.");
            }

            if (string.IsNullOrEmpty(req.SpConfig.SpEntityId))
            {
                throw new ArgumentException("RP entity ID must not be null or empty.");
            }

            if (string.IsNullOrEmpty(req.SpConfig.CallbackUri))
            {
                throw new ArgumentException("Callback URL must not be null or empty.");
            }
            else if (!IsWellFormedUriString(req.SpConfig.CallbackUri))
            {
                throw new ArgumentException($"Malformed callback URL: {req.SpConfig.CallbackUri}");
            }

            return req;
        }

        internal override AuthProviderConfig.Request ToUpdateRequest()
        {
            var req = this.ToRequest();
            if (req.IdpConfig.HasValues)
            {
                this.ValidateIdpConfigForUpdate(req.IdpConfig);
            }
            else
            {
                req.IdpConfig = null;
            }

            if (req.SpConfig.HasValues)
            {
                this.ValidateSpConfigForUpdate(req.SpConfig);
            }
            else
            {
                req.SpConfig = null;
            }

            return req;
        }

        internal override ProviderConfigClient<SamlProviderConfig> GetClient()
        {
            return SamlProviderConfigClient.Instance;
        }

        private SamlProviderConfig.Request ToRequest()
        {
            return new SamlProviderConfig.Request()
            {
                DisplayName = this.DisplayName,
                Enabled = this.Enabled,
                IdpConfig = new SamlProviderConfig.IdpConfig()
                {
                    IdpEntityId = this.IdpEntityId,
                    SsoUrl = this.SsoUrl,
                    IdpCertificates = this.X509Certificates?
                        .Select((cert) => new SamlProviderConfig.IdpCertificate()
                        {
                            X509Certificate = cert,
                        }),
                },
                SpConfig = new SamlProviderConfig.SpConfig()
                {
                    SpEntityId = this.RpEntityId,
                    CallbackUri = this.CallbackUrl,
                },
            };
        }

        private void ValidateIdpConfigForUpdate(SamlProviderConfig.IdpConfig idpConfig)
        {
            if (idpConfig.IdpEntityId == string.Empty)
            {
                throw new ArgumentException("IDP entity ID must not be empty.");
            }

            var ssoUrl = idpConfig.SsoUrl;
            if (ssoUrl == string.Empty)
            {
                throw new ArgumentException("SSO URL must not be empty.");
            }
            else if (ssoUrl != null && !IsWellFormedUriString(ssoUrl))
            {
                throw new ArgumentException($"Malformed SSO URL: {ssoUrl}");
            }

            var certs = idpConfig.IdpCertificates;
            if (certs?.Count() == 0)
            {
                throw new ArgumentException("X509 certificates must not be empty.");
            }
            else if (certs?.Any((cert) => string.IsNullOrEmpty(cert.X509Certificate)) ?? false)
            {
                throw new ArgumentException(
                    "X509 certificates must not contain null or empty values.");
            }
        }

        private void ValidateSpConfigForUpdate(SamlProviderConfig.SpConfig spConfig)
        {
            if (spConfig.SpEntityId == string.Empty)
            {
                throw new ArgumentException("RP entity ID must not be empty.");
            }

            var callbackUri = spConfig.CallbackUri;
            if (callbackUri == string.Empty)
            {
                throw new ArgumentException("Callback URL must not be empty.");
            }
            else if (callbackUri != null && !IsWellFormedUriString(callbackUri))
            {
                throw new ArgumentException($"Malformed callback URL: {callbackUri}");
            }
        }
    }
}
