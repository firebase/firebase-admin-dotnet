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

namespace FirebaseAdmin.Auth.Hash
{
    /// <summary>
    /// Represents the Bcrypt password hashing algorithm. Can be used as an instance of
    /// <a cref="UserImportHash">UserImportHash</a> when importing users.
    /// </summary>
    public sealed class Bcrypt : UserImportHash
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Bcrypt"/> class.
        /// Defines name to be equal to BCRYPT.
        /// </summary>
        public Bcrypt()
            : base("BCRYPT") { }

        /// <summary>
        /// Returns an empty dictionary representing no options for the Bcrypt hashing algorithm.
        /// </summary>
        /// <returns>
        /// Dictionary defining no options.
        /// </returns>
        protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
        {
            return new Dictionary<string, object>();
        }
    }
}
