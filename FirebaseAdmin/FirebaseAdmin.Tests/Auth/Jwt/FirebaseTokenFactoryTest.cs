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
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class FirebaseTokenFactoryTest
    {
        public static readonly IEnumerable<object[]> TenantIds = new List<object[]>
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
            var factory = CreateTokenFactory(tenantId);

            var token = await factory.CreateCustomTokenAsync("user1");

            MockCustomTokenVerifier.WithTenant(tenantId).Verify(token, "user1");
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task CreateCustomTokenWithEmptyClaims(string tenantId)
        {
            var factory = CreateTokenFactory(tenantId);

            var token = await factory.CreateCustomTokenAsync(
                "user1", new Dictionary<string, object>());

            MockCustomTokenVerifier.WithTenant(tenantId).Verify(token, "user1");
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task CreateCustomTokenWithClaims(string tenantId)
        {
            var factory = CreateTokenFactory(tenantId);
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };

            var token = await factory.CreateCustomTokenAsync("user2", developerClaims);

            MockCustomTokenVerifier.WithTenant(tenantId).Verify(token, "user2", developerClaims);
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task InvalidUid(string tenantId)
        {
            var factory = CreateTokenFactory(tenantId);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(new string('a', 129)));
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public async Task ReservedClaims(string tenantId)
        {
            var factory = CreateTokenFactory(tenantId);
            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var developerClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };

                await Assert.ThrowsAsync<ArgumentException>(
                    () => factory.CreateCustomTokenAsync("user", developerClaims));
            }
        }

        [Fact]
        public void NullArgs()
        {
            Assert.Throws<ArgumentNullException>(() => new FirebaseTokenFactory(null));
        }

        [Fact]
        public void NullSigner()
        {
            var args = new FirebaseTokenFactory.Args
            {
                Signer = null,
            };

            Assert.Throws<ArgumentNullException>(() => new FirebaseTokenFactory(args));
        }

        [Fact]
        public void EmptyTenantId()
        {
            var args = new FirebaseTokenFactory.Args
            {
                Signer = Signer,
                TenantId = string.Empty,
            };

            Assert.Throws<ArgumentException>(() => new FirebaseTokenFactory(args));
        }

        [Fact]
        public void SignerOnly()
        {
            var args = new FirebaseTokenFactory.Args
            {
                Signer = Signer,
            };

            var factory = new FirebaseTokenFactory(args);

            Assert.Same(SystemClock.Default, factory.Clock);
            Assert.Null(factory.TenantId);
            Assert.False(factory.IsEmulatorMode);
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public void ProductionMode(string tenantId)
        {
            var factory = CreateTokenFactory(tenantId);

            Assert.False(factory.IsEmulatorMode);
            Assert.Same(Signer, factory.Signer);
            Assert.Same(Clock, factory.Clock);
            AssertTenantId(tenantId, factory);
        }

        [Theory]
        [MemberData(nameof(TenantIds))]
        public void EmulatorMode(string tenantId)
        {
            var args = new FirebaseTokenFactory.Args
            {
                IsEmulatorMode = true,
                TenantId = tenantId,
            };
            var factory = new FirebaseTokenFactory(args);

            Assert.True(factory.IsEmulatorMode);
            Assert.Same(EmulatorSigner.Instance, factory.Signer);
            Assert.Same(SystemClock.Default, factory.Clock);
            AssertTenantId(tenantId, factory);
        }

        private static void AssertTenantId(string expectedTenantId, FirebaseTokenFactory factory)
        {
            if (expectedTenantId == null)
            {
                Assert.Null(factory.TenantId);
            }
            else
            {
                Assert.Equal(expectedTenantId, factory.TenantId);
            }
        }

        private static FirebaseTokenFactory CreateTokenFactory(string tenantId)
        {
            var args = new FirebaseTokenFactory.Args
            {
                Signer = Signer,
                Clock = Clock,
                TenantId = tenantId,
            };
            return new FirebaseTokenFactory(args);
        }

        private sealed class MockSigner : ISigner
        {
            public const string KeyIdString = "mock-key-id";
            public const string Signature = "signature";

            public string Algorithm => JwtUtils.AlgorithmRS256;

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

        private sealed class MockCustomTokenVerifier : CustomTokenVerifier
        {
            private readonly string expectedSignature;

            private MockCustomTokenVerifier(string issuer, string signature, string tenantId)
            : base(issuer, tenantId)
            {
                this.expectedSignature = signature;
            }

            internal static MockCustomTokenVerifier WithTenant(string tenantId)
            {
                return new MockCustomTokenVerifier(
                    MockSigner.KeyIdString, MockSigner.Signature, tenantId);
            }

            protected override void AssertSignature(string tokenData, string signature)
            {
                Assert.Equal(this.expectedSignature, JwtUtils.Base64Decode(signature));
            }
        }
    }
}
