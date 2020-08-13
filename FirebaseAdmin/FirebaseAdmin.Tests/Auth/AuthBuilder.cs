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
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Multitenancy;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Tests
{
    public sealed class AuthBuilder
    {
        internal IClock Clock { get; set; }

        internal RetryOptions RetryOptions { get; set; }

        internal string ProjectId { get; set; }

        internal IPublicKeySource KeySource { get; set; }

        internal string TenantId { get; set; }

        public AbstractFirebaseAuth Build(TestOptions options)
        {
            if (this.TenantId != null)
            {
                var args = TenantAwareFirebaseAuth.Args.CreateDefault(this.TenantId);
                this.PopulateArgs(args, options);
                return new TenantAwareFirebaseAuth(args);
            }
            else
            {
                var args = FirebaseAuth.Args.CreateDefault();
                this.PopulateArgs(args, options);
                return new FirebaseAuth(args);
            }
        }

        public string BuildRequestPath(string prefix, string suffix)
        {
            var tenantInfo = this.TenantId != null ? $"/tenants/{this.TenantId}" : string.Empty;
            return $"/{prefix}/projects/{this.ProjectId}{tenantInfo}/{suffix}";
        }

        private void PopulateArgs(AbstractFirebaseAuth.Args args, TestOptions options)
        {
            if (options.UserManagerRequestHandler != null)
            {
                args.UserManager = new Lazy<FirebaseUserManager>(
                    this.CreateUserManager(options));
            }

            if (options.IdTokenVerifier)
            {
                args.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    this.CreateIdTokenVerifier());
            }
        }

        private FirebaseUserManager CreateUserManager(TestOptions options)
        {
            var args = new FirebaseUserManager.Args
            {
                Clock = this.Clock,
                RetryOptions = this.RetryOptions,
                ProjectId = this.ProjectId,
                ClientFactory = new MockHttpClientFactory(options.UserManagerRequestHandler),
                TenantId = this.TenantId,
            };
            return new FirebaseUserManager(args);
        }

        private FirebaseTokenVerifier CreateIdTokenVerifier()
        {
            return FirebaseTokenVerifier.CreateIdTokenVerifier(
                this.ProjectId, this.KeySource, this.Clock, this.TenantId);
        }
    }
}
