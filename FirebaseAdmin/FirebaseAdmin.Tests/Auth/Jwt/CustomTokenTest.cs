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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Tests;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class CustomTokenTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { TestConfig.ForFirebaseAuth() },
            new object[] { TestConfig.ForTenantAwareFirebaseAuth("tenant1") },
            new object[] { TestConfig.ForFirebaseAuth().ForEmulator() },
            new object[] { TestConfig.ForTenantAwareFirebaseAuth("tenant1").ForEmulator() },
        };

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateCustomToken(TestConfig config)
        {
            var token = await config.CreateAuth().CreateCustomTokenAsync("user1");

            config.AssertCustomToken(token, "user1");
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateCustomTokenWithClaims(TestConfig config)
        {
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };

            var token = await config.CreateAuth().CreateCustomTokenAsync("user2", developerClaims);

            config.AssertCustomToken(token, "user2", developerClaims);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateCustomTokenCancel(TestConfig config)
        {
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var auth = config.CreateAuth();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => auth.CreateCustomTokenAsync("user1", canceller.Token));
        }

        public class TestConfig
        {
            private static readonly ServiceAccountCredential DefaultServiceAccount =
                GoogleCredential.FromFile("./resources/service_account.json").ToServiceAccountCredential();

            private readonly AuthBuilder authBuilder;
            private readonly CustomTokenVerifier tokenVerifier;

            private TestConfig(AuthBuilder authBuilder, CustomTokenVerifier tokenVerifier)
            {
                this.authBuilder = authBuilder;
                this.tokenVerifier = tokenVerifier;
            }

            public string TenantId => this.authBuilder.TenantId;

            public static TestConfig ForFirebaseAuth()
            {
                var authBuilder = new AuthBuilder
                {
                    Signer = new ServiceAccountSigner(DefaultServiceAccount),
                };
                var tokenVerifier = CustomTokenVerifier.FromDefaultServiceAccount();
                return new TestConfig(authBuilder, tokenVerifier);
            }

            public static TestConfig ForTenantAwareFirebaseAuth(string tenantId)
            {
                var authBuilder = new AuthBuilder
                {
                    TenantId = tenantId,
                    Signer = new ServiceAccountSigner(DefaultServiceAccount),
                };
                var tokenVerifier = CustomTokenVerifier.FromDefaultServiceAccount(tenantId);
                return new TestConfig(authBuilder, tokenVerifier);
            }

            internal TestConfig ForEmulator()
            {
                var authBuilder = new AuthBuilder
                {
                    TenantId = this.TenantId,
                    EmulatorHost = "localhost:9090",
                };
                var tokenVerifier = CustomTokenVerifier.ForEmulator(this.TenantId);
                return new TestConfig(authBuilder, tokenVerifier);
            }

            internal AbstractFirebaseAuth CreateAuth()
            {
                var options = new TestOptions
                {
                    TokenFactory = true,
                };
                return this.authBuilder.Build(options);
            }

            internal void AssertCustomToken(
                string token, string uid, Dictionary<string, object> claims = null)
            {
                this.tokenVerifier.Verify(token, uid, claims);
            }
        }
    }
}
