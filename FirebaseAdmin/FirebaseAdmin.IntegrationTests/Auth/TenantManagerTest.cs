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
using FirebaseAdmin.Auth.Multitenancy;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public class TenantManagerTest : IClassFixture<TenantFixture>
    {
        private readonly TenantFixture fixture;

        public TenantManagerTest(TenantFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        [TestRank(0)]
        public void CreateTenant()
        {
            var tenant = this.fixture.Tenant;

            Assert.Equal("admin-dotnet-tenant", tenant.DisplayName);
            Assert.True(tenant.PasswordSignUpAllowed);
            Assert.True(tenant.EmailLinkSignInEnabled);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetTenant()
        {
            var tenant = await FirebaseAuth.DefaultInstance.TenantManager
                .GetTenantAsync(this.fixture.TenantId);

            Assert.Equal(this.fixture.TenantId, tenant.TenantId);
            Assert.Equal("admin-dotnet-tenant", tenant.DisplayName);
            Assert.True(tenant.PasswordSignUpAllowed);
            Assert.True(tenant.EmailLinkSignInEnabled);
        }

        [Fact]
        [TestRank(10)]
        public async Task ListTenants()
        {
            Tenant tenant = null;

            var pagedEnumerable = FirebaseAuth.DefaultInstance.TenantManager
                .ListTenantsAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current.TenantId == this.fixture.TenantId)
                {
                    tenant = enumerator.Current;
                    break;
                }
            }

            Assert.NotNull(tenant);
            Assert.Equal(this.fixture.TenantId, tenant.TenantId);
            Assert.Equal("admin-dotnet-tenant", tenant.DisplayName);
            Assert.True(tenant.PasswordSignUpAllowed);
            Assert.True(tenant.EmailLinkSignInEnabled);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateTenant()
        {
            var args = new TenantArgs
            {
                DisplayName = "new-dotnet-tenant",
                PasswordSignUpAllowed = false,
                EmailLinkSignInEnabled = false,
            };

            var tenant = await FirebaseAuth.DefaultInstance.TenantManager
                .UpdateTenantAsync(this.fixture.TenantId, args);

            Assert.Equal(this.fixture.TenantId, tenant.TenantId);
            Assert.Equal("new-dotnet-tenant", tenant.DisplayName);
            Assert.False(tenant.PasswordSignUpAllowed);
            Assert.False(tenant.EmailLinkSignInEnabled);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteTenant()
        {
            var tenantId = this.fixture.TenantId;
            await FirebaseAuth.DefaultInstance.TenantManager.DeleteTenantAsync(tenantId);

            this.fixture.Tenant = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => FirebaseAuth.DefaultInstance.TenantManager.GetTenantAsync(tenantId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.TenantNotFound, exception.AuthErrorCode);
        }
    }
}
