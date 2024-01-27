namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Interface representing a verified App Check token response.
    /// </summary>
    public class AppCheckVerifyTokenResponse
    {
        /// <summary>
        /// Gets or sets App ID corresponding to the App the App Check token belonged to.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets decoded Firebase App Check token.
        /// </summary>
        public AppCheckDecodedToken Token { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether already conumed.
        /// </summary>
        public bool AlreadyConsumed { get; set; }
    }
}
