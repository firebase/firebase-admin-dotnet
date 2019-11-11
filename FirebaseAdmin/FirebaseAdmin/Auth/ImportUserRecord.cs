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

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a user account to be imported to Firebase Auth via the
    /// FirebaseAuth#importUsers(List, UserImportOptions) API. Must contain at least a
    /// uid string.
    /// </summary>
    public sealed class ImportUserRecord : UserRecord
    {
        internal ImportUserRecord(GetAccountInfoResponse.User user)
            : base(user)
        {
            this.PasswordHash = user.PasswordHash;
            this.PasswordSalt = user.PasswordSalt;
        }

        /// <summary>
        /// Gets or sets the user's password hash as a base64-encoded string.
        /// If at least one user account carries a password hash, a UserImportHash must be specified when
        /// calling the FirebaseAuth#importUsersAsync(List, UserImportOptions) method.
        /// </summary>
        public string PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the user's password salt as a base64-encoded string.
        /// </summary>
        public string PasswordSalt { get; set; }
    }
}