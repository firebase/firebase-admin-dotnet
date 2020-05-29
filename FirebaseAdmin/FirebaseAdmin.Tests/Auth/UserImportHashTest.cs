// Copyright 2019, Google Inc. All rights reserved.
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
using Xunit;

namespace FirebaseAdmin.Auth.Hash.Tests
{
  public class UserImportHashTest
  {
    private static string signerKey = "key%20";
    private static string saltSeperator = "separator";

    [Fact]
    public void TestBase()
    {
      UserImportHash hash = new MockHash();
      var props = hash.GetProperties();

      var expectedResult = new Dictionary<string, object>
        {
          { "key", "value" },
          { "hashAlgorithm", "MockHash" },
        };

      Assert.Equal(expectedResult, hash.GetProperties());
    }

    [Fact]
    public void TestScryptHash()
    {
      UserImportHash hash = new Scrypt()
      {
          Rounds = 8,
          Key = signerKey,
          SaltSeparator = saltSeperator,
          MemoryCost = 13,
      };
      var props = hash.GetProperties();

      var expectedResult = new Dictionary<string, object>
        {
          { "hashAlgorithm", "SCRYPT" },
          { "rounds", 8 },
          { "signerKey", "a2V5JTIw" },
          { "saltSeparator", "c2VwYXJhdG9y" },
          { "memoryCost", 13 },
        };

      Assert.Equal(expectedResult, hash.GetProperties());
    }

    [Fact]
    public void TestStandardScryptHash()
    {
      UserImportHash hash = new StandardScrypt()
      {
          DerivedKeyLength = 8,
          BlockSize = 4,
          Parallelization = 2,
          MemoryCost = 13,
      };
      var props = hash.GetProperties();

      var expectedResult = new Dictionary<string, object>
        {
          { "hashAlgorithm", "STANDARD_SCRYPT" },
          { "rounds", 8 },
          { "dkLen", 8 },
          { "blockSize", 4 },
          { "parallization", 2 },
          { "memoryCost", 13 },
        };

      Assert.Equal(expectedResult, hash.GetProperties());
    }

    [Fact]
    public void TestRepeatableHashes()
    {
      var repeatableHashes = new Dictionary<string, RepeatableHash>()
      {
          { "MD5", new Md5 { Rounds = 5 } },
          { "SHA1", new Sha1 { Rounds = 5 } },
          { "SHA256", new Sha256 { Rounds = 5 } },
          { "SHA512", new Sha512 { Rounds = 5 } },
          { "PBKDF_SHA1", new PdkdfSha1 { Rounds = 5 } },
          { "PBKDF2_SHA256", new Pdkdf2Sha256 { Rounds = 5 } },
      };

      foreach (KeyValuePair<string, RepeatableHash> entry in repeatableHashes)
      {
          var expected = new Dictionary<string, object>()
          {
            { "hashAlgorithm", entry.Key },
            { "rounds", 5 },
          };
          Assert.Equal(expected, entry.Value.GetProperties());
      }
    }

    [Fact]
    public void TestRepeatableHashesNoRounds()
    {
      var repeatableHashes = new Dictionary<string, RepeatableHash>()
      {
          { "MD5", new Md5 { } },
          { "SHA1", new Sha1 { } },
          { "SHA256", new Sha256 { } },
          { "SHA512", new Sha512 { } },
          { "PBKDF_SHA1", new PdkdfSha1 { } },
          { "PBKDF2_SHA256", new Pdkdf2Sha256 { } },
      };

      foreach (KeyValuePair<string, RepeatableHash> entry in repeatableHashes)
      {
          Assert.Throws<ArgumentException>(() => entry.Value.GetProperties());
      }
    }

    [Fact]
    public void TestHmacHashes()
    {
      var repeatableHashes = new Dictionary<string, Hmac>()
      {
          { "HMAC_MD5", new HmacMd5 { Key = signerKey } },
          { "HMAC_SHA1", new HmacSha1 { Key = signerKey } },
          { "HMAC_SHA256", new HmacSha256 { Key = signerKey } },
          { "HMAC_SHA512", new HmacSha512 { Key = signerKey } },
      };

      foreach (KeyValuePair<string, Hmac> entry in repeatableHashes)
      {
          var expected = new Dictionary<string, object>()
          {
            { "hashAlgorithm", entry.Key },
            { "signerKey", signerKey },
          };
          Assert.Equal(expected, entry.Value.GetProperties());
      }
    }

    [Fact]
    public void TestHmacHashesNoKey()
    {
      var repeatableHashes = new Dictionary<string, Hmac>()
      {
          { "HMAC_MD5", new HmacMd5 { } },
          { "HMAC_SHA1", new HmacSha1 { } },
          { "HMAC_SHA256", new HmacSha256 { } },
          { "HMAC_SHA512", new HmacSha512 { } },
      };

      foreach (KeyValuePair<string, Hmac> entry in repeatableHashes)
      {
          Assert.Throws<ArgumentException>(() => entry.Value.GetProperties());
      }
    }

    private class MockHash : UserImportHash
    {
      protected override string HashName { get { return "MockHash"; } }

      protected override IReadOnlyDictionary<string, object> GetOptions()
      {
        return new Dictionary<string, object>
           {
               { "key", "value" },
           };
      }
    }
  }
}
