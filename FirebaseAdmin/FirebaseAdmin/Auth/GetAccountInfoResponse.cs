﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// JSON data binding for GetAccountInfoResponse messages sent by Google identity toolkit service.
    /// </summary>
    internal sealed class GetAccountInfoResponse
    {
        /// <summary>
        /// Gets or sets a string representing what kind of account is represented by this object.
        /// </summary>
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get; set; }

        /// <summary>
        /// Gets or sets a list of provider users linked to this account.
        /// </summary>
        [JsonProperty(PropertyName = "users")]
        public List<User> Users { get; set; }

        /// <summary>
        /// JSON data binding for user records.
        /// </summary>
        internal sealed class User
        {
            /// <summary>
            /// Gets or sets the user's ID.
            /// </summary>
            [JsonProperty(PropertyName = "localId")]
            public string UserId { get; set; }

            /// <summary>
            /// Gets or sets the user's email address.
            /// </summary>
            [JsonProperty(PropertyName = "email")]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the user's password hash.
            /// </summary>
            [JsonProperty(PropertyName = "passwordHash")]
            public string PasswordHash { get; set; }

            /// <summary>
            /// Gets or sets the user's password salt.
            /// </summary>
            [JsonProperty(PropertyName = "salt")]
            public string PasswordSalt { get; set; }

            /// <summary>
            /// Gets or sets the user's phone number.
            /// </summary>
            [JsonProperty(PropertyName = "phoneNumber")]
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the user's email address is verified or not.
            /// </summary>
            [JsonProperty(PropertyName = "emailVerified")]
            public bool EmailVerified { get; set; }

            /// <summary>
            /// Gets or sets the user's display name.
            /// </summary>
            [JsonProperty(PropertyName = "displayName")]
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets the URL for the user's photo.
            /// </summary>
            [JsonProperty(PropertyName = "photoUrl")]
            public string PhotoUrl { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the user is disabled or not.
            /// </summary>
            [JsonProperty(PropertyName = "disabled")]
            public bool Disabled { get; set; }

            /// <summary>
            /// Gets or sets a list of provider-specified data for this user.
            /// </summary>
            [JsonProperty(PropertyName = "providerUserInfo")]
            public List<Provider> Providers { get; set; }

            /// <summary>
            /// Gets or sets the timestamp representing the time that the user account was created.
            /// </summary>
            [JsonProperty(PropertyName = "createdAt")]
            public long CreatedAt { get; set; }

            /// <summary>
            /// Gets or sets the timestamp representing the last time that the user has logged in.
            /// </summary>
            [JsonProperty(PropertyName = "lastLoginAt")]
            public long LastLoginAt { get; set; }

            /// <summary>
            /// Gets or sets the timestamp representing the time that the user account was first valid.
            /// </summary>
            [JsonProperty(PropertyName = "validSince")]
            public long ValidSince { get; set; }

            /// <summary>
            /// Gets or sets the user's custom claims.
            /// </summary>
            [JsonProperty(PropertyName = "customAttributes")]
            public string CustomClaims { get; set; }
        }

        /// <summary>
        /// JSON data binding for provider data.
        /// </summary>
        internal sealed class Provider
        {
            /// <summary>
            /// Gets or sets the user's ID.
            /// </summary>
            [JsonProperty(PropertyName = "rawId")]
            public string UserId { get; set; }

            /// <summary>
            /// Gets or sets the user's display name.
            /// </summary>
            [JsonProperty(PropertyName = "displayName")]
            public string DisplayName { get; set; }

            /// <summary>
            /// Gets or sets the user's email address.
            /// </summary>
            [JsonProperty(PropertyName = "email")]
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets the user's phone number.
            /// </summary>
            [JsonProperty(PropertyName = "phoneNumber")]
            public string PhoneNumber { get; set; }

            /// <summary>
            /// Gets or sets the URL for the user's photo.
            /// </summary>
            [JsonProperty(PropertyName = "photoUrl")]
            public string PhotoUrl { get; set; }

            /// <summary>
            /// Gets or sets the provider's ID.
            /// </summary>
            [JsonProperty(PropertyName = "providerId")]
            public string ProviderID { get; set; }
        }
    }
}
