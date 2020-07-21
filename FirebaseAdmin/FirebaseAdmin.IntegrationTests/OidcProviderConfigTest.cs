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
using System.Linq;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public class OidcProviderConfigTest : IClassFixture<OidcProviderConfigFixture>
    {
        private readonly OidcProviderConfigFixture fixture;

        public OidcProviderConfigTest(OidcProviderConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        [TestRank(0)]
        public void CreateProviderConfig()
        {
            var config = this.fixture.ProviderConfig;

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/issuer", config.Issuer);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetProviderConfig()
        {
            var config = await FirebaseAuth.DefaultInstance.GetOidcProviderConfigAsync(
                this.fixture.ProviderId);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/issuer", config.Issuer);
        }

        [Fact]
        [TestRank(10)]
        public async Task ListProviderConfig()
        {
            OidcProviderConfig config = null;

            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListOidcProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                if (enumerator.Current.ProviderId == this.fixture.ProviderId)
                {
                    config = enumerator.Current;
                    break;
                }
            }

            Assert.NotNull(config);
            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/issuer", config.Issuer);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateProviderConfig()
        {
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = this.fixture.ProviderId,
                DisplayName = "UPDATED_OIDC_DISPLAY_NAME",
                Enabled = false,
                ClientId = "UPDATED_OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/updated-issuer",
            };

            var config = await FirebaseAuth.DefaultInstance.UpdateProviderConfigAsync(args);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("UPDATED_OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.False(config.Enabled);
            Assert.Equal("UPDATED_OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/updated-issuer", config.Issuer);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteProviderConfig()
        {
            await FirebaseAuth.DefaultInstance.DeleteProviderConfigAsync(this.fixture.ProviderId);

            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => FirebaseAuth.DefaultInstance.GetOidcProviderConfigAsync(this.fixture.ProviderId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }

    /// <summary>
    /// A fixture that allows reusing the same <see cref="AuthProviderConfig"/> instance across
    /// multiple test cases.
    /// </summary>
    public abstract class ProviderConfigFixture<T> : IDisposable
    where T : AuthProviderConfig
    {
        public string ProviderId { get; protected set; }

        public T ProviderConfig { get; internal set; }

        public void Dispose()
        {
            if (this.ProviderConfig != null)
            {
                FirebaseAuth.DefaultInstance
                    .DeleteProviderConfigAsync(this.ProviderConfig.ProviderId)
                    .Wait();
            }
        }

        protected static string GetRandomIdentifier(int length = 10)
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
            var suffix = new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
            return $"id-{suffix}";
        }
    }

    public class OidcProviderConfigFixture : ProviderConfigFixture<OidcProviderConfig>
    {
        public OidcProviderConfigFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            var providerId = $"oidc.{GetRandomIdentifier()}";
            var args = new OidcProviderConfigArgs()
            {
                ProviderId = providerId,
                DisplayName = "OIDC_DISPLAY_NAME",
                Enabled = true,
                ClientId = "OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };
            this.ProviderConfig = FirebaseAuth.DefaultInstance
                .CreateProviderConfigAsync(args)
                .Result;
            this.ProviderId = providerId;
        }
    }
}
