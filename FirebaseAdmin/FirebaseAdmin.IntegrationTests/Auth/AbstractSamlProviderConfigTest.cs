using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public abstract class AbstractSamlProviderConfigTest<T>
    where T : AbstractFirebaseAuth
    {
        private readonly SamlProviderConfigFixture<T> fixture;
        private readonly T auth;

        public AbstractSamlProviderConfigTest(SamlProviderConfigFixture<T> fixture)
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
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetProviderConfig()
        {
            var config = await this.auth.GetSamlProviderConfigAsync(
                this.fixture.ProviderId);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task ListProviderConfig()
        {
            SamlProviderConfig config = null;

            var pagedEnumerable = this.auth.ListSamlProviderConfigsAsync(null);
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
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateProviderConfig()
        {
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = this.fixture.ProviderId,
                DisplayName = "UPDATED_SAML_DISPLAY_NAME",
                Enabled = false,
                IdpEntityId = "UPDATED_IDP_ENTITY_ID",
                SsoUrl = "https://example.com/updated-login",
                X509Certificates = new List<string>
                {
                    SamlProviderConfigFixture<T>.X509Certificates[1],
                },
                RpEntityId = "UPDATED_RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/updated-handler",
            };

            var config = await this.auth.UpdateProviderConfigAsync(args);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("UPDATED_SAML_DISPLAY_NAME", config.DisplayName);
            Assert.False(config.Enabled);
            Assert.Equal("UPDATED_IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/updated-login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[1]);
            Assert.Equal("UPDATED_RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal(
                "https://projectId.firebaseapp.com/__/auth/updated-handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteProviderConfig()
        {
            var providerId = this.fixture.ProviderId;

            await this.auth.DeleteProviderConfigAsync(providerId);
            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.auth.GetSamlProviderConfigAsync(providerId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }
}
