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
  /// An abstract <a cref="UserImportHash">UserImportHash</a> implementation for specifying a <c>Rounds</c> count in
  /// a given range.
  /// </summary>
  public abstract class RepeatableHash : UserImportHash
  {
    /// <summary>
    /// Gets or sets the number of rounds for the repeatable hash.
    /// </summary>
    public int Rounds { get; set; }

    /// <summary>
    /// Gets the minimum number of rounds for that respective repeatable hash implementation.
    /// </summary>
    protected abstract int MinRounds { get; }

    /// <summary>
    /// Gets the maximum number of rounds for that respective repeatable hash implementation.
    /// </summary>
    protected abstract int MaxRounds { get; }

    /// <summary>
    /// Verifies that the specified Rounds are within the required bounds and returns an appropriate dictionary.
    /// </summary>
    /// <returns> Dictionary containing the number of rounds.</returns>
    protected override IReadOnlyDictionary<string, object> GetOptions()
    {
      if (this.Rounds < this.MinRounds || this.Rounds > this.MaxRounds)
      {
        throw new ArgumentException($"Rounds value must be between {this.MinRounds} and {this.MaxRounds} (inclusive).");
      }

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
