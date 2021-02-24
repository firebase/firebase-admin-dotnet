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
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class FirebaseTokenVerifierTest : IDisposable
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void NoProjectId()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
            });
            Assert.Throws<ArgumentException>(() => FirebaseTokenVerifier.CreateIdTokenVerifier(app));
            Assert.Throws<ArgumentException>(() => FirebaseTokenVerifier.CreateSessionCookieVerifier(app));
        }

        [Fact]
        public void ProjectIdFromOptions()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                ProjectId = "explicit-project-id",
            });
            var verifier = FirebaseTokenVerifier.CreateIdTokenVerifier(app);
            Assert.Equal("explicit-project-id", verifier.ProjectId);

            verifier = FirebaseTokenVerifier.CreateSessionCookieVerifier(app);
            Assert.Equal("explicit-project-id", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromServiceAccount()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            });
            var verifier = FirebaseTokenVerifier.CreateIdTokenVerifier(app);
            Assert.Equal("test-project", verifier.ProjectId);

            verifier = FirebaseTokenVerifier.CreateSessionCookieVerifier(app);
            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromEnvironment()
        {
            Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", "env-project-id");
            try
            {
                var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = MockCredential,
                });
                var verifier = FirebaseTokenVerifier.CreateIdTokenVerifier(app);
                Assert.Equal("env-project-id", verifier.ProjectId);

                verifier = FirebaseTokenVerifier.CreateSessionCookieVerifier(app);
                Assert.Equal("env-project-id", verifier.ProjectId);
            }
            finally
            {
                Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", string.Empty);
            }
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidProjectId(string projectId)
        {
            var args = FullyPopulatedArgs();
            args.ProjectId = projectId;

            Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));
        }

        [Fact]
        public void NullKeySource()
        {
            var args = FullyPopulatedArgs();
            args.PublicKeySource = null;

            Assert.Throws<ArgumentNullException>(() => new FirebaseTokenVerifier(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidShortName(string shortName)
        {
            var args = FullyPopulatedArgs();
            args.ShortName = shortName;

            Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidIssuer(string issuer)
        {
            var args = FullyPopulatedArgs();
            args.Issuer = issuer;

            Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidOperation(string operation)
        {
            var args = FullyPopulatedArgs();
            args.Operation = operation;

            Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidUrl(string url)
        {
            var args = FullyPopulatedArgs();
            args.Url = url;

            Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));
        }

        [Fact]
        public void ProjectId()
        {
            var args = FullyPopulatedArgs();

            var verifier = new FirebaseTokenVerifier(args);

            Assert.Equal("test-project", verifier.ProjectId);
            Assert.Null(verifier.TenantId);
        }

        [Fact]
        public void TenantId()
        {
            var args = FullyPopulatedArgs();
            args.TenantId = "test-tenant";

            var verifier = new FirebaseTokenVerifier(args);

            Assert.Equal("test-project", verifier.ProjectId);
            Assert.Equal("test-tenant", verifier.TenantId);
        }

        [Fact]
        public void EmptyTenantId()
        {
            var args = FullyPopulatedArgs();
            args.TenantId = string.Empty;

            var ex = Assert.Throws<ArgumentException>(() => new FirebaseTokenVerifier(args));

            Assert.Equal("Tenant ID must not be empty.", ex.Message);
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private static FirebaseTokenVerifierArgs FullyPopulatedArgs()
        {
            return new FirebaseTokenVerifierArgs
            {
                ProjectId = "test-project",
                ShortName = "short name",
                Operation = "VerifyToken()",
                Url = "https://firebase.google.com",
                Issuer = "https://firebase.google.com/",
                PublicKeySource = JwtTestUtils.DefaultKeySource,
            };
        }
    }
}
