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
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Multitenancy
{
    public class TenantAwareFirebaseAuthTest : IDisposable
    {
        private const string MockTenantId = "tenant1";

        [Fact]
        public void NullTenantId()
        {
            var args = TenantAwareFirebaseAuth.Args.CreateDefault(null);

            Assert.Throws<ArgumentException>(() => new TenantAwareFirebaseAuth(args));
        }

        [Fact]
        public void EmptyTenantId()
        {
            var args = TenantAwareFirebaseAuth.Args.CreateDefault(string.Empty);

            Assert.Throws<ArgumentException>(() => new TenantAwareFirebaseAuth(args));
        }

        [Fact]
        public void UseAfterDelete()
        {
            var app = CreateFirebaseApp();
            var auth = FirebaseAuth.DefaultInstance.TenantManager.AuthForTenant(MockTenantId);

            app.Delete();

            Assert.Throws<InvalidOperationException>(() => auth.TokenFactory);
            Assert.Throws<InvalidOperationException>(() => auth.IdTokenVerifier);
            Assert.Throws<InvalidOperationException>(() => auth.UserManager);
            Assert.Throws<InvalidOperationException>(() => auth.ProviderConfigManager);
        }

        [Fact]
        public void TenantId()
        {
            var app = CreateFirebaseApp();

            var auth = FirebaseAuth.DefaultInstance.TenantManager.AuthForTenant(MockTenantId);

            Assert.Equal(MockTenantId, auth.TenantId);
            Assert.Equal(MockTenantId, auth.TokenFactory.TenantId);
            Assert.Equal(MockTenantId, auth.IdTokenVerifier.TenantId);
            Assert.Equal(MockTenantId, auth.UserManager.TenantId);
            Assert.Equal(MockTenantId, auth.ProviderConfigManager.TenantId);
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private static FirebaseApp CreateFirebaseApp()
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromAccessToken("token"),
                ProjectId = "project1",
            };
            return FirebaseApp.Create(options);
        }
    }
}
