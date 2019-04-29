using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    internal class ExportedUserRecord : UserRecord
    {
        private string passwordHash;
        private string passwordSalt;

        public ExportedUserRecord(string uid)
            : base(uid)
        {
        }

        /// <summary>
        /// Gets or sets the user's password hash as a base64-encoded string.
        /// </summary>
        /// <p>If the Firebase Auth hashing algorithm (SCRYPT) was used to create the user account,
        /// returns the base64-encoded password hash of the user.If a different hashing algorithm was
        /// used to create this user, as is typical when migrating from another Auth system, returns
        /// an empty string.</p>
        public string PasswordHash
        {
            get => this.passwordHash;
            set => this.passwordHash = value;
        }

        /// <summary>
        /// Gets or sets the user's password salt as a base64-encoded string.
        /// </summary>
        /// <p>If the Firebase Auth hashing algorithm (SCRYPT) was used to create the user account,
        /// returns the base64-encoded password hash of the user.If a different hashing algorithm was
        /// used to create this user, as is typical when migrating from another Auth system, returns
        /// an empty string.</p>
        public string PasswordSalt
        {
            get => this.passwordSalt;
            set => this.passwordSalt = value;
        }
    }
}
