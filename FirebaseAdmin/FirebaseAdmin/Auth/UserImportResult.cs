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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents the result of the
    /// <see cref="AbstractFirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs})"/> API.
    /// </summary>
    public sealed class UserImportResult
    {
        private readonly int users;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportResult"/> class.
        /// </summary>
        /// <param name="users">The number of users that was included in the input import
        /// request.</param>
        /// <param name="errors">Any errors encountered while importing users. Possibly
        /// null.</param>
        internal UserImportResult(int users, IReadOnlyList<ErrorInfo> errors)
        {
            this.users = users;
            this.Errors = errors ?? new List<ErrorInfo>();
        }

        /// <summary>
        /// Gets errors associated with a user import. Empty list if there were no errors.
        /// </summary>
        public IReadOnlyList<ErrorInfo> Errors { get; }

        /// <summary>
        /// Gets the number of users that were imported successfully.
        /// </summary>
        public int SuccessCount => this.users - this.FailureCount;

        /// <summary>
        /// Gets the number of users that failed to be imported.
        /// </summary>
        public int FailureCount => this.Errors?.Count ?? 0;
    }
}
