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
        private static readonly byte[] SignerKey = Encoding.UTF8.GetBytes("key%20");
        private static readonly byte[] SaltSeperator = Encoding.UTF8.GetBytes("separator");

        [Fact]
        public void Base()
        {
            var hash = new MockHash();
            var props = hash.GetProperties();

            var expectedResult = new Dictionary<string, object>
            {
                { "key", "value" },
                { "hashAlgorithm", "MockHash" },
            };

            Assert.Equal(expectedResult, hash.GetProperties());
        }

        [Fact]
        public void ScryptHash()
        {
            var scryptHash = new Scrypt()
            {
                Rounds = 8,
                Key = SignerKey,
                SaltSeparator = SaltSeperator,
                MemoryCost = 13,
            };
            var props = scryptHash.GetProperties();

            var expectedResult = new Dictionary<string, object>
            {
                { "hashAlgorithm", "SCRYPT" },
                { "rounds", 8 },
                { "signerKey", SignerKey },
                { "saltSeparator", SaltSeperator },
                { "memoryCost", 13 },
            };

            Assert.Equal(expectedResult, scryptHash.GetProperties());
        }

        [Fact]
        public void StandardScryptHash()
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
        public void RepeatableHashes()
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

            foreach (var entry in repeatableHashes)
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
        public void RepeatableHashesNoRounds()
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

            foreach (var entry in repeatableHashes)
            {
                Assert.Throws<ArgumentNullException>(() => entry.Value.GetProperties());
            }
        }

        [Fact]
        public void HmacHashes()
        {
            var hmacHashes = new Dictionary<string, Hmac>()
            {
                { "HMAC_MD5", new HmacMd5 { Key = SignerKey } },
                { "HMAC_SHA1", new HmacSha1 { Key = SignerKey } },
                { "HMAC_SHA256", new HmacSha256 { Key = SignerKey } },
                { "HMAC_SHA512", new HmacSha512 { Key = SignerKey } },
            };

            foreach (var entry in hmacHashes)
            {
                var expected = new Dictionary<string, object>()
                {
                    { "hashAlgorithm", entry.Key },
                    { "signerKey", SignerKey },
                };
                Assert.Equal(expected, entry.Value.GetProperties());
            }
        }

        [Fact]
        public void HmacHashesNoKey()
        {
            var hmacHashes = new Dictionary<string, Hmac>()
            {
                { "HMAC_MD5", new HmacMd5 { } },
                { "HMAC_SHA1", new HmacSha1 { } },
                { "HMAC_SHA256", new HmacSha256 { } },
                { "HMAC_SHA512", new HmacSha512 { } },
            };

            foreach (var entry in hmacHashes)
            {
                Assert.Throws<ArgumentException>(() => entry.Value.GetProperties());
            }
        }

        [Fact]
        public void RepeatableHashRoundsTooLow()
        {
            Assert.Throws<ArgumentException>(() => new Md5 { Rounds = -1 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha1 { Rounds = 0 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha256 { Rounds = 0 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha512 { Rounds = 0 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new PbkdfSha1 { Rounds = -1 }.GetProperties());
            Assert.Throws<ArgumentException>(
                () => new Pbkdf2Sha256 { Rounds = -1 }.GetProperties());
        }

        [Fact]
        public void RepeatableHashRoundsTooHigh()
        {
            Assert.Throws<ArgumentException>(() => new Md5 { Rounds = 8193 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha1 { Rounds = 8193 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha256 { Rounds = 8193 }.GetProperties());
            Assert.Throws<ArgumentException>(() => new Sha512 { Rounds = 8193 }.GetProperties());
            Assert.Throws<ArgumentException>(
                () => new PbkdfSha1 { Rounds = 120001 }.GetProperties());
            Assert.Throws<ArgumentException>(
                () => new Pbkdf2Sha256 { Rounds = 120001 }.GetProperties());
        }

        [Fact]
        public void ScryptHashConstraintsTooLow()
        {
            var scryptHash = new Scrypt()
            {
                Rounds = -1,
                Key = SignerKey,
                SaltSeparator = SaltSeperator,
                MemoryCost = 3,
            };

            Assert.Throws<ArgumentException>(() => scryptHash.GetProperties());

            scryptHash = new Scrypt()
            {
                Rounds = 3,
                Key = SignerKey,
                SaltSeparator = SaltSeperator,
                MemoryCost = 0,
            };

            Assert.Throws<ArgumentException>(() => scryptHash.GetProperties());
        }

        [Fact]
        public void ScryptHashConstraintsTooHigh()
        {
            var scryptHash = new Scrypt()
            {
                Rounds = 9,
                Key = SignerKey,
                SaltSeparator = SaltSeperator,
                MemoryCost = 3,
            };

            Assert.Throws<ArgumentException>(() => scryptHash.GetProperties());

            scryptHash = new Scrypt()
            {
                Rounds = 3,
                Key = SignerKey,
                SaltSeparator = SaltSeperator,
                MemoryCost = 15,
            };

            Assert.Throws<ArgumentException>(() => scryptHash.GetProperties());
        }

        [Fact]
        public void StandardScryptHashConstraintsTooLow()
        {
            var standardScryptHash = new StandardScrypt()
            {
                DerivedKeyLength = -1,
                BlockSize = 4,
                Parallelization = 2,
                MemoryCost = 13,
            };

            Assert.Throws<ArgumentException>(() => standardScryptHash.GetProperties());

            standardScryptHash = new StandardScrypt()
            {
                DerivedKeyLength = 2,
                BlockSize = -1,
                Parallelization = 2,
                MemoryCost = 13,
            };

            Assert.Throws<ArgumentException>(() => standardScryptHash.GetProperties());

            standardScryptHash = new StandardScrypt()
            {
                DerivedKeyLength = 2,
                BlockSize = 4,
                Parallelization = -2,
                MemoryCost = 13,
            };

            Assert.Throws<ArgumentException>(() => standardScryptHash.GetProperties());

            standardScryptHash = new StandardScrypt()
            {
                DerivedKeyLength = 2,
                BlockSize = 4,
                Parallelization = 2,
                MemoryCost = -1,
            };

            Assert.Throws<ArgumentException>(() => standardScryptHash.GetProperties());
        }

        [Fact]
        public void InstantiateInvalidHashType()
        {
            Assert.Throws<ArgumentException>(() => new InvalidHashType(null));
            Assert.Throws<ArgumentException>(() => new InvalidHashType(string.Empty));
        }

        private class MockHash : UserImportHash
        {
            public MockHash()
                : base("MockHash") { }

            protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
            {
                return new Dictionary<string, object>
                {
                    { "key", "value" },
                };
            }
        }

        private class InvalidHashType : UserImportHash
        {
            public InvalidHashType(string hashName)
                : base(hashName) { }

            protected override IReadOnlyDictionary<string, object> GetHashConfiguration()
            {
                return new Dictionary<string, object>
                {
                    { "key", "value" },
                };
            }
        }
    }
}
