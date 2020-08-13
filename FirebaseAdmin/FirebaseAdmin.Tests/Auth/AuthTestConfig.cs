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
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Multitenancy;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    /// <summary>
    /// An abstraction for initializing an <see cref="AbstractFirebaseAuth"/> instance with
    /// specific configuration settings for testing purposes.
    /// </summary>
    public abstract class AuthTestConfig
    {
        private readonly Lazy<Context> context;

        public AuthTestConfig()
        {
            // Defer calling Init to prevent unexpected constant resolution issues.
            this.context = new Lazy<Context>(this.Init);
        }

        private protected Context Config => this.context.Value;

        private protected abstract string BaseUrl { get; }

        public AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler)
        {
            return this.CreateAuth(TestOptions.WithUserManagerHandler(handler));
        }

        public abstract AbstractFirebaseAuth CreateAuth(TestOptions options);

        /// <summary>
        /// Creates a mock ID token for testing purposes. By default the created token has an issue
        /// time 10 minutes ago, and an expirty time 50 minutes into the future. All header and
        /// payload claims can be overridden if needed.
        /// </summary>
        public async Task<string> CreateIdTokenAsync(
            Dictionary<string, object> headerOverrides = null,
            Dictionary<string, object> payloadOverrides = null)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "RS256" },
                { "typ", "jwt" },
                { "kid", "test-key-id" },
            };
            if (headerOverrides != null)
            {
                foreach (var entry in headerOverrides)
                {
                    header[entry.Key] = entry.Value;
                }
            }

            var payload = new Dictionary<string, object>()
            {
                { "sub", "testuser" },
                { "iss", $"https://securetoken.google.com/{this.Config.ProjectId}" },
                { "aud", this.Config.ProjectId },
                { "iat", this.Config.Clock.UnixTimestamp() - (60 * 10) },
                { "exp", this.Config.Clock.UnixTimestamp() + (60 * 50) },
            };
            if (this.Config.TenantId != null)
            {
                payload["firebase"] = new Dictionary<string, object>
                {
                    { "tenant", this.Config.TenantId },
                };
            }

            if (payloadOverrides != null)
            {
                foreach (var entry in payloadOverrides)
                {
                    payload[entry.Key] = entry.Value;
                }
            }

            return await JwtUtils.CreateSignedJwtAsync(header, payload, this.Config.Signer);
        }

        /// <summary>
        /// Asserts that the given <c>FirebaseToken</c> is correctly populated, and contains
        /// the expected claims.
        /// </summary>
        public void AssertFirebaseToken(
            FirebaseToken decoded, IDictionary<string, object> expected = null)
        {
            Assert.Equal(this.Config.ProjectId, decoded.Audience);
            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal("testuser", decoded.Subject);

            // The default test token created by CreateTestTokenAsync has an issue time 10 minutes
            // ago, and an expiry time 50 minutes in the future.
            Assert.Equal(
                this.Config.Clock.UnixTimestamp() - (60 * 10), decoded.IssuedAtTimeSeconds);
            Assert.Equal(
                this.Config.Clock.UnixTimestamp() + (60 * 50), decoded.ExpirationTimeSeconds);

            if (expected != null)
            {
                if (this.Config.TenantId != null)
                {
                    Assert.Contains(decoded.Claims, (kvp) => kvp.Key == "firebase");
                    Assert.Equal(expected.Count + 1, decoded.Claims.Count);
                }
                else
                {
                    Assert.Equal(expected.Count, decoded.Claims.Count);
                }

                foreach (var entry in expected)
                {
                    Assert.Equal(entry.Value, decoded.Claims[entry.Key]);
                }
            }
            else if (this.Config.TenantId != null)
            {
                Assert.Equal("firebase", Assert.Single(decoded.Claims).Key);
            }
            else
            {
                Assert.Empty(decoded.Claims);
            }

            Assert.Equal(this.Config.TenantId, decoded.TenantId);
        }

        internal void AssertRequest(
            string expectedPath, MockMessageHandler.IncomingRequest request)
        {
            Assert.Equal($"{this.BaseUrl}/{expectedPath}", request.Url.PathAndQuery);
            Assert.Equal(
                FirebaseUserManager.ClientVersion,
                request.Headers.GetValues(FirebaseUserManager.ClientVersionHeader).First());
        }

        private protected abstract Context Init();

        private protected void PopulateArgs(AbstractFirebaseAuth.Args args, TestOptions options)
        {
            if (options == null)
            {
                options = new TestOptions();
            }

            if (options.UserManagerHandler != null)
            {
                args.UserManager = new Lazy<FirebaseUserManager>(
                    this.CreateUserManager(options.UserManagerHandler));
            }

            if (options.IdTokenVerifier)
            {
                args.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    this.CreateIdTokenVerifier());
            }
        }

        private FirebaseUserManager CreateUserManager(HttpMessageHandler handler)
        {
            var args = new FirebaseUserManager.Args
            {
                Credential = this.Config.Credential,
                Clock = this.Config.Clock,
                RetryOptions = this.Config.RetryOptions,
                ProjectId = this.Config.ProjectId,
                TenantId = this.Config.TenantId,
                ClientFactory = new MockHttpClientFactory(handler),
            };
            return new FirebaseUserManager(args);
        }

        private FirebaseTokenVerifier CreateIdTokenVerifier()
        {
            return FirebaseTokenVerifier.CreateIdTokenVerifier(
                this.Config.ProjectId,
                this.Config.KeySource,
                this.Config.Clock,
                this.Config.TenantId);
        }

        /// <summary>
        /// Configuration options for an <see cref="AuthTestConfig"/> instance. Initialized once
        /// in the <see cref="Init"/> method, and possibly used across multiple test cases.
        /// </summary>
        public class Context
        {
            internal GoogleCredential Credential { get; set; }

            internal IClock Clock { get; set; }

            internal RetryOptions RetryOptions { get; set; }

            internal string ProjectId { get; set; }

            internal string TenantId { get; set; }

            internal IPublicKeySource KeySource { get; set; }

            internal ISigner Signer { get; set; }
        }

        internal abstract class AbstractFirebaseAuthTestConfig : AuthTestConfig
        {
            private protected override string BaseUrl => $"/v1/projects/{this.Config.ProjectId}";

            public override AbstractFirebaseAuth CreateAuth(TestOptions options)
            {
                var args = FirebaseAuth.Args.CreateDefault();
                this.PopulateArgs(args, options);
                return new FirebaseAuth(args);
            }
        }

        internal abstract class AbstractTenantAwareFirebaseAuthTestConfig : AuthTestConfig
        {
            private protected override string BaseUrl =>
                $"/v1/projects/{this.Config.ProjectId}/tenants/{this.Config.TenantId}";

            public override AbstractFirebaseAuth CreateAuth(TestOptions options)
            {
                var args = TenantAwareFirebaseAuth.Args.CreateDefault(this.Config.TenantId);
                this.PopulateArgs(args, options);
                return new TenantAwareFirebaseAuth(args);
            }
        }
    }
}
