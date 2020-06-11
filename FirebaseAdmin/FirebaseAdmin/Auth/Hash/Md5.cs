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

namespace FirebaseAdmin.Auth.Hash
{
    /// <summary>
    /// Represents the MD5 password hashing algorithm. Can be used as an instance of
    /// <a cref="UserImportHash">UserImportHash</a> when importing users.
    /// </summary>
    public sealed class Md5 : RepeatableHash
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Md5"/> class.
        /// Defines the name of the hash to be equal to MD5.
        /// </summary>
        public Md5()
            : base("MD5") { }

        /// <summary>
        /// Gets the minimum number of rounds for an MD5 hash, which is 0.
        /// </summary>
        protected override int MinRounds { get => 0; }

        /// <summary>
        /// Gets the maximum number of rounds for an MD5 hash, which is 8192.
        /// </summary>
        protected override int MaxRounds { get => 8192; }
    }
}
