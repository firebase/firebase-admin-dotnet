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
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class ServiceAccountSignerTest
    {
        [Fact]
        public async Task Signer()
        {
            var credential = GoogleCredential.FromFile("./resources/service_account.json");
            var serviceAccount = (ServiceAccountCredential)credential.UnderlyingCredential;
            var signer = new ServiceAccountSigner(serviceAccount);
            Assert.Equal(
                "client@test-project.iam.gserviceaccount.com", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            byte[] signature = signer.SignDataAsync(data).Result;
            Assert.True(this.Verify(data, signature));
        }

        [Fact]
        public void NullCredential()
        {
            Assert.Throws<ArgumentNullException>(() => { new ServiceAccountSigner(null); });
        }

        private bool Verify(byte[] data, byte[] signature)
        {
            var x509cert = new X509Certificate2(File.ReadAllBytes("./resources/public_cert.pem"));
            var rsa = (RSA)x509cert.PublicKey.Key;
            return rsa.VerifyData(
                data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
    }
}
