// Copyright 2019, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account, along with password hash and salt.
    /// Instances of this class are immutable and thread safe.
    /// </summary>
    public sealed class ExportedUserRecord : UserRecord
    {
        internal ExportedUserRecord(GetAccountInfoResponse.User user)
            : base(user)
        {
            this.PasswordHash = user.PasswordHash;
            this.PasswordSalt = user.PasswordSalt;
        }

        /// <summary>
        /// Gets the user's password hash as a base64-encoded string.
        /// If the Firebase Auth hashing algorithm (SCRYPT) was used to create the user account,
        /// returns the base64-encoded password hash of the user. If a different hashing algorithm was
        /// used to create this user, as is typical when migrating from another Auth system, returns
        /// an empty string. Returns null if no password is set.
        /// </summary>
        public string PasswordHash { get; }

        /// <summary>
        /// Gets the user's password salt as a base64-encoded string.
        /// If the Firebase Auth hashing algorithm (SCRYPT) was used to create the user account,
        /// returns the base64-encoded password salt of the user. If a different hashing algorithm was
        /// used to create this user, as is typical when migrating from another Auth system, returns
        /// an empty string. Returns null if no password is set.
        /// </summary>
        public string PasswordSalt { get; }
    }
}
