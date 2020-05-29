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
    private int? derivedKeyLength;

    private int? blockSize;

    private int? memoryCost;

    private int? parallelization;

    /// <summary>
    /// Gets or sets the derived key length for the hashing algorithm.
    /// <remark>The length cannot be negative.</remark>
    /// </summary>
    public int DerivedKeyLength
    {
      get
      {
        if (this.derivedKeyLength == null)
        {
          throw new ArgumentException("DerivedKeyLength must be initialized");
        }

        return (int)this.derivedKeyLength;
      }

      set
      {
        if (value < 0)
        {
          throw new ArgumentException("DerivedKeyLength must be non-negative");
        }

        this.derivedKeyLength = value;
      }
    }

    /// <summary>
    /// Gets or sets the block size for the hashing algorithm.
    /// <remark>The size cannot be negative.</remark>
    /// </summary>
    public int BlockSize
    {
      get
      {
        if (this.blockSize == null)
        {
          throw new ArgumentException("BlockSize must be initialized");
        }

        return (int)this.blockSize;
      }

      set
      {
        if (value < 0)
        {
          throw new ArgumentException("BlockSize must be non-negative");
        }

        this.blockSize = value;
      }
    }

    /// <summary>
    /// Gets or sets parallelization of the hashing algorithm.
    /// <remark> The parallelization factor cannot be negative. </remark>
    /// </summary>
    public int Parallelization
    {
      get
      {
        if (this.parallelization == null)
        {
          throw new ArgumentException("Parallelization must be initialized");
        }

        return (int)this.parallelization;
      }

      set
      {
        if (value < 0)
        {
          throw new ArgumentException("Parallelization must be non-negative");
        }

        this.parallelization = value;
      }
    }

    /// <summary>
    /// Gets or sets memory cost for the hashing algorithm.
    /// <remark> The memory cost cannot be negative. </remark>
    /// </summary>
    public int MemoryCost
    {
      get
      {
        if (this.memoryCost == null)
        {
          throw new ArgumentException("Memory cost must be initialized");
        }

        return (int)this.memoryCost;
      }

      set
      {
        if (value < 0)
        {
          throw new ArgumentException("Memory cost must be non-negative");
        }

        this.memoryCost = value;
      }
    }

    /// <summary>
    /// Gets the hash name which is STANDARD_SCRYPT.
    /// </summary>
    protected override string HashName { get { return "STANDARD_SCRYPT"; } }

    /// <summary>
    /// Returns the options for the hashing algorithm.
    /// </summary>
    /// <returns>
    /// Dictionary defining options such as derived key length, block size, parallization and memory cost.
    /// </returns>
    protected override IReadOnlyDictionary<string, object> GetOptions()
    {
      var dict = new Dictionary<string, object>();
      dict.Add("dkLen", this.DerivedKeyLength);
      dict.Add("blockSize", this.BlockSize);
      dict.Add("parallization", this.Parallelization);
      dict.Add("memoryCost", this.MemoryCost);
      return dict;
    }
  }
}
