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
    /// Used for looking up an account by uid.
    ///
    /// See <see cref="AbstractFirebaseAuth.GetUsersAsync(IReadOnlyCollection{UserIdentifier})"/>.
    /// </summary>
    public sealed class UidIdentifier : UserIdentifier
    {
        private readonly string uid;

        /// <summary>
        /// Initializes a new instance of the <see cref="UidIdentifier"/> class.
        /// </summary>
        /// <param name="uid">The uid.</param>
        public UidIdentifier(string uid)
        {
            this.uid = UserRecordArgs.CheckUid(uid, required: true);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "UidIdentifier(" + this.uid + ")";
        }

        internal override void Populate(GetAccountInfoRequest payload)
        {
            payload.AddUid(this.uid);
        }

        internal override bool Matches(UserRecord userRecord)
        {
            return this.uid == userRecord.Uid;
        }
    }
}
