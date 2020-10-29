namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A collection of standard profile information for a user. Used to expose profile information
    /// returned by an identity provider.
    /// </summary>
    public interface IUserInfo
    {
        /// <summary>
        /// Gets the user's unique ID assigned by the identity provider.
        /// </summary>
        string Uid { get; }

        /// <summary>
        /// Gets the user's display name, if available. Otherwise null.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Gets the user's email address, if available. Otherwise null.
        /// </summary>
        string Email { get; }

        /// <summary>
        /// Gets the user's phone number, if available. Otherwise null.
        /// </summary>
        string PhoneNumber { get; }

        /// <summary>
        /// Gets the user's photo URL, if available. Otherwise null.
        /// </summary>
        string PhotoUrl { get; }

        /// <summary>
        /// Gets the ID of the identity provider. This can be a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider.
        /// </summary>
        string ProviderId { get; }
    }
}
