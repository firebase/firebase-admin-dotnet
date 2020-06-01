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
using System.Text;
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
                { "PBKDF_SHA1", new PbkdfSha1 { Rounds = 5 } },
                { "PBKDF2_SHA256", new Pbkdf2Sha256 { Rounds = 5 } },
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
                { "PBKDF_SHA1", new PbkdfSha1 { } },
                { "PBKDF2_SHA256", new Pbkdf2Sha256 { } },
            };

            foreach (KeyValuePair<string, RepeatableHash> entry in repeatableHashes)
            {
                Assert.Throws<ArgumentNullException>(() => entry.Value.GetProperties());
            }
        }

        [Fact]
        public void TestHmacHashes()
        {
            var hmacHashes = new Dictionary<string, Hmac>()
            {
                { "HMAC_MD5", new HmacMd5 { Key = signerKey } },
                { "HMAC_SHA1", new HmacSha1 { Key = signerKey } },
                { "HMAC_SHA256", new HmacSha256 { Key = signerKey } },
                { "HMAC_SHA512", new HmacSha512 { Key = signerKey } },
            };

            foreach (KeyValuePair<string, Hmac> entry in hmacHashes)
            {
                var expected = new Dictionary<string, object>()
                {
                    { "hashAlgorithm", entry.Key },
                    { "signerKey", Encoding.ASCII.GetBytes(signerKey) },
                };
                Assert.Equal(expected, entry.Value.GetProperties());
            }
        }

        [Fact]
        public void TestHmacHashesNoKey()
        {
            var hmacHashes = new Dictionary<string, Hmac>()
            {
                { "HMAC_MD5", new HmacMd5 { } },
                { "HMAC_SHA1", new HmacSha1 { } },
                { "HMAC_SHA256", new HmacSha256 { } },
                { "HMAC_SHA512", new HmacSha512 { } },
            };

            foreach (KeyValuePair<string, Hmac> entry in hmacHashes)
            {
                Assert.Throws<ArgumentException>(() => entry.Value.GetProperties());
            }
        }

        [Fact]
        public void TestRepeatableHashRoundsTooLow()
        {
            Assert.Throws<ArgumentException>(() => new Md5 { Rounds = -1 });
            Assert.Throws<ArgumentException>(() => new Sha1 { Rounds = 0 });
            Assert.Throws<ArgumentException>(() => new Sha256 { Rounds = 0 });
            Assert.Throws<ArgumentException>(() => new Sha512 { Rounds = 0 });
            Assert.Throws<ArgumentException>(() => new PbkdfSha1 { Rounds = -1 });
            Assert.Throws<ArgumentException>(() => new Pbkdf2Sha256 { Rounds = -1 });
        }

        [Fact]
        public void TestRepeatableHashRoundsTooHigh()
        {
            Assert.Throws<ArgumentException>(() => new Md5 { Rounds = 8193 });
            Assert.Throws<ArgumentException>(() => new Sha1 { Rounds = 8193 });
            Assert.Throws<ArgumentException>(() => new Sha256 { Rounds = 8193 });
            Assert.Throws<ArgumentException>(() => new Sha512 { Rounds = 8193 });
            Assert.Throws<ArgumentException>(() => new PbkdfSha1 { Rounds = 120001 });
            Assert.Throws<ArgumentException>(() => new Pbkdf2Sha256 { Rounds = 120001 });
        }

        [Fact]
        public void TestScryptHashConstraintsTooLow()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Scrypt()
                {
                    Rounds = -1,
                    Key = signerKey,
                    SaltSeparator = saltSeperator,
                    MemoryCost = 3,
                };
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new Scrypt()
                {
                    Rounds = 3,
                    Key = signerKey,
                    SaltSeparator = saltSeperator,
                    MemoryCost = 0,
                };
            });
        }

        [Fact]
        public void TestScryptHashConstraintsTooHigh()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new Scrypt()
                {
                    Rounds = 9,
                    Key = signerKey,
                    SaltSeparator = saltSeperator,
                    MemoryCost = 3,
                };
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new Scrypt()
                {
                    Rounds = 3,
                    Key = signerKey,
                    SaltSeparator = saltSeperator,
                    MemoryCost = 15,
                };
            });
        }

        [Fact]
        public void TestStandardScryptHashConstraintsTooLow()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new StandardScrypt()
                {
                    DerivedKeyLength = -1,
                    BlockSize = 4,
                    Parallelization = 2,
                    MemoryCost = 13,
                };
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new StandardScrypt()
                {
                    DerivedKeyLength = 2,
                    BlockSize = -1,
                    Parallelization = 2,
                    MemoryCost = 13,
                };
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new StandardScrypt()
                {
                    DerivedKeyLength = 2,
                    BlockSize = 4,
                    Parallelization = -2,
                    MemoryCost = 13,
                };
            });

            Assert.Throws<ArgumentException>(() =>
            {
                new StandardScrypt()
                {
                    DerivedKeyLength = 2,
                    BlockSize = 4,
                    Parallelization = 2,
                    MemoryCost = -1,
                };
            });
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
