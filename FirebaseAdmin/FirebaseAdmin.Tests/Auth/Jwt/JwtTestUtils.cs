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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public sealed class JwtTestUtils
    {
        internal const string ProjectId = "test-project";

        internal static readonly IClock Clock = new MockClock();

        internal static readonly IPublicKeySource DefaultKeySource = new FileSystemPublicKeySource(
            "./resources/public_cert.pem");

        internal static readonly ISigner DefaultSigner = CreateTestSigner(
            "./resources/service_account.json");

        public static AuthBuilder AuthBuilderForTokenVerification(string tenantId = null)
        {
            return new AuthBuilder
            {
                ProjectId = ProjectId,
                Clock = Clock,
                KeySource = DefaultKeySource,
                RetryOptions = RetryOptions.NoBackOff,
                TenantId = tenantId,
            };
        }

        public static MockTokenBuilder IdTokenBuilder(string tenantId = null)
        {
            return new MockTokenBuilder
            {
                ProjectId = ProjectId,
                Clock = Clock,
                Signer = JwtTestUtils.DefaultSigner,
                IssuerPrefix = "https://securetoken.google.com",
                Uid = "testuser",
                TenantId = tenantId,
            };
        }

        public static MockTokenBuilder SessionCookieBuilder(string tenantId = null)
        {
            return new MockTokenBuilder
            {
                ProjectId = ProjectId,
                Clock = Clock,
                Signer = JwtTestUtils.DefaultSigner,
                IssuerPrefix = "https://session.firebase.google.com",
                Uid = "testuser",
                TenantId = tenantId,
            };
        }

        public static void AssertRevocationCheckRequest(string tenantId, Uri uri)
        {
            var tenantInfo = tenantId != null ? $"/tenants/{tenantId}" : string.Empty;
            var expectedPath = $"/v1/projects/{ProjectId}{tenantInfo}/accounts:lookup";
            Assert.Equal(expectedPath, uri.PathAndQuery);
        }

        private static ISigner CreateTestSigner(string filePath)
        {
            var credential = GoogleCredential.FromFile(filePath);
            var serviceAccount = (ServiceAccountCredential)credential.UnderlyingCredential;
            return new ServiceAccountSigner(serviceAccount);
        }

        private sealed class FileSystemPublicKeySource : IPublicKeySource
        {
            private IReadOnlyList<PublicKey> rsa;

            public FileSystemPublicKeySource(string file)
            {
                var x509cert = new X509Certificate2(File.ReadAllBytes(file));
                var rsa = (RSA)x509cert.PublicKey.Key;
                this.rsa = ImmutableList.Create(new PublicKey("test-key-id", rsa));
            }

            public Task<IReadOnlyList<PublicKey>> GetPublicKeysAsync(
                CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return Task.FromResult(this.rsa);
            }
        }
    }
}
