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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// The tenant-aware Firebase client. This can be used to perform a variety of
    /// authentication-related operations, scoped to a particular tenant.
    /// </summary>
    public sealed class TenantAwareFirebaseAuth : AbstractFirebaseAuth
    {
        internal TenantAwareFirebaseAuth(Args args)
        : base(args)
        {
            this.TenantId = args.TenantId;
            if (string.IsNullOrEmpty(this.TenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty.");
            }
        }

        /// <summary>
        /// Gets the tenant ID associated with this instance.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Creates a new Firebase session cookie from the given ID token and options. The returned JWT can
        /// be set as a server-side session cookie with a custom cookie policy.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while creating the cookie.</exception>
        /// <param name="idToken">The Firebase ID token to exchange for a session cookie.</param>
        /// <param name="options">Additional options required to create the cookie.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with the Firebase session cookie.</returns>
        public override async Task<string> CreateSessionCookieAsync(
            string idToken, SessionCookieOptions options, CancellationToken cancellationToken)
        {
            // As a minor optimization, validate options here before calling VerifyIdToken().
            options.ThrowIfNull(nameof(options)).CopyAndValidate();
            await this.VerifyIdTokenAsync(idToken, cancellationToken);
            return await base.CreateSessionCookieAsync(idToken, options, cancellationToken)
                .ConfigureAwait(false);
        }

        internal static TenantAwareFirebaseAuth Create(FirebaseApp app, string tenantId)
        {
            var args = new Args
            {
                TenantId = tenantId,
                TokenFactory = new Lazy<FirebaseTokenFactory>(
                    () => FirebaseTokenFactory.Create(app, tenantId), true),
                IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    () => FirebaseTokenVerifier.CreateIdTokenVerifier(app, tenantId), true),
                SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(
                    () => FirebaseTokenVerifier.CreateSessionCookieVerifier(app, tenantId), true),
                UserManager = new Lazy<FirebaseUserManager>(
                    () => FirebaseUserManager.Create(app, tenantId), true),
            };
            return new TenantAwareFirebaseAuth(args);
        }

        internal new class Args : AbstractFirebaseAuth.Args
        {
            public string TenantId { get; set; }

            internal static Args CreateDefault(string tenantId)
            {
                return new Args()
                {
                    TokenFactory = new Lazy<FirebaseTokenFactory>(),
                    IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(),
                    SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(),
                    UserManager = new Lazy<FirebaseUserManager>(),
                    TenantId = tenantId,
                };
            }
        }
    }
}
