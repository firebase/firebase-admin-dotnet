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
using FirebaseAdmin.Auth;
using Google.Apis.Auth;
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
            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            app.Delete();
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await auth.CreateCustomTokenAsync("user"));
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await auth.VerifyIdTokenAsync("user"));
        }

        [Fact]
        public async Task CreateCustomToken()
        {
            var cred = GoogleCredential.FromFile("./resources/service_account.json");
            FirebaseApp.Create(new AppOptions() { Credential = cred });
            var token = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("user1");
            VerifyCustomToken(token, "user1", null);
        }

        [Fact]
        public async Task CreateCustomTokenWithClaims()
        {
            var cred = GoogleCredential.FromFile("./resources/service_account.json");
            FirebaseApp.Create(new AppOptions() { Credential = cred });
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };
            var token = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(
                "user2", developerClaims);
            VerifyCustomToken(token, "user2", developerClaims);
        }

        [Fact]
        public async Task CreateCustomTokenCancel()
        {
            var cred = GoogleCredential.FromFile("./resources/service_account.json");
            FirebaseApp.Create(new AppOptions() { Credential = cred });
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(
                async () => await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(
                    "user1", canceller.Token));
        }

        [Fact]
        public async Task CreateCustomTokenInvalidCredential()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("user1"));
        }

        [Fact]
        public async Task VerifyIdTokenNoProjectId()
        {
            FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            var idToken = await FirebaseTokenVerifierTest.CreateTestTokenAsync();
            await Assert.ThrowsAsync<ArgumentException>(
                async () => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken));
        }

        [Fact]
        public async Task VerifyIdTokenCancel()
        {
            FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                ProjectId = "test-project",
            });
            var canceller = new CancellationTokenSource();
            canceller.Cancel();
            var idToken = await FirebaseTokenVerifierTest.CreateTestTokenAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(
                    idToken, canceller.Token));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private static void VerifyCustomToken(string token, string uid, Dictionary<string, object> claims)
        {
            string[] segments = token.Split(".");
            Assert.Equal(3, segments.Length);

            var payload = JwtUtils.Decode<FirebaseTokenFactory.CustomTokenPayload>(segments[1]);
            Assert.Equal("client@test-project.iam.gserviceaccount.com", payload.Issuer);
            Assert.Equal("client@test-project.iam.gserviceaccount.com", payload.Subject);
            Assert.Equal(uid, payload.Uid);
            if (claims == null)
            {
                Assert.Null(payload.Claims);
            }
            else
            {
                Assert.Equal(claims.Count, payload.Claims.Count);
                foreach (var entry in claims)
                {
                    object value;
                    Assert.True(payload.Claims.TryGetValue(entry.Key, out value));
                    Assert.Equal(entry.Value, value);
                }
            }

            var x509cert = new X509Certificate2(File.ReadAllBytes("./resources/public_cert.pem"));
            var rsa = (RSA)x509cert.PublicKey.Key;
            var tokenData = Encoding.UTF8.GetBytes(segments[0] + "." + segments[1]);
            var signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            var verified = rsa.VerifyData(tokenData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.True(verified);
        }
    }
}
