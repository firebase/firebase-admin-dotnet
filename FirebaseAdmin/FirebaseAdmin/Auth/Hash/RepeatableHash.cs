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
    /// An abstract <a cref="UserImportHash">UserImportHash</a> implementation for specifying a
    /// <c>Rounds</c> count in a given range.
    /// </summary>
    public abstract class RepeatableHash : UserImportHash
    {
        private int? rounds;

        /// <summary>
        /// Gets or sets the number of rounds for the repeatable hash. Verifies that the 
        /// specified <c>Rounds</c> are within the required bounds.
        /// </summary>
        public int Rounds
        {
            get
            {
                if (this.rounds == null)
                {
                    throw new ArgumentNullException("Rounds was not initialized");
                }

                return (int)this.rounds;
            }

            set
            {
                if (value < this.MinRounds || value > this.MaxRounds)
                {
                    throw new ArgumentException($"Rounds value must be between {this.MinRounds}"
                    + " and {this.MaxRounds} (inclusive).");
                }

                this.rounds = value;
            }
        }

        /// <summary>
        /// Gets the minimum number of rounds for that respective repeatable hash implementation.
        /// </summary>
        protected abstract int MinRounds { get; }

        /// <summary>
        /// Gets the maximum number of rounds for that respective repeatable hash implementation.
        /// </summary>
        protected abstract int MaxRounds { get; }

        /// <summary>
        /// Returns a dictionary specifying the number of rounds the hashing algorithm
        /// was set to iterate over.
        /// </summary>
        /// <returns>Dictionary containing the number of rounds.</returns>
        protected override IReadOnlyDictionary<string, object> GetOptions()
        {
            return new Dictionary<string, object>
            {
                {
                    "rounds",
                    this.Rounds
                },
            };
        }
    }
}
