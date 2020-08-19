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

using FirebaseAdmin.Auth.Users;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata regarding how a user is known by a particular identity provider (IdP).
    /// Instances of this class are immutable and thread safe.
    /// </summary>
    internal sealed class ProviderUserInfo : IUserInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderUserInfo"/> class with data provided by an authentication provider.
        /// </summary>
        /// <param name="provider">The deserialized JSON user data from the provider.</param>
        internal ProviderUserInfo(GetAccountInfoResponse.Provider provider)
        {
            this.Uid = provider.UserId;
            this.DisplayName = provider.DisplayName;
            this.Email = provider.Email;
            this.PhoneNumber = provider.PhoneNumber;
            this.PhotoUrl = provider.PhotoUrl;
            this.ProviderId = provider.ProviderID;
        }

        /// <summary>
        /// Gets the user's unique ID assigned by the identity provider.
        /// </summary>
        /// <returns>a user ID string.</returns>
        public string Uid { get; private set; }

        /// <summary>
        /// Gets the user's display name, if available.
        /// </summary>
        /// <returns>a display name string or null.</returns>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Gets the user's email address, if available.
        /// </summary>
        /// <returns>an email address string or null.</returns>
        public string Email { get; private set; }

        /// <summary>
        /// Gets the user's phone number.
        /// </summary>
        /// <returns>a phone number string or null.</returns>
        public string PhoneNumber { get; private set; }

        /// <summary>
        /// Gets the user's photo URL, if available.
        /// </summary>
        /// <returns>a URL string or null.</returns>
        public string PhotoUrl { get; private set; }

        /// <summary>
        /// Gets the ID of the identity provider. This can be a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider.
        /// </summary>
        /// <returns>an ID string that uniquely identifies the identity provider.</returns>
        public string ProviderId { get; private set; }
    }
}
