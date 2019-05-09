using System;
using System.Collections.Generic;
using System.Text;

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
