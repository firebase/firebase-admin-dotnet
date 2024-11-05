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

        internal const string DefaultClientEmail = "client@test-project.iam.gserviceaccount.com";

        internal static readonly IClock Clock = new MockClock();

        internal static readonly byte[] DefaultPublicKey = File.ReadAllBytes(
            "./resources/public_cert.pem");

        internal static readonly IPublicKeySource DefaultKeySource = new ByteArrayPublicKeySource(
            DefaultPublicKey);

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
            AssertRevocationCheckRequest(tenantId, null, uri);
        }

        public static void AssertRevocationCheckRequest(string tenantId, string emulatorHost, Uri uri)
        {
            var options = new Utils.UrlOptions
            {
                TenantId = tenantId,
                EmulatorHost = emulatorHost,
            };
            var expectedUrl = $"{Utils.BuildAuthUrl(ProjectId, options)}/accounts:lookup";
            Assert.Equal(expectedUrl, uri.ToString());
        }

        internal static void AssertRequest(MockMessageHandler.IncomingRequest request)
        {
            Assert.Contains(HttpUtils.GetMetricsHeader(), request.Headers.GetValues("X-Goog-Api-Client"));
        }

        private static ISigner CreateTestSigner(string filePath)
        {
            var credential = GoogleCredential.FromFile(filePath);
            return new ServiceAccountSigner(credential.ToServiceAccountCredential());
        }

        private sealed class ByteArrayPublicKeySource : IPublicKeySource
        {
            private IReadOnlyList<PublicKey> rsa;

            public ByteArrayPublicKeySource(byte[] publicKey)
            {
                var x509cert = new X509Certificate2(publicKey);
                var rsa = (RSA)x509cert.GetRSAPublicKey();
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
