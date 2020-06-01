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
    /// <a cref="o:FirebaseAuth.ImportUsersAsync">FirebaseAuth.ImportUsersAsync</a> API.
    /// </summary>
    public class UserImportResult
    {
        private readonly int users;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportResult"/> class based on supplied
        /// users and <a cref="UploadAccountResponse">UploadAccountResponse</a> objects.
        /// </summary>
        /// <param name="users"> The number of users.</param>
        /// <param name="response"> The UploadAccountResponse generated from the post request.</param>
        public UserImportResult(int users, UploadAccountResponse response)
        {
            this.Errors = new List<ErrorInfo>(response.Errors ?? new List<ErrorInfo>());
            this.users = users;
        }

        /// <summary>
        /// Gets or sets errors associated with a user import.
        /// </summary>
        public IReadOnlyList<ErrorInfo> Errors { get; set; }

        /// <summary>
        /// Returns the number of users that were imported successfully.
        /// </summary>
        /// <returns>Number of users successfully imported (possibly zero).</returns>
        public int SuccessCount()
        {
            return this.users - this.Errors.Count;
        }

        /// <summary>
        /// Returns the number of users that failed to be imported.
        /// </summary>
        /// <returns>Number of users that resulted in import failures (possibly zero).</returns>
        public int FailureCount()
        {
            return this.Errors.Count;
        }
    }
}
