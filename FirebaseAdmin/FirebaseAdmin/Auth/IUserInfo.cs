using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A collection of standard profile information for a user. Used to expose profile information
    /// returned by an identity provider.
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// Returns the user's unique ID assigned by the identity provider.
        /// </summary>
        /// <returns>a user ID string.</returns>
        string GetUid();

        /// <summary>
        /// Returns the user's display name, if available.
        /// </summary>
        /// <returns>a display name string or null.</returns>
        string GetDisplayName();

        /// <summary>
        /// Returns the user's email address, if available.
        /// </summary>
        /// <returns>an email address string or null.</returns>
        string GetEmail();

        /// <summary>
        /// Returns the user's phone number, if available.
        /// </summary>
        /// <returns>a phone number string or null.</returns>
        string GetPhoneNumber();

        /// <summary>
        /// Returns the user's photo URL, if available.
        /// </summary>
        /// <returns>a URL string or null.</returns>
        string GetPhotoUrl();

        /// <summary>
        /// Returns the ID of the identity provider. This can be a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider.
        /// </summary>
        /// <returns>an ID string that uniquely identifies the identity provider.</returns>
        string GetProviderId();
    }
}
