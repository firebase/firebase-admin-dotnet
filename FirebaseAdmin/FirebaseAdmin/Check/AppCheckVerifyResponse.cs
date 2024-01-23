using System;
using System.Collections.Generic;
using System.Text;
using FirebaseAdmin.Auth;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// AppCheckVerifyResponse.
    /// </summary>
    public class AppCheckVerifyResponse(string appId, FirebaseToken verifiedToken, bool alreadyConsumed = false)
    {
        /// <summary>
        /// Gets or sets a value indicating whether gets the Firebase App Check token.
        /// </summary>
        public bool AlreadyConsumed { get; set; } = alreadyConsumed;

        /// <summary>
        /// Gets or sets the Firebase App Check token.
        /// </summary>
        public string AppId { get; set; } = appId;

        /// <summary>
        /// Gets or sets the Firebase App Check VerifiedToken.
        /// </summary>
        public string VerifiedToken { get; set; } = verifiedToken;
    }
}
