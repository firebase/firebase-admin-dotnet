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
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Multitenancy;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    /// <summary>
    /// A fixture that ensures a test tenant is created before all the tests, and is deleted after
    /// all tests.
    /// </summary>
    public class TenantFixture : IDisposable
    {
        public TenantFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            var tenantManager = FirebaseAuth.DefaultInstance.TenantManager;
            var args = new TenantArgs
            {
                DisplayName = "admin-dotnet-tenant",
                PasswordSignUpAllowed = true,
                EmailLinkSignInEnabled = true,
            };
            this.Tenant = tenantManager.CreateTenantAsync(args).Result;
            this.Auth = tenantManager.AuthForTenant(this.Tenant.TenantId);
            this.TenantId = this.Tenant.TenantId;
        }

        public Tenant Tenant { get; set; }

        public TenantAwareFirebaseAuth Auth { get; }

        public string TenantId { get; }

        public void Dispose()
        {
            if (this.Tenant != null)
            {
                FirebaseAuth.DefaultInstance.TenantManager
                    .DeleteTenantAsync(this.Tenant.TenantId)
                    .Wait();
            }
        }
    }
}
