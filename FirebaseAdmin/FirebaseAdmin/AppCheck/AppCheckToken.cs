using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Interface representing an App Check token.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AppCheckToken"/> class.
    /// </remarks>
    /// <param name="tokenValue">Generator from custom token.</param>
    /// <param name="ttlValue">TTl value .</param>
    public class AppCheckToken(string tokenValue, int ttlValue)
    {
        /// <summary>
        /// Gets the Firebase App Check token.
        /// </summary>
        public string Token { get; } = tokenValue;

        /// <summary>
        /// Gets or sets the time-to-live duration of the token in milliseconds.
        /// </summary>
        public int TtlMillis { get; set; } = ttlValue;
    }
}
