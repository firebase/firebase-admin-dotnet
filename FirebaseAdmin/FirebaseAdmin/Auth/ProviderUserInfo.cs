using System;
using System.Collections.Generic;
using System.Text;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata regarding how a user is known by a particular identity provider (IdP).
    /// Instances of this class are immutable and thread safe.
    /// </summary>
    public sealed class ProviderUserInfo : IUserInfo
    {
        /// <summary>
        /// Gets or sets the user's unique ID assigned by the identity provider.
        /// </summary>
        /// <returns>a user ID string.</returns>
        [JsonProperty("rawId")]
        public string Uid { get; set; }

        /// <summary>
        /// Gets or sets the user's display name, if available.
        /// </summary>
        /// <returns>a display name string or null.</returns>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the user's email address, if available.
        /// </summary>
        /// <returns>an email address string or null.</returns>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        /// <returns>a phone number string or null.</returns>
        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the user's photo URL, if available.
        /// </summary>
        /// <returns>a URL string or null.</returns>
        [JsonProperty("photoUrl")]
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the ID of the identity provider. This can be a short domain name (e.g.
        /// google.com) or the identifier of an OpenID identity provider.
        /// </summary>
        /// <returns>an ID string that uniquely identifies the identity provider.</returns>
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderUserInfo"/> class with data provided by an authentication provider.
        /// </summary>
        /// <param name="provider">The deserialized JSON user data from the provider.</param>
        internal static ProviderUserInfo Create(GetAccountInfoResponse.Provider provider)
        {
            return new ProviderUserInfo()
            {
                Uid = provider.UserId,
                DisplayName = provider.DisplayName,
                Email = provider.Email,
                PhoneNumber = provider.PhoneNumber,
                PhotoUrl = provider.PhotoUrl,
                ProviderId = provider.ProviderID,
            };
        }
    }
}
