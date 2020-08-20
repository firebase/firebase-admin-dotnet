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
        private readonly Lazy<TenantManager> tenantManager;

        internal FirebaseAuth(Args args)
        : base(args)
        {
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
