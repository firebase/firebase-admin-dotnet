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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
        public async Task UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            var auth = FirebaseAuth.DefaultInstance;

            app.Delete();

            Assert.Throws<InvalidOperationException>(() => auth.TokenFactory);
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await auth.VerifyIdTokenAsync("user"));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await auth.SetCustomUserClaimsAsync("user", null));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await auth.GetOidcProviderConfigAsync("oidc.provider"));
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
            Assert.Null(auth.UserManager.TenantId);
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
        public async Task ImportUsersPasswordNoHash()
        {
            var args = new ImportUserRecordArgs()
            {
                Uid = "123",
                Email = "example@gmail.com",
                PasswordSalt = Encoding.ASCII.GetBytes("abc"),
                PasswordHash = Encoding.ASCII.GetBytes("def"),
            };

            var usersLst = new List<ImportUserRecordArgs>()
            {
                args,
            };
            await Assert.ThrowsAsync<NullReferenceException>(
                async () => await FirebaseAuth.DefaultInstance.ImportUsersAsync(usersLst));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}
