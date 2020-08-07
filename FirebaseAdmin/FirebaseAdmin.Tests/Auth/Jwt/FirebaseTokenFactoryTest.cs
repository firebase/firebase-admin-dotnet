// Copyright 2018, Google Inc. All rights reserved.
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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="FirebaseTokenFactory"/> class. These tests verify the core
    /// functionality of the above class, and do not verify how it's integrated with user-facing
    /// APIs like <see cref="FirebaseAuth"/>.
    /// </summary>
    public class FirebaseTokenFactoryTest
    {
        public static readonly IEnumerable<object[]> TenantIds = new List<object[]>()
        {
            new object[] { null },
            new object[] { "tenant1" },
        };

        private static readonly MockClock Clock = new MockClock();

        private static readonly MockSigner Signer = new MockSigner();

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task CreateCustomToken(string tenantId)
        {
            var factory = new FirebaseTokenFactory(Signer, Clock, tenantId);
            var token = await factory.CreateCustomTokenAsync("user1");
            MockSignedTokenVerifier.ForTenant(tenantId).VerifyCustomToken(token, "user1");
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task CreateCustomTokenWithEmptyClaims(string tenantId)
        {
            var factory = new FirebaseTokenFactory(Signer, Clock, tenantId);
            var token = await factory.CreateCustomTokenAsync(
                "user1", new Dictionary<string, object>());
            MockSignedTokenVerifier.ForTenant(tenantId).VerifyCustomToken(token, "user1");
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task CreateCustomTokenWithClaims(string tenantId)
        {
            var factory = new FirebaseTokenFactory(Signer, Clock, tenantId);
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };
            var token = await factory.CreateCustomTokenAsync("user2", developerClaims);
            MockSignedTokenVerifier.ForTenant(tenantId)
                .VerifyCustomToken(token, "user2", developerClaims);
        }

        [Fact]
        public async Task InvalidUid()
        {
            var factory = new FirebaseTokenFactory(Signer, Clock);
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(new string('a', 129)));
        }

        [Fact]
        public async Task ReservedClaims()
        {
            var factory = new FirebaseTokenFactory(new MockSigner(), Clock);
            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var developerClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };
                await Assert.ThrowsAsync<ArgumentException>(
                    async () => await factory.CreateCustomTokenAsync("user", developerClaims));
            }
        }

        [Fact]
        public void NullSigner()
        {
            Assert.Throws<ArgumentNullException>(() => new FirebaseTokenFactory(null, Clock));
        }

        [Fact]
        public void NullClock()
        {
            Assert.Throws<ArgumentNullException>(() => new FirebaseTokenFactory(Signer, null));
        }

        [Fact]
        public void EmptyTenantId()
        {
            Assert.Throws<ArgumentException>(
                () => new FirebaseTokenFactory(Signer, Clock, string.Empty));
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public void TenantId(string tenantId)
        {
            var factory = new FirebaseTokenFactory(Signer, Clock, tenantId);
            if (tenantId == null)
            {
                Assert.Null(factory.TenantId);
            }
            else
            {
                Assert.Equal(tenantId, factory.TenantId);
            }
        }

        private sealed class MockSigner : ISigner
        {
            public const string KeyIdString = "mock-key-id";
            public const string Signature = "signature";

            public Task<string> GetKeyIdAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(KeyIdString);
            }

            public Task<byte[]> SignDataAsync(byte[] data, CancellationToken cancellationToken)
            {
                return Task.FromResult(Encoding.UTF8.GetBytes(Signature));
            }

            public void Dispose() { }
        }

        private sealed class MockSignedTokenVerifier : CustomTokenVerifier
        {
            private readonly string expectedSignature;

            private MockSignedTokenVerifier(string issuer, string signature, string tenantId)
            : base(issuer, tenantId)
            {
                this.expectedSignature = signature;
            }

            internal static MockSignedTokenVerifier ForTenant(string tenantId)
            {
                return new MockSignedTokenVerifier(
                    MockSigner.KeyIdString, MockSigner.Signature, tenantId);
            }

            protected override void AssertSignature(string tokenData, string signature)
            {
                Assert.Equal(this.expectedSignature, JwtUtils.Base64Decode(signature));
            }
        }
    }
}
