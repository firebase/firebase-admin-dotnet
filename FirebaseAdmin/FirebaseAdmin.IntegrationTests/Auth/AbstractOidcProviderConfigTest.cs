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

using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public abstract class AbstractOidcProviderConfigTest<T>
    where T : AbstractFirebaseAuth
    {
        private readonly OidcProviderConfigFixture<T> fixture;
        private readonly T auth;
        private readonly string providerId;

        public AbstractOidcProviderConfigTest(OidcProviderConfigFixture<T> fixture)
        {
            this.fixture = fixture;
            this.auth = fixture.Auth;
            this.providerId = fixture.ProviderId;
        }

        [Fact]
        [TestRank(0)]
        public void CreateProviderConfig()
        {
            var config = this.fixture.ProviderConfig;

            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/issuer", config.Issuer);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetProviderConfig()
        {
            var config = await this.auth.GetOidcProviderConfigAsync(this.providerId);

            Assert.Equal(this.providerId, config.ProviderId);
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

            var pagedEnumerable = this.auth.ListOidcProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current.ProviderId == this.providerId)
                {
                    config = enumerator.Current;
                    break;
                }
            }

            Assert.NotNull(config);
            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/issuer", config.Issuer);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateProviderConfig()
        {
            var args = new OidcProviderConfigArgs
            {
                ProviderId = this.providerId,
                DisplayName = "UPDATED_OIDC_DISPLAY_NAME",
                Enabled = false,
                ClientId = "UPDATED_OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/updated-issuer",
            };

            var config = await this.auth.UpdateProviderConfigAsync(args);

            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("UPDATED_OIDC_DISPLAY_NAME", config.DisplayName);
            Assert.False(config.Enabled);
            Assert.Equal("UPDATED_OIDC_CLIENT_ID", config.ClientId);
            Assert.Equal("https://oidc.com/updated-issuer", config.Issuer);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteProviderConfig()
        {
            await this.auth.DeleteProviderConfigAsync(this.providerId);
            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.auth.GetOidcProviderConfigAsync(this.providerId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }
}
