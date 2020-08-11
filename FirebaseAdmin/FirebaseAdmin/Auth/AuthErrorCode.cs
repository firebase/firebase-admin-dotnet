// Copyright 2019, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Error codes that can be raised by the Firebase Auth APIs.
    /// </summary>
    public enum AuthErrorCode
    {
        /// <summary>
        /// Failed to retrieve required public key certificates.
        /// </summary>
        CertificateFetchFailed,

        /// <summary>
        /// The user with the provided email already exists.
        /// </summary>
        EmailAlreadyExists,

        /// <summary>
        /// The specified ID token is expired.
        /// </summary>
        ExpiredIdToken,

        /// <summary>
        /// The specified ID token is invalid.
        /// </summary>
        InvalidIdToken,

        /// <summary>
        /// The user with the provided phone number already exists.
        /// </summary>
        PhoneNumberAlreadyExists,

        /// <summary>
        /// The user with the provided uid already exists.
        /// </summary>
        UidAlreadyExists,

        /// <summary>
        /// Backend API responded with an unexpected message.
        /// </summary>
        UnexpectedResponse,

        /// <summary>
        /// No user record found for the given identifier.
        /// </summary>
        UserNotFound,

        /// <summary>
        /// Dynamic link domain specified in <see cref="ActionCodeSettings"/> is not authorized.
        /// </summary>
        InvalidDynamicLinkDomain,

        /// <summary>
        /// The specified ID token has been revoked.
        /// </summary>
        RevokedIdToken,

        /// <summary>
        /// The specified session cookie is invalid.
        /// </summary>
        InvalidSessionCookie,

        /// <summary>
        /// The specified session cookie is expired.
        /// </summary>
        ExpiredSessionCookie,

        /// <summary>
        /// The specified session cookie has been revoked.
        /// </summary>
        RevokedSessionCookie,

        /// <summary>
        /// No identity provider configuration found for the given identifier.
        /// </summary>
        ConfigurationNotFound,

        /// <summary>
        /// No tenant found for the given identifier.
        /// </summary>
        TenantNotFound,

        /// <summary>
        /// Tenant ID in a token does not match.
        /// </summary>
        TenantIdMismatch,
    }
}
