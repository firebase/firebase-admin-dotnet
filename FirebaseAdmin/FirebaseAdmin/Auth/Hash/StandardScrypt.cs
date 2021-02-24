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
    /// Represents the Standard Scrypt password hashing algorithm. Can be used as an instance of
    /// <a cref="UserImportHash">UserImportHash</a> when importing users.
    /// </summary>
    public sealed class StandardScrypt : UserImportHash
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardScrypt"/> class.
        /// Defines the name of the hash to be equal to STANDARD_SCRYPT.
        /// </summary>
        public StandardScrypt()
            : base("STANDARD_SCRYPT") { }

        /// <summary>
        /// Gets or sets the derived key length for the hashing algorithm.
        /// <remarks>The length cannot be negative.</remarks>
        /// </summary>
        public int DerivedKeyLength { get; set; }

        /// <summary>
        /// Gets or sets the block size for the hashing algorithm.
        /// <remarks>The size cannot be negative.</remarks>
        /// </summary>
        public int BlockSize { get; set; }

        /// <summary>
        /// Gets or sets parallelization of the hashing algorithm.
        /// <remarks> The parallelization factor cannot be negative. </remarks>
        /// </summary>
        public int? Parallelization { get; set; }

        /// <summary>
        /// Gets or sets memory cost for the hashing algorithm.
        /// <remarks> The memory cost cannot be negative. </remarks>
        /// </summary>
        public int? MemoryCost { get; set; }

        /// <summary>
        /// Returns the options for the hashing algorithm.
        /// </summary>
        /// <returns>
        /// Dictionary defining options such as derived key length, block size, parallelization and memory cost.
        /// </returns>
        protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
        {
            this.ThrowIfNullOrNegative(
                this.DerivedKeyLength, "DerivedKeyLength cannot be null or negative");
            this.ThrowIfNullOrNegative(
                this.BlockSize, "BlockSize cannot be null or negative");
            this.ThrowIfNullOrNegative(
                this.Parallelization, "Parallelization cannot be null or negative");
            this.ThrowIfNullOrNegative(
                this.MemoryCost, "MemoryCost cannot be null or negative");

            var dict = new Dictionary<string, object>()
            {
                { "dkLen", this.DerivedKeyLength },
                { "blockSize", this.BlockSize },
                { "parallization", this.Parallelization },
                { "memoryCost", this.MemoryCost },
            };

            return dict;
        }

        private void ThrowIfNullOrNegative(int? value, string message)
        {
            if (value == null || value < 0)
            {
                throw new ArgumentException(message);
            }
        }
    }
}
