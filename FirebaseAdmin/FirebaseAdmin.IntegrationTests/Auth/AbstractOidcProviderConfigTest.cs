using System;
using System.Collections.Generic;
using System.Linq;
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

        public AbstractOidcProviderConfigTest(OidcProviderConfigFixture<T> fixture)
        {
            this.fixture = fixture;
            this.auth = fixture.Auth;
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
            var config = await this.auth.GetOidcProviderConfigAsync(this.fixture.ProviderId);

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

            var pagedEnumerable = this.auth.ListOidcProviderConfigsAsync(null);
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
            var args = new OidcProviderConfigArgs
            {
                ProviderId = this.fixture.ProviderId,
                DisplayName = "UPDATED_OIDC_DISPLAY_NAME",
                Enabled = false,
                ClientId = "UPDATED_OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/updated-issuer",
            };

            var config = await this.auth.UpdateProviderConfigAsync(args);

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
            await this.auth.DeleteProviderConfigAsync(this.fixture.ProviderId);

            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.auth.GetOidcProviderConfigAsync(this.fixture.ProviderId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }
}