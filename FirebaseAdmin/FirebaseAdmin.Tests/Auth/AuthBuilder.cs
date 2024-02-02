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
using FirebaseAdmin.Auth.Providers;
using FirebaseAdmin.Auth.Users;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Tests
{
    /// <summary>
    /// An abstraction for creating instances of <see cref="AbstractFirebaseAuth"/> for testing.
    /// When a tenant ID is configured creates <see cref="TenantAwareFirebaseAuth"/> instances.
    /// Otherwise creates <see cref="FirebaseAuth"/> instances. Test cases can further customize
    /// the components of <c>AbstractFirebaseAuth</c> to initialize via <see cref="TestOptions"/>.
    /// </summary>
    public sealed class AuthBuilder
    {
        internal IClock Clock { get; set; }

        internal RetryOptions RetryOptions { get; set; }

        internal string ProjectId { get; set; }

        internal IPublicKeySource KeySource { get; set; }

        internal string TenantId { get; set; }

        internal string EmulatorHost { get; set; }

        internal ISigner Signer { get; set; }

        public AbstractFirebaseAuth Build(TestOptions options)
        {
            if (this.TenantId != null)
            {
                var tenantArgs = TenantAwareFirebaseAuth.Args.CreateDefault(this.TenantId);
                this.PopulateArgs(tenantArgs, options);
                return new TenantAwareFirebaseAuth(tenantArgs);
            }

            var args = FirebaseAuth.Args.CreateDefault();
            this.PopulateArgs(args, options);
            return new FirebaseAuth(args);
        }

        private void PopulateArgs(AbstractFirebaseAuth.Args args, TestOptions options)
        {
            if (options.UserManagerRequestHandler != null)
            {
                args.UserManager = new Lazy<FirebaseUserManager>(
                    () => this.CreateUserManager(options));
            }

            if (options.ProviderConfigRequestHandler != null)
            {
                args.ProviderConfigManager = new Lazy<ProviderConfigManager>(
                    () => this.CreateProviderConfigManager(options));
            }

            if (options.IdTokenVerifier)
            {
                args.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    () => this.CreateIdTokenVerifier());
            }

            if (options.SessionCookieVerifier)
            {
                if (args is FirebaseAuth.Args)
                {
                    (args as FirebaseAuth.Args).SessionCookieVerifier =
                        new Lazy<FirebaseTokenVerifier>(() => this.CreateSessionCookieVerifier());
                }
                else
                {
                    throw new InvalidOperationException(
                        $"Session cookie verification not supported on {args.GetType()}");
                }
            }

            if (options.TokenFactory)
            {
                args.TokenFactory = new Lazy<FirebaseTokenFactory>(() => this.CreateTokenFactory());
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
                EmulatorHost = this.EmulatorHost,
            };
            return new FirebaseUserManager(args);
        }

        private ProviderConfigManager CreateProviderConfigManager(TestOptions options)
        {
            var args = new ProviderConfigManager.Args
            {
                RetryOptions = this.RetryOptions,
                ProjectId = this.ProjectId,
                ClientFactory = new MockHttpClientFactory(options.ProviderConfigRequestHandler),
                TenantId = this.TenantId,
            };
            return new ProviderConfigManager(args);
        }

        private FirebaseTokenVerifier CreateIdTokenVerifier()
        {
            var args = FirebaseTokenVerifier.CreateIdTokenVerifierArgs();
            args.ProjectId = this.ProjectId;
            args.PublicKeySource = this.KeySource;
            args.Clock = this.Clock;
            args.TenantId = this.TenantId;
            args.IsEmulatorMode = !string.IsNullOrWhiteSpace(this.EmulatorHost);
            return new FirebaseTokenVerifier(args);
        }

        private FirebaseTokenVerifier CreateSessionCookieVerifier()
        {
            return FirebaseTokenVerifier.CreateSessionCookieVerifier(
                this.ProjectId, this.KeySource, this.Clock);
        }

        private FirebaseTokenFactory CreateTokenFactory()
        {
            var args = new FirebaseTokenFactory.Args
            {
                Signer = this.Signer,
                Clock = this.Clock,
                TenantId = this.TenantId,
                IsEmulatorMode = !string.IsNullOrWhiteSpace(this.EmulatorHost),
            };
            return new FirebaseTokenFactory(args);
        }
    }
}
