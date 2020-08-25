using FirebaseAdmin.Auth.Multitenancy;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class TenantAwareOidcProviderConfigTest
    : AbstractOidcProviderConfigTest<TenantAwareFirebaseAuth>,
    IClassFixture<TenantAwareOidcProviderConfigTest.Fixture>
    {
        public TenantAwareOidcProviderConfigTest(Fixture fixture)
        : base(fixture) { }

        public class Fixture : OidcProviderConfigFixture<TenantAwareFirebaseAuth>
        {
            private readonly TenantFixture tenant = new TenantFixture();

            public override void Dispose()
            {
                base.Dispose();
                this.tenant.Dispose();
            }

            private protected override TenantAwareFirebaseAuth CreateAuth()
            {
                return this.tenant.Auth;
            }
        }
    }
}
