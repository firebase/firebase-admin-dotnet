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

namespace FirebaseAdmin.Auth
{
    // TODO(rsgowman): This class is expected to also be used for the
    // ImportUsersAsync() method... once that exists.

    /// <summary>
    /// Represents an error encountered while deleting users via the
    /// <see cref="M:FirebaseAuth.DeleteUsersAsync(IReadOnlyList{String})"/> API.
    /// </summary>
    public sealed class ErrorInfo
    {
        internal ErrorInfo(int index, string reason)
        {
            this.Index = index;
            this.Reason = reason;
        }

        /// <summary>
        /// Gets the index of the user that was unable to be deleted in the list passed to the
        /// <see cref="M:FirebaseAuth.DeleteUsersAsync(IReadOnlyList{String})"/> method.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Gets a string describing the error.
        /// </summary>
        public string Reason { get; }
    }
}
