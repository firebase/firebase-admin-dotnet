using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata regarding how a user is known by a particular identity provider (IdP).
    /// Instances of this class are immutable and thread safe.
    /// </summary>
    public sealed class ProviderUserInfo
    {
        private string uid;
        private string displayName;
        private string email;
        private string phoneNumber;
        private string photoUrl;
        private string providerId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderUserInfo"/> class with data provided by an authentication provider.
        /// </summary>
        /// <param name="provider">The deserialized JSON user data from the provider.</param>
        public ProviderUserInfo(Internal.GetAccountInfoResponse.Provider provider)
        {
            this.uid = provider.UserID;
            this.displayName = provider.DisplayName;
            this.email = provider.Email;
            this.phoneNumber = provider.PhoneNumber;
            this.photoUrl = provider.PhotoUrl;
            this.providerId = provider.ProviderID;
        }

        /// <summary>
        /// Gets the user's ID.
        /// </summary>
        public string UserID { get => this.uid; }

        /// <summary>
        /// Gets the user's display name.
        /// </summary>
        public string DisplayName { get => this.displayName; }

        /// <summary>
        /// Gets the user's email address.
        /// </summary>
        public string Email { get => this.email; }

        /// <summary>
        /// Gets the user's phone number.
        /// </summary>
        public string PhoneNumber { get => this.phoneNumber; }

        /// <summary>
        /// Gets the URL for the user's photo/avatar.
        /// </summary>
        public string PhotoUrl { get => this.photoUrl; }

        /// <summary>
        /// Gets the user's ID specified by the provider.
        /// </summary>
        public string ProviderId { get => this.providerId; }
    }
}
