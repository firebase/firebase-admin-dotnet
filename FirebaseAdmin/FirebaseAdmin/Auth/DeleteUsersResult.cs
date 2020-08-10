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

using System;
using System.Collections.Generic;
using System.Linq;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents the result of the
    /// <see cref="AbstractFirebaseAuth.DeleteUsersAsync(IReadOnlyList{string})"/> API.
    /// </summary>
    public sealed class DeleteUsersResult
    {
        internal DeleteUsersResult(int users, BatchDeleteResponse response)
        {
            var errors = new List<ErrorInfo>();
            if (response.Errors != null)
            {
                if (users < response.Errors.Count)
                {
                    string errorMessages =
                        string.Join(",", response.Errors.Select(errorInfo => errorInfo.Message));
                    throw new InvalidOperationException(string.Format(
                        "Internal error: More errors encountered ({0}) than users ({1}). Errors: {2}",
                        response.Errors.Count,
                        users,
                        errorMessages));
                }

                foreach (BatchDeleteResponse.ErrorInfo error in response.Errors)
                {
                    errors.Add(new ErrorInfo(error.Index, error.Message));
                }
            }

            this.Errors = errors;
            this.SuccessCount = users - errors.Count;
        }

        /// <summary>
        /// Gets the number of users that were deleted successfully (possibly zero). Users that
        /// did not exist prior to calling
        /// <see cref="AbstractFirebaseAuth.DeleteUsersAsync(IReadOnlyList{string})"/> are considered to
        /// be successfully deleted.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets the number of users that `DeleteUsersAsync` failed to be deleted (possibly zero).
        /// </summary>
        public int FailureCount { get => this.Errors.Count; }

        /// <summary>
        /// Gets a list of <see cref="ErrorInfo"/> instances describing the errors that were
        /// encountered during the deletion. Length of this list is equal to the return value of
        /// <see cref="FailureCount"/>.
        /// </summary>
        /// <returns>A non-null list (possibly empty).</returns>
        public IReadOnlyList<ErrorInfo> Errors { get; }
    }
}
