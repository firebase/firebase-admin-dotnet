// Copyright 2018, Google Inc. All rights reserved.
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
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth.OAuth2;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseAuthTest : IDisposable
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void GetAuthWithoutApp()
        {
            Assert.Null(FirebaseAuth.DefaultInstance);
        }

        [Fact]
        public void GetDefaultAuth()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            Assert.Same(auth, FirebaseAuth.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseAuth.DefaultInstance);
        }

        [Fact]
        public void GetAuth()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential }, "MyApp");
            FirebaseAuth auth = FirebaseAuth.GetAuth(app);
            Assert.Same(auth, FirebaseAuth.GetAuth(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseAuth.GetAuth(app));
        }

        [Fact]
        public void UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            var auth = FirebaseAuth.DefaultInstance;

            app.Delete();

            Assert.Throws<InvalidOperationException>(() => auth.TokenFactory);
            Assert.Throws<InvalidOperationException>(() => auth.IdTokenVerifier);
            Assert.Throws<InvalidOperationException>(() => auth.SessionCookieVerifier);
            Assert.Throws<InvalidOperationException>(() => auth.UserManager);
            Assert.Throws<InvalidOperationException>(() => auth.ProviderConfigManager);
            Assert.Throws<InvalidOperationException>(() => auth.TenantManager);
        }

        [Fact]
        public void NoTenantId()
        {
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = MockCredential,
                ProjectId = "project1",
            });

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;

            Assert.Null(auth.TokenFactory.TenantId);
            Assert.Null(auth.IdTokenVerifier.TenantId);
            Assert.Null(auth.SessionCookieVerifier.TenantId);
            Assert.Null(auth.UserManager.TenantId);
        }

        [Fact]
        public void NoEmulator()
        {
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = MockCredential,
                ProjectId = "project1",
            });

            var auth = FirebaseAuth.DefaultInstance;

            Assert.False(auth.TokenFactory.IsEmulatorMode);
            Assert.False(auth.IdTokenVerifier.IsEmulatorMode);
            Assert.Null(auth.UserManager.EmulatorHost);
            Assert.Null(auth.TenantManager.EmulatorHost);
        }

        [Fact]
        public void UserManagerNoProjectId()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });

            var ex = Assert.Throws<ArgumentException>(
                () => FirebaseAuth.DefaultInstance.UserManager);

            Assert.Equal(
                "Must initialize FirebaseApp with a project ID to manage users.",
                ex.Message);
        }

        [Fact]
        public void ProviderConfigManagerNoProjectId()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });

            var ex = Assert.Throws<ArgumentException>(
                () => FirebaseAuth.DefaultInstance.ProviderConfigManager);

            Assert.Equal(
                "Must initialize FirebaseApp with a project ID to manage provider configurations.",
                ex.Message);
        }

        [Fact]
        public void TenantManagerNoProjectId()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });

            var ex = Assert.Throws<ArgumentException>(
                () => FirebaseAuth.DefaultInstance.TenantManager);

            Assert.Equal(
                "Must initialize FirebaseApp with a project ID to manage tenants.",
                ex.Message);
        }

        [Fact]
        public void ServiceAccountCredential()
        {
            var options = new AppOptions
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            };
            var app = FirebaseApp.Create(options);

            var tokenFactory = FirebaseAuth.DefaultInstance.TokenFactory;

            Assert.IsType<ServiceAccountSigner>(tokenFactory.Signer);
        }

        [Fact]
        public void ServiceAccountId()
        {
            var options = new AppOptions
            {
                Credential = MockCredential,
                ServiceAccountId = "test-service-account",
            };
            var app = FirebaseApp.Create(options);

            var tokenFactory = FirebaseAuth.DefaultInstance.TokenFactory;

            Assert.IsType<FixedAccountIAMSigner>(tokenFactory.Signer);
        }

        [Fact]
        public async void InvalidCredential()
        {
            var options = new AppOptions
            {
                Credential = MockCredential,
            };
            var app = FirebaseApp.Create(options);

            var tokenFactory = FirebaseAuth.DefaultInstance.TokenFactory;

            Assert.IsType<IAMSigner>(tokenFactory.Signer);
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("user1"));
            var errorMessage = "Failed to determine service account ID. Make sure to initialize the SDK "
                + "with service account credentials or specify a service account "
                + "ID with iam.serviceAccounts.signBlob permission. Please refer to "
                + "https://firebase.google.com/docs/auth/admin/create-custom-tokens for "
                + "more details on creating custom tokens.";
            Assert.Equal(errorMessage, ex.Message);
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}
