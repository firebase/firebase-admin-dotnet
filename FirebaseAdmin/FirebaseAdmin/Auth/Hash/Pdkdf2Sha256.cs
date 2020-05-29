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
  /// Represents the PBKDF2 SHA256 password hashing algorithm. Can be used as an instance of
  /// <a cref="UserImportHash">UserImportHash</a> when importing users.
  /// </summary>
  public sealed class Pdkdf2Sha256 : RepeatableHash
  {
    /// <summary>
    /// Gets the hash name which is PBKDF2_SHA256.
    /// </summary>
    protected override string HashName { get { return "PBKDF2_SHA256"; } }

    /// <summary>
    /// Gets the minimum number of rounds for an Pdkdf2 Sha256 hash which is 0.
    /// </summary>
    protected override int MinRounds { get { return 0; } }

    /// <summary>
    /// Gets the maximum number of rounds for an Pdkdf2 Sha256 hash which is 120000.
    /// </summary>
    protected override int MaxRounds { get { return 120000; } }
  }
}