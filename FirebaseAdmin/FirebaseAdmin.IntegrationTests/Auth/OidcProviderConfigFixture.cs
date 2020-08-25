using System;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public abstract class OidcProviderConfigFixture<T> : IDisposable
    where T : AbstractFirebaseAuth
    {
        public OidcProviderConfigFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            this.Auth = this.CreateAuth();

            var providerId = $"oidc.{AuthIntegrationUtils.GetRandomIdentifier()}";
            var args = new OidcProviderConfigArgs
            {
                ProviderId = providerId,
                DisplayName = "OIDC_DISPLAY_NAME",
                Enabled = true,
                ClientId = "OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };
            this.ProviderConfig = this.Auth.CreateProviderConfigAsync(args).Result;
        }

        public OidcProviderConfig ProviderConfig { get; set; }

        public string ProviderId => this.ProviderConfig.ProviderId;

        public T Auth { get; }

        public virtual void Dispose()
        {
            if (this.ProviderConfig != null)
            {
                this.Auth.DeleteProviderConfigAsync(this.ProviderConfig.ProviderId).Wait();
            }
        }

        private protected abstract T CreateAuth();
    }
}
