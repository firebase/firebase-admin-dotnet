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
    public class TenantAwareFirebaseAuthFixture : AbstractAuthFixture<TenantAwareFirebaseAuth>, IDisposable
    {
        private readonly TenantFixture tenant;

        public TenantAwareFirebaseAuthFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            this.tenant = new TenantFixture();
            this.UserBuilder = new TenantAwareTemporaryUserBuilder(this.tenant.Auth);
        }

        public override TenantAwareFirebaseAuth Auth => this.tenant.Auth;

        public override TemporaryUserBuilder UserBuilder { get; }

        public override string TenantId => this.Auth.TenantId;

        public override TenantAwareFirebaseAuth AuthFromApp(FirebaseApp app)
        {
            var auth = FirebaseAuth.GetAuth(app);
            return auth.TenantManager.AuthForTenant(this.Auth.TenantId);
        }

        public void Dispose()
        {
            this.UserBuilder.Dispose();
            this.tenant.Dispose();
        }

        private sealed class TenantAwareTemporaryUserBuilder : TemporaryUserBuilder
        {
            private readonly TenantAwareFirebaseAuth auth;

            public TenantAwareTemporaryUserBuilder(TenantAwareFirebaseAuth auth)
            {
                this.auth = auth;
            }

            private protected override AbstractFirebaseAuth Auth => auth;
        }
    }
}
