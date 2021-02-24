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
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class CustomTokenTest : IDisposable
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { FirebaseAuthTestConfig.DefaultInstance },
            new object[] { TenantAwareFirebaseAuthTestConfig.DefaultInstance },
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

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateCustomTokenInvalidCredential(TestConfig config)
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromAccessToken("test-token"),
                ProjectId = "project1",
            };
            var auth = config.CreateAuth(options);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => auth.CreateCustomTokenAsync("user1"));

            var errorMessage = "Failed to determine service account ID. Make sure to initialize the SDK "
                + "with service account credentials or specify a service account "
                + "ID with iam.serviceAccounts.signBlob permission. Please refer to "
                + "https://firebase.google.com/docs/auth/admin/create-custom-tokens for "
                + "more details on creating custom tokens.";
            Assert.Equal(errorMessage, ex.Message);
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        public abstract class TestConfig
        {
            protected static readonly AppOptions DefaultOptions = new AppOptions
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            };

            internal abstract CustomTokenVerifier TokenVerifier { get; }

            internal abstract AbstractFirebaseAuth CreateAuth(AppOptions options = null);

            internal void AssertCustomToken(
                string token, string uid, Dictionary<string, object> claims = null)
            {
                this.TokenVerifier.Verify(token, uid, claims);
            }
        }

        private sealed class FirebaseAuthTestConfig : TestConfig
        {
            internal static readonly FirebaseAuthTestConfig DefaultInstance =
                new FirebaseAuthTestConfig();

            internal override CustomTokenVerifier TokenVerifier =>
                CustomTokenVerifier.FromDefaultServiceAccount();

            internal override AbstractFirebaseAuth CreateAuth(AppOptions options = null)
            {
                FirebaseApp.Create(options ?? DefaultOptions);
                return FirebaseAuth.DefaultInstance;
            }
        }

        private sealed class TenantAwareFirebaseAuthTestConfig : TestConfig
        {
            internal static readonly TenantAwareFirebaseAuthTestConfig DefaultInstance =
                new TenantAwareFirebaseAuthTestConfig();

            internal override CustomTokenVerifier TokenVerifier =>
                CustomTokenVerifier.FromDefaultServiceAccount("tenant1");

            internal override AbstractFirebaseAuth CreateAuth(AppOptions options = null)
            {
                FirebaseApp.Create(options ?? DefaultOptions);
                return FirebaseAuth.DefaultInstance.TenantManager.AuthForTenant("tenant1");
            }
        }
    }
}
