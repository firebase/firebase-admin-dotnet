using FirebaseAdmin.Auth.Multitenancy;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class TenantAwareSamlProviderConfigTest
    : AbstractSamlProviderConfigTest<TenantAwareFirebaseAuth>,
    IClassFixture<TenantAwareSamlProviderConfigTest.Fixture>
    {
        public TenantAwareSamlProviderConfigTest(Fixture fixture)
        : base(fixture) { }

        public class Fixture : SamlProviderConfigFixture<TenantAwareFirebaseAuth>
        {
            private readonly TenantFixture tenant = new TenantFixture();

            public override TenantAwareFirebaseAuth Auth => this.tenant.Auth;

            public override void Dispose()
            {
                base.Dispose();
                this.tenant.Dispose();
            }
        }
    }
}
