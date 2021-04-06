// Copyright 2021, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseAuthEmulatorTest : IDisposable
    {
        private const string AuthEmulatorHost = "FIREBASE_AUTH_EMULATOR_HOST";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        public FirebaseAuthEmulatorTest()
        {
            Environment.SetEnvironmentVariable(AuthEmulatorHost, "localhost:9090");
        }

        [Fact]
        public void DefaultFirebaseAuth()
        {
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = MockCredential,
                ProjectId = "project1",
            });

            var auth = FirebaseAuth.DefaultInstance;

            Assert.Equal("localhost:9090", auth.UserManager.EmulatorHost);
            Assert.True(auth.IdTokenVerifier.IsEmulatorMode);
        }

        [Fact]
        public void TenantAwareFirebseAuth()
        {
            var app = FirebaseApp.Create(new AppOptions
            {
                Credential = MockCredential,
                ProjectId = "project1",
            });

            var auth = FirebaseAuth.DefaultInstance.TenantManager.AuthForTenant("tenant1");

            Assert.Equal("localhost:9090", auth.UserManager.EmulatorHost);
            Assert.True(auth.IdTokenVerifier.IsEmulatorMode);
        }

        public virtual void Dispose()
        {
            FirebaseApp.DeleteAll();
            Environment.SetEnvironmentVariable(AuthEmulatorHost, string.Empty);
        }
    }
}
