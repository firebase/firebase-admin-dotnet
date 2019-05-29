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
        private readonly long creationTimestampMillis;
        private readonly long lastSignInTimestampMillis;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetadata"/> class with the specified creation and last sign-in timestamps.
        /// </summary>
        /// <param name="creationTimestamp">A timestamp representing the date and time that the user account was created.</param>
        /// <param name="lastSignInTimestamp">A timestamp representing the date and time that the user account was last signed-on to.</param>
        internal UserMetadata(long creationTimestamp, long lastSignInTimestamp)
        {
            this.creationTimestampMillis = creationTimestamp;
            this.lastSignInTimestampMillis = lastSignInTimestamp;
        }

        /// <summary>
        /// Gets a timestamp representing the date and time that the account was created.
        /// If not available this property is <c>null</c>.
        /// </summary>
        public DateTime? CreationTimestamp
        {
            get => this.ToDateTime(this.creationTimestampMillis);
        }

        /// <summary>
        /// Gets a timestamp representing the last time that the user has signed in. If the user
        /// has never signed in this property is <c>null</c>.
        /// </summary>
        public DateTime? LastSignInTimestamp
        {
            get => this.ToDateTime(this.lastSignInTimestampMillis);
        }

        private DateTime? ToDateTime(long millisFromEpoch)
        {
            if (millisFromEpoch == 0)
            {
                return null;
            }

            return UserRecord.UnixEpoch.AddMilliseconds(millisFromEpoch);
        }
    }
}
