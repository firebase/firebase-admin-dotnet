using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains additional metadata associated with a user account.
    /// </summary>
    public sealed class UserMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetadata"/> class with the specified creation and last sign-in timestamps.
        /// </summary>
        /// <param name="creationTimestamp">A timestamp representing the date and time that the user account was created.</param>
        /// <param name="lastSignInTimestamp">A timestamp representing the date and time that the user account was last signed-on to.</param>
        internal UserMetadata(long creationTimestamp, long lastSignInTimestamp)
        {
            this.CreationTimestamp = creationTimestamp;
            this.LastSignInTimestamp = lastSignInTimestamp;
        }

        /// <summary>
        /// Gets or sets a timestamp representing the date and time that the account was created.
        /// </summary>
        [JsonProperty("creationTimestamp")]
        public long CreationTimestamp { get; set; }

        /// <summary>
        /// Gets or sets a timestamp representing the last time that the user has logged in.
        /// </summary>
        [JsonProperty("lastSignInTimestamp")]
        public long LastSignInTimestamp { get; set; }
    }
}
