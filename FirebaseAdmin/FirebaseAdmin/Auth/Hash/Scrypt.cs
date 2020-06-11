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

namespace FirebaseAdmin.Auth.Hash
{
    /// <summary>
    /// Represents the Scrypt password hashing algorithm. This is the
    /// <a href="https://github.com/firebase/scrypt">modified Scrypt algorithm</a> used by
    /// Firebase Auth. See <a cref="StandardScrypt">StandardScrypt</a> for the standard
    /// Scrypt algorithm. Can be used as an instance of
    /// <a cref="UserImportHash">UserImportHash</a> when importing users.
    /// </summary>
    public sealed class Scrypt : RepeatableHash
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Scrypt"/> class.
        /// Defines the name of the hash to be equal to SCRYPT.
        /// </summary>
        public Scrypt()
            : base("SCRYPT") { }

        /// <summary>
        /// Gets or sets the signer key for the hashing algorithm.
        /// </summary>
        public byte[] Key { get; set; }

        /// <summary>
        /// Gets or sets the salt separator for the hashing algorithm.
        /// </summary>
        public byte[] SaltSeparator { get; set; }

        /// <summary>
        /// Gets or sets the memory cost for the hashing algorithm.
        /// </summary>
        public int? MemoryCost { get; set; }

        /// <summary>
        /// Gets the minimum number of rounds for a Scrypt hash, which is 0.
        /// </summary>
        protected override int MinRounds { get { return 0; } }

        /// <summary>
        /// Gets the maximum number of rounds for a Scrypt hash, which is 8.
        /// </summary>
        protected override int MaxRounds { get { return 8; } }

        /// <summary>
        /// Returns the options for the hashing algorithm.
        /// </summary>
        /// <returns>
        /// Dictionary defining options such as signer key.
        /// </returns>
        protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
        {
            if (this.Key == null || this.Key.Length == 0)
            {
                throw new ArgumentException("key must not be null or empty");
            }

            if (this.SaltSeparator == null)
            {
                this.SaltSeparator = new byte[0];
            }

            if (this.MemoryCost == null)
            {
                throw new ArgumentNullException("memory cost must be set");
            }

            if (this.MemoryCost < 1 || this.MemoryCost > 14)
            {
                throw new ArgumentException("memory cost must be between 1 and 14 (inclusive)");
            }

            var dict = new Dictionary<string, object>()
            {
                { "signerKey", this.Key },
                { "memoryCost", (int)this.MemoryCost },
                { "saltSeparator", this.SaltSeparator },
            };

            foreach (var entry in base.GetHashConfiguration())
            {
                dict[entry.Key] = entry.Value;
            }

            return dict;
        }
    }
}
