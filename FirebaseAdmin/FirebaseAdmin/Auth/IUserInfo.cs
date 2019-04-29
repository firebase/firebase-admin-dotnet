using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A collection of standard profile information for a user.
    /// Used to expose profile information returned by an identity provider.
    /// </summary>
    internal interface IUserInfo
    {
        /// <summary>
        /// Gets or sets the user's unique ID assigned by the identity provider.
        /// </summary>
        string Uid { get; set; }

        /// <summary>
        /// Gets or sets the user's display name.
        /// </summary>
        string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the user's email address, if available.
        /// </summary>
        string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number, if available.
        /// </summary>
        string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the user's photo URL, if available.
        /// </summary>
        string PhotoUrl { get; set; }

        /// <summary>
        /// Gets the ID of the identity provider, such as a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider.
        /// </summary>
        string ProviderId { get; }
    }
}
