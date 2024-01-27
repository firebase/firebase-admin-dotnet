namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Interface representing an App Check token.
    /// </summary>
    /// <param name="tokenValue">Generator from custom token.</param>
    /// <param name="ttlValue">TTl value .</param>
    public class AppCheckToken(string tokenValue, int ttlValue)
    {
        /// <summary>
        /// Gets or sets the Firebase App Check token.
        /// </summary>
        public string Token { get; set; } = tokenValue;

        /// <summary>
        /// Gets or sets the time-to-live duration of the token in milliseconds.
        /// </summary>
        public int TtlMillis { get; set; } = ttlValue;
    }
}
