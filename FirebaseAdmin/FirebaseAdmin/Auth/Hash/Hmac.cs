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
    /// Base class for Hmac type hashes.
    /// </summary>
    public abstract class Hmac : UserImportHash
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hmac"/> class.
        /// Propogates the name to <c>UserImportHash</c>.
        /// </summary>
        /// <param name="hashName">The name of the hashing algorithm.</param>
        internal Hmac(string hashName)
            : base(hashName) { }

        /// <summary>
        /// Gets or sets the key for the hash.
        /// </summary>
        // Note: Newtonsoft Base64 encodes the byte array by default.
        // Source: https://www.newtonsoft.com/json/help/html/SerializationGuide.htm#PrimitiveTypes
        public byte[] Key { get; set; }

        /// <summary>
        /// Verifies that the is key non-empty or null and returns the options dictionary.
        /// </summary>
        /// <returns>
        /// Returns the dictionary containing an entry for the signing key.
        /// </returns>
        protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
        {
            if (this.Key == null || this.Key.Length == 0)
            {
                throw new ArgumentException("key must not be null or empty");
            }

            return new Dictionary<string, object>
            {
                { "signerKey", this.Key },
            };
        }
    }
}
