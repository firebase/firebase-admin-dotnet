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
using System.Linq;
using FirebaseAdmin.Auth.Users;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents the result of the
    /// <see cref="AbstractFirebaseAuth.GetUsersAsync(IReadOnlyCollection{UserIdentifier})"/> API.
    /// </summary>
    public sealed class GetUsersResult
    {
        internal GetUsersResult(GetAccountInfoResponse resp, IReadOnlyCollection<UserIdentifier> identifiers)
        {
            if (resp.Users != null)
            {
                this.Users = resp.Users.Select(user => new UserRecord(user));
            }
            else
            {
                this.Users = new List<UserRecord>();
            }

            this.NotFound = identifiers.Where(id => !this.IsUserFound(id, this.Users));
        }

        /// <summary>
        /// Gets user records corresponding to the set of users that were requested. Only
        /// users that were found are listed here. The result set is unordered.
        /// </summary>
        public IEnumerable<UserRecord> Users { get; }

        /// <summary>
        /// Gets the set of identifiers that were requested, but not found.
        /// </summary>
        public IEnumerable<UserIdentifier> NotFound { get; }

        private bool IsUserFound(UserIdentifier id, IEnumerable<UserRecord> userRecords)
        {
            return userRecords.Any(userRecord => id.Matches(userRecord));
        }
    }
}
