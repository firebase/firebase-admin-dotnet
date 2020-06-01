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
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Encapsulates user import requests by specifying hashing properties for passwords and
    /// the list of users to be imported.
    /// </summary>
    public class UserImportRequest
    {
        internal const int MaxImportUsers = 1000;

        [JsonProperty("users")]
        private IEnumerable<IReadOnlyDictionary<string, object>> users;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportRequest"/> class by verifying
        /// the supplied users IEnumerable is valid (non-empty and not greater than
        /// <c>MaxImportUsers</c>), and a valid <see cref="UserImportHash"/> is supplied when a
        /// password is provided to at least one of the users.
        /// </summary>
        /// <param name="usersToImport"> List of users to be imported.</param>
        /// <param name="options"> Options for user imports, see
        /// <a cref="UserImportOptions">UserImportOptions</a>.</param>
        /// <returns>Dictionary containing key/values for password hashing algorithm.</returns>
        public UserImportRequest(
            IEnumerable<ImportUserRecordArgs> usersToImport,
            UserImportOptions options)
        {
            if (usersToImport.Count() == 0)
            {
                throw new ArgumentException("users must not be empty");
            }

            if (usersToImport.Count() > MaxImportUsers)
            {
                throw new ArgumentException($"users list must not contain more than"
                    + " {MaxImportUsers} items");
            }

            bool hasPassword = false;
            List<IReadOnlyDictionary<string, object>> usersLst =
                new List<IReadOnlyDictionary<string, object>>();
            foreach (ImportUserRecordArgs user in usersToImport)
            {
                hasPassword = hasPassword || user.HasPassword();
                usersLst.Add(user.GetProperties());
            }

            this.users = usersLst;

            if (hasPassword)
            {
                if (options == null || options.Hash == null)
                {
                    throw new ArgumentNullException("UserImportHash option is required when at"
                        + " least one user has a password. Provide a UserImportHash via the"
                        + " Hash setter.");
                }

                this.HashingProperties = (Dictionary<string, object>)options.GetHashProperties();
            }
        }

        /// <summary>
        /// Gets or sets JsonExtensionData for putting hashing properties at the root of the
        /// Json serialized object.
        /// </summary>
        /// <returns>Dictionary containing key/values for password hashing algorithm.</returns>
        [JsonExtensionData]
        protected Dictionary<string, object> HashingProperties { get; set; }

        /// <summary>
        /// Retrives the number of users based on the constructor parameter.
        /// </summary>
        /// <returns>Number of users.</returns>
        public int GetUsersCount()
        {
            return this.users.Count();
        }
    }
}
