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
    /// Gets or sets the key for the hash.
    /// </summary>
    public string Key { get; set; }

    /// <summary>
    /// Verifies that the key was appropriately set and returns the options dictionary.
    /// </summary>
    /// <returns>
    /// Returns the dictionary containing an entry for the signing key.
    /// </returns>
    protected override IReadOnlyDictionary<string, object> GetOptions()
    {
      if (string.IsNullOrEmpty(this.Key))
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
