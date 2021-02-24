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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A collection of options that can be passed to the
    /// <a cref="o:FirebaseAuth.ImportUsersAsync">FirebaseAuth.ImportUsersAsync</a> API.
    /// </summary>
    public sealed class UserImportOptions
    {
        /// <summary>
        /// Gets or sets the <a cref="UserImportHash">UserImportHash</a> object associated with
        /// the UserImportOptions instance.
        /// </summary>
        public UserImportHash Hash { get; set; }

        /// <summary>
        /// Retrieves properties of the password hashing algorithm.
        /// </summary>
        /// <returns>Dictionary containing key/values for password hashing properties.</returns>
        internal Dictionary<string, object> GetHashProperties()
        {
            if (this.Hash == null)
            {
                throw new ArgumentException("UserImportHash Hash was not defined");
            }

            return this.Hash.GetProperties();
        }
    }
}
