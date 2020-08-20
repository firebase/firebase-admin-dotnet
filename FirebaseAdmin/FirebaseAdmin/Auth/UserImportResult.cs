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
    /// Represents the result of
    /// <see cref="AbstractFirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs})"/>.
    /// </summary>
    public sealed class UserImportResult
    {
        private readonly int users;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportResult"/> class from the
        /// provided total user count and list of import errors.
        /// </summary>
        /// <param name="users">The number of users.</param>
        /// <param name="errors">Any errors generated from the import operation.</param>
        internal UserImportResult(int users, IReadOnlyList<ErrorInfo> errors)
        {
            this.Errors = errors;
            this.users = users;
        }

        /// <summary>
        /// Gets errors associated with a user import.
        /// </summary>
        public IReadOnlyList<ErrorInfo> Errors { get; private set; }

        /// <summary>
        /// Gets the number of users that were imported successfully.
        /// </summary>
        /// <returns>Number of users successfully imported (possibly zero).</returns>
        public int SuccessCount
        {
            get => this.users - this.FailureCount;
        }

        /// <summary>
        /// Gets the number of users that failed to be imported.
        /// </summary>
        /// <returns>Number of users that resulted in import failures (possibly zero).</returns>
        public int FailureCount
        {
            get => this.Errors?.Count ?? 0;
        }
    }
}
