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
    /// Represents a hash algorithm and the related configuration parameters used to hash user
    /// passwords. An instance of this class must be specified if importing any users with password
    /// hashes (see <a cref="UserImportOptions">UserImportOptions</a>).
    /// </summary>
    /// <remarks>
    /// This is not expected to be extended in user code. Applications should use one of the provided
    ///  concrete implementations in the <a cref="FirebaseAdmin.Auth.Hash">namespace</a>. See
    /// <a href="https://firebase.google.com/docs/auth/admin/import-users">documentation</a> for more
    /// details on available options.
    /// </remarks>
    public abstract class UserImportHash
    {
        private string hashName;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportHash"/> class.
        /// </summary>
        /// <param name="hashName">The name of the hashing algorithm.</param>
        protected UserImportHash(string hashName)
        {
            if (string.IsNullOrEmpty(hashName))
            {
                throw new ArgumentException("Hash name cannot be empty or null");
            }

            this.hashName = hashName;
        }

        /// <summary>
        /// Retrieves the properties of the chosen hashing algorithm.
        /// </summary>
        /// <returns>Dictionary containing the specified properties of the hashing algorithm.</returns>
        internal Dictionary<string, object> GetProperties()
        {
            var options = this.GetHashConfiguration();
            var properties = new Dictionary<string, object>();
            foreach (var entry in options)
            {
                properties[entry.Key] = entry.Value;
            }

            properties.Add("hashAlgorithm", this.hashName);
            return properties;
        }

        /// <summary>
        /// Retrieves dictionary with the specified properties of the hashing algorithm.
        /// </summary>
        /// <returns>Dictionary containing the specified properties of the hashing algorithm.</returns>
        protected abstract IReadOnlyDictionary<string, object> GetHashConfiguration();
    }
}
