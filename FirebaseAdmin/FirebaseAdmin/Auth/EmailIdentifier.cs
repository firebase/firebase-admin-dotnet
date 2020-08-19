// Copyright 2020, Google Inc. All rights reserved.
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

using System.Collections.Generic;
using FirebaseAdmin.Auth.Users;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Used for looking up an account by email.
    ///
    /// See <see cref="AbstractFirebaseAuth.GetUsersAsync(IReadOnlyCollection{UserIdentifier})"/>.
    /// </summary>
    public sealed class EmailIdentifier : UserIdentifier
    {
        private readonly string email;

        /// <summary>
        /// Initializes a new instance of the <see cref="EmailIdentifier"/> class.
        /// </summary>
        /// <param name="email">The email.</param>
        public EmailIdentifier(string email)
        {
            this.email = UserRecordArgs.CheckEmail(email, required: true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "EmailIdentifier(" + this.email + ")";
        }

        internal override void Populate(GetAccountInfoRequest payload)
        {
            payload.AddEmail(this.email);
        }

        internal override bool Matches(UserRecord userRecord)
        {
            return this.email == userRecord.Email;
        }
    }
}
