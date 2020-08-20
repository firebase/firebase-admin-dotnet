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
    /// Used for looking up an account by phone number.
    ///
    /// See <see cref="AbstractFirebaseAuth.GetUsersAsync(IReadOnlyCollection{UserIdentifier})"/>.
    /// </summary>
    public sealed class PhoneIdentifier : UserIdentifier
    {
        private readonly string phoneNumber;

        /// <summary>
        /// Initializes a new instance of the <see cref="PhoneIdentifier"/> class.
        /// </summary>
        /// <param name="phoneNumber">The phoneNumber.</param>
        public PhoneIdentifier(string phoneNumber)
        {
            this.phoneNumber = UserRecordArgs.CheckPhoneNumber(phoneNumber, required: true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "PhoneIdentifier(" + this.phoneNumber + ")";
        }

        internal override void Populate(GetAccountInfoRequest payload)
        {
            payload.AddPhoneNumber(this.phoneNumber);
        }

        internal override bool Matches(UserRecord userRecord)
        {
            return this.phoneNumber == userRecord.PhoneNumber;
        }
    }
}
