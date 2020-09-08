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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Multitenancy;
using FirebaseAdmin.Auth.Providers;
using FirebaseAdmin.Auth.Users;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// This is the entry point to all server-side Firebase Authentication operations. You can
    /// get an instance of this class via <c>FirebaseAuth.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseAuth : AbstractFirebaseAuth
    {
        private readonly Lazy<FirebaseTokenVerifier> sessionCookieVerifier;
        private readonly Lazy<TenantManager> tenantManager;

        internal FirebaseAuth(Args args)
        : base(args)
        {
            this.sessionCookieVerifier = args.SessionCookieVerifier.ThrowIfNull(
                nameof(args.SessionCookieVerifier));
            this.tenantManager = args.TenantManager.ThrowIfNull(nameof(args.TenantManager));
        }

        /// <summary>
        /// Gets the auth instance associated with the default Firebase app. This property is
        /// <c>null</c> if the default app doesn't yet exist.
        /// </summary>
        public static FirebaseAuth DefaultInstance
        {
            get
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    return null;
                }

                return GetAuth(app);
            }
        }

        /// <summary>
        /// Gets the <see cref="TenantManager"/> instance associated with the current project.
        /// </summary>
        public TenantManager TenantManager => this.IfNotDeleted(() => this.tenantManager.Value);

        internal FirebaseTokenVerifier SessionCookieVerifier =>
            this.IfNotDeleted(() => this.sessionCookieVerifier.Value);

        /// <summary>
        /// Returns the auth instance for the specified app.
        /// </summary>
        /// <returns>The <see cref="FirebaseAuth"/> instance associated with the specified
        /// app.</returns>
        /// <exception cref="System.ArgumentNullException">If the app argument is null.</exception>
        /// <param name="app">An app instance.</param>
        public static FirebaseAuth GetAuth(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }

            return app.GetOrInit<FirebaseAuth>(typeof(FirebaseAuth).Name, () =>
            {
                return FirebaseAuth.Create(app);
            });
        }

        /// <summary>
        /// Creates a new Firebase session cookie from the given ID token and options. The returned JWT can
        /// be set as a server-side session cookie with a custom cookie policy.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while creating the cookie.</exception>
        /// <param name="idToken">The Firebase ID token to exchange for a session cookie.</param>
        /// <param name="options">Additional options required to create the cookie.</param>
        /// <returns>A task that completes with the Firebase session cookie.</returns>
        public async Task<string> CreateSessionCookieAsync(
            string idToken, SessionCookieOptions options)
        {
            return await this.CreateSessionCookieAsync(idToken, options, default(CancellationToken))
                .ConfigureAwait(false);
        }

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
        public async Task<string> CreateSessionCookieAsync(
            string idToken, SessionCookieOptions options, CancellationToken cancellationToken)
        {
            return await this.UserManager
                .CreateSessionCookieAsync(idToken, options, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase session cookie.
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/manage-cookies">Manage
        /// Session Cookies</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded session cookie.</returns>
        /// <exception cref="ArgumentException">If the session cookie is null or
        /// empty.</exception>
        /// <exception cref="FirebaseAuthException">If the session cookie is invalid.</exception>
        /// <param name="sessionCookie">A Firebase session cookie string to verify and
        /// parse.</param>
        public async Task<FirebaseToken> VerifySessionCookieAsync(string sessionCookie)
        {
            return await this.VerifySessionCookieAsync(sessionCookie, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase session cookie.
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/manage-cookies">Manage
        /// Session Cookies</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded session cookie.</returns>
        /// <exception cref="ArgumentException">If the session cookie is null or
        /// empty.</exception>
        /// <exception cref="FirebaseAuthException">If the session cookie is invalid.</exception>
        /// <param name="sessionCookie">A Firebase session cookie string to verify and
        /// parse.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<FirebaseToken> VerifySessionCookieAsync(
            string sessionCookie, CancellationToken cancellationToken)
        {
            return await this.VerifySessionCookieAsync(sessionCookie, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase session cookie.
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/manage-cookies">Manage
        /// Session Cookies</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded session cookie.</returns>
        /// <exception cref="ArgumentException">If the session cookie is null or
        /// empty.</exception>
        /// <exception cref="FirebaseAuthException">If the session cookie is invalid.</exception>
        /// <param name="sessionCookie">A Firebase session cookie string to verify and
        /// parse.</param>
        /// <param name="checkRevoked">A boolean indicating whether to check if the tokens were
        /// revoked.</param>
        public async Task<FirebaseToken> VerifySessionCookieAsync(
            string sessionCookie, bool checkRevoked)
        {
            return await this
                .VerifySessionCookieAsync(sessionCookie, checkRevoked, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase session cookie.
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/manage-cookies">Manage
        /// Session Cookies</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded session cookie.</returns>
        /// <exception cref="ArgumentException">If the session cookie is null or
        /// empty.</exception>
        /// <exception cref="FirebaseAuthException">If the session cookie is invalid.</exception>
        /// <param name="sessionCookie">A Firebase session cookie string to verify and
        /// parse.</param>
        /// <param name="checkRevoked">A boolean indicating whether to check if the tokens were
        /// revoked.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<FirebaseToken> VerifySessionCookieAsync(
            string sessionCookie, bool checkRevoked, CancellationToken cancellationToken)
        {
            var decodedToken = await this.SessionCookieVerifier
                .VerifyTokenAsync(sessionCookie, cancellationToken)
                .ConfigureAwait(false);
            if (checkRevoked)
            {
                var revoked = await this.IsRevokedAsync(decodedToken, cancellationToken)
                    .ConfigureAwait(false);
                if (revoked)
                {
                    throw new FirebaseAuthException(
                        ErrorCode.InvalidArgument,
                        "Firebase session cookie has been revoked.",
                        AuthErrorCode.RevokedSessionCookie);
                }
            }

            return decodedToken;
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseAuth"/> service instance.
        /// </summary>
        internal override void Cleanup()
        {
            this.tenantManager.DisposeIfCreated();
        }

        private static FirebaseAuth Create(FirebaseApp app)
        {
            var args = new Args
            {
                TokenFactory = new Lazy<FirebaseTokenFactory>(
                    () => FirebaseTokenFactory.Create(app), true),
                IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    () => FirebaseTokenVerifier.CreateIdTokenVerifier(app), true),
                SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(
                    () => FirebaseTokenVerifier.CreateSessionCookieVerifier(app), true),
                UserManager = new Lazy<FirebaseUserManager>(
                    () => FirebaseUserManager.Create(app), true),
                ProviderConfigManager = new Lazy<ProviderConfigManager>(
                    () => Providers.ProviderConfigManager.Create(app), true),
                TenantManager = new Lazy<TenantManager>(
                    () => Multitenancy.TenantManager.Create(app), true),
            };
            return new FirebaseAuth(args);
        }

        internal sealed new class Args : AbstractFirebaseAuth.Args
        {
            internal Lazy<FirebaseTokenVerifier> SessionCookieVerifier { get; set; }

            internal Lazy<TenantManager> TenantManager { get; set; }

            internal static Args CreateDefault()
            {
                return new Args()
                {
                    TokenFactory = new Lazy<FirebaseTokenFactory>(),
                    IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(),
                    SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(),
                    UserManager = new Lazy<FirebaseUserManager>(),
                    ProviderConfigManager = new Lazy<ProviderConfigManager>(),
                    TenantManager = new Lazy<TenantManager>(),
                };
            }
        }
    }
}
