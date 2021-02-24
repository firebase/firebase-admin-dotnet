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

using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    internal abstract class CustomTokenVerifier
    {
        private const string ClientEmail = "client@test-project.iam.gserviceaccount.com";

        private static readonly byte[] PublicKey =
            File.ReadAllBytes("./resources/public_cert.pem");

        private readonly string issuer;
        private readonly string tenantId;

        internal CustomTokenVerifier(string issuer, string tenantId = null)
        {
            this.issuer = issuer;
            this.tenantId = tenantId;
        }

        internal static CustomTokenVerifier FromDefaultServiceAccount(string tenantId = null)
        {
            return new RSACustomTokenVerifier(ClientEmail, PublicKey, tenantId);
        }

        internal void Verify(string token, string uid, IDictionary<string, object> claims = null)
        {
            string[] segments = token.Split(".");
            Assert.Equal(3, segments.Length);

            var payload = JwtUtils.Decode<FirebaseTokenFactory.CustomTokenPayload>(segments[1]);
            Assert.Equal(this.issuer, payload.Issuer);
            Assert.Equal(this.issuer, payload.Subject);
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

            if (this.tenantId == null)
            {
                Assert.Null(payload.TenantId);
            }
            else
            {
                Assert.Equal(this.tenantId, payload.TenantId);
            }

            this.AssertSignature($"{segments[0]}.{segments[1]}", segments[2]);
        }

        protected abstract void AssertSignature(string tokenData, string signature);

        private sealed class RSACustomTokenVerifier : CustomTokenVerifier
        {
            private readonly RSA rsa;

            internal RSACustomTokenVerifier(string issuer, byte[] publicKey, string tenantId)
            : base(issuer, tenantId)
            {
                var x509cert = new X509Certificate2(publicKey);
                this.rsa = (RSA)x509cert.PublicKey.Key;
            }

            protected override void AssertSignature(string tokenData, string signature)
            {
                var tokenDataBytes = Encoding.UTF8.GetBytes(tokenData);
                var signatureBytes = JwtUtils.Base64DecodeToBytes(signature);
                var verified = this.rsa.VerifyData(
                    tokenDataBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);
                Assert.True(verified);
            }
        }
    }
}
