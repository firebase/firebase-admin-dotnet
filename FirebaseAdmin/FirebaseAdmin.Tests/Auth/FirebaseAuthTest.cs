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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseAuthTest: IDisposable
    {
        private static readonly GoogleCredential mockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void GetAuthWithoutApp()
        {
            Assert.Null(FirebaseAuth.DefaultInstance);
        }

        [Fact]
        public void GetDefaultAuth()
        {
            var app = FirebaseApp.Create(new AppOptions(){Credential = mockCredential});
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            Assert.Same(auth, FirebaseAuth.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseAuth.DefaultInstance);
        }

        [Fact]
        public void GetAuth()
        {
            var app = FirebaseApp.Create(new AppOptions(){Credential = mockCredential}, "MyApp");
            FirebaseAuth auth = FirebaseAuth.GetAuth(app);
            Assert.Same(auth, FirebaseAuth.GetAuth(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseAuth.GetAuth(app));
        }

        [Fact]
        public async Task VerifyIdTokenNoProjectId()
        {
            FirebaseApp.Create(new AppOptions(){Credential = mockCredential});
            var idToken = await FirebaseTokenVerifierTest.CreateTestTokenAsync();
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken));
        }

        [Fact]
        public async Task VerifyIdTokenCancel()
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = mockCredential,
                ProjectId = "test-project",
            });
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var idToken = await FirebaseTokenVerifierTest.CreateTestTokenAsync();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(
                    idToken, canceller.Token));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}
