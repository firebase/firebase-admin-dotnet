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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseTokenFactoryTest
    {
        [Fact]
        public async Task CreateCustomToken()
        {
            var clock = new MockClock();
            var factory = new FirebaseTokenFactory(new MockSigner(), clock);
            var token = await factory.CreateCustomTokenAsync("user1");
            VerifyCustomToken(token, "user1", null);
        }

        [Fact]
        public async Task CreateCustomTokenWithEmptyClaims()
        {
            var clock = new MockClock();
            var factory = new FirebaseTokenFactory(new MockSigner(), clock);
            var token = await factory.CreateCustomTokenAsync(
                "user1", new Dictionary<string, object>());
            VerifyCustomToken(token, "user1", null);
        }

        [Fact]
        public async Task CreateCustomTokenWithClaims()
        {
            var clock = new MockClock();
            var factory = new FirebaseTokenFactory(new MockSigner(), clock);
            var developerClaims = new Dictionary<string, object>()
            {
                {"admin", true},
                {"package", "gold"},
                {"magicNumber", 42L},
            };
            var token = await factory.CreateCustomTokenAsync("user2", developerClaims);
            VerifyCustomToken(token, "user2", developerClaims);
        }

        [Fact]
        public async Task InvalidUid()
        {
            var factory = new FirebaseTokenFactory(new MockSigner(), new MockClock());
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(null));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(""));
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await factory.CreateCustomTokenAsync(new String('a', 129)));
        }

        [Fact]
        public async Task ReservedClaims()
        {
            var factory = new FirebaseTokenFactory(new MockSigner(), new MockClock());
            foreach(var key in FirebaseTokenFactory.ReservedClaims)
            {
                var developerClaims = new Dictionary<string, object>(){
                    {key, "value"},
                };
                await Assert.ThrowsAsync<ArgumentException>(
                    async () => await factory.CreateCustomTokenAsync("user", developerClaims));    
            }    
        }

        private static void VerifyCustomToken(
            string token, string uid, Dictionary<string, object> claims)
        {
            String[] segments = token.Split(".");
            Assert.Equal(3, segments.Length);
            // verify header
            var header = JwtUtils.Decode<GoogleJsonWebSignature.Header>(segments[0]);
            Assert.Equal("JWT", header.Type);
            Assert.Equal("RS256", header.Algorithm);

            // verify payload
            var payload = JwtUtils.Decode<CustomTokenPayload>(segments[1]);
            Assert.Equal(MockSigner.KeyIdString, payload.Issuer);
            Assert.Equal(MockSigner.KeyIdString, payload.Subject);
            Assert.Equal(uid, payload.Uid);
            Assert.Equal(FirebaseTokenFactory.FirebaseAudience, payload.Audience);
            if (claims == null)
            {
                Assert.Null(payload.Claims);
            }
            else
            {
                Assert.Equal(claims.Count, payload.Claims.Count);
                foreach (var entry in claims)
                {
                    object value;
                    Assert.True(payload.Claims.TryGetValue(entry.Key, out value));
                    Assert.Equal(entry.Value, value);
                }
            }

            // verify mock signature
            Assert.Equal(MockSigner.Signature, JwtUtils.Base64Decode(segments[2]));
        }
    }

    internal sealed class MockSigner : ISigner
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

        public void Dispose() {}
    }
}
