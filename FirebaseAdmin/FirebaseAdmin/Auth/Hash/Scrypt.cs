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
        private string key;

        private string saltSeparator;

        private int? memoryCost;

        /// <summary>
        /// Gets or sets the signer key for the hashing algorithm.
        /// </summary>
        public string Key
        {
            get
            {
                if (this.key == null)
                {
                    throw new ArgumentNullException("key must be initialized");
                }

                return this.key;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("key must not be null or empty");
                }

                var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
                this.key = UrlSafeBase64Encode(plainTextBytes);
            }
        }

        /// <summary>
        /// Gets or sets the salt separator for the hashing algorithm.
        /// </summary>
        public string SaltSeparator
        {
            get
            {
                return this.saltSeparator;
            }

            set
            {
                if (value != null)
                {
                    var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(value);
                    this.saltSeparator = UrlSafeBase64Encode(plainTextBytes);
                }
                else
                {
                    this.saltSeparator = System.Convert.ToBase64String(new byte[0]);
                }
            }
        }

        /// <summary>
        /// Gets or sets the memory cost for the hashing algorithm.
        /// </summary>
        public int MemoryCost
        {
            get
            {
                if (this.memoryCost == null)
                {
                    throw new ArgumentNullException("memory cost must be set");
                }

                return (int)this.memoryCost;
            }

            set
            {
                if (value < 1 || value > 14)
                {
                    throw new ArgumentException("memory cost must be between 1 and 14 (inclusive)");
                }

                this.memoryCost = (int?)value;
            }
        }

        /// <summary>
        /// Gets the hash name which is SCRYPT.
        /// </summary>
        protected override string HashName { get { return "SCRYPT"; } }

        /// <summary>
        /// Gets the minimum number of rounds for a Scrypt hash which is 0.
        /// </summary>
        protected override int MinRounds { get { return 1; } }

        /// <summary>
        /// Gets the maximum number of rounds for a Scrypt hash which is 8.
        /// </summary>
        protected override int MaxRounds { get { return 8; } }

        /// <summary>
        /// Returns the options for the hashing algorithm.
        /// </summary>
        /// <returns>
        /// Dictionary defining options such as signer key, .
        /// </returns>
        protected override IReadOnlyDictionary<string, object> GetOptions()
        {
            var dict = new Dictionary<string, object>((Dictionary<string, object>)base.GetOptions());
            dict.Add("signerKey", this.Key);
            dict.Add("memoryCost", this.MemoryCost);
            dict.Add("saltSeparator", this.SaltSeparator);
            return dict;
        }

        private static string UrlSafeBase64Encode(byte[] bytes)
        {
            var base64Value = Convert.ToBase64String(bytes);
            return base64Value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
