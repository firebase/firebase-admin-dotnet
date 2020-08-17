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
using Google.Api.Gax;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// This is the entry point to all server-side Firebase Authentication operations. You can
    /// get an instance of this class via <c>FirebaseAuth.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseAuth : AbstractFirebaseAuth
    {
        private readonly Lazy<ProviderConfigManager> providerConfigManager;
        private readonly Lazy<TenantManager> tenantManager;

        internal FirebaseAuth(Args args)
        : base(args)
        {
            this.providerConfigManager = args.ProviderConfigManager.ThrowIfNull(
                nameof(args.ProviderConfigManager));
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
        /// Looks up an OIDC auth provider configuration by the provided ID.
        /// </summary>
        /// <returns>A task that completes with a <see cref="OidcProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain the <c>oidc.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">The ID of the OIDC provider config to return.</param>
        public async Task<OidcProviderConfig> GetOidcProviderConfigAsync(string providerId)
        {
            return await this.GetOidcProviderConfigAsync(providerId, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Looks up an OIDC auth provider configuration by the provided ID.
        /// </summary>
        /// <returns>A task that completes with a <see cref="OidcProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain the <c>oidc.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">The ID of the OIDC provider config to return.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<OidcProviderConfig> GetOidcProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return await providerConfigManager
                .GetOidcProviderConfigAsync(providerId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Looks up a SAML auth provider configuration by the provided ID.
        /// </summary>
        /// <returns>A task that completes with a <see cref="SamlProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain the <c>saml.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">The ID of the SAML provider config to return.</param>
        public async Task<SamlProviderConfig> GetSamlProviderConfigAsync(string providerId)
        {
            return await this.GetSamlProviderConfigAsync(providerId, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Looks up a SAML auth provider configuration by the provided ID.
        /// </summary>
        /// <returns>A task that completes with a <see cref="SamlProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain the <c>saml.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">The ID of the SAML provider config to return.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<SamlProviderConfig> GetSamlProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return await providerConfigManager
                .GetSamlProviderConfigAsync(providerId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new auth provider configuration.
        /// </summary>
        /// <returns>A task that completes with an <see cref="AuthProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="args"/> is null or
        /// invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while creating
        /// the provider configuration.</exception>
        /// <param name="args">Arguments that describe the new provider configuration.</param>
        /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> to create.</typeparam>
        public async Task<T> CreateProviderConfigAsync<T>(AuthProviderConfigArgs<T> args)
        where T : AuthProviderConfig
        {
            return await this.CreateProviderConfigAsync(args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new auth provider configuration.
        /// </summary>
        /// <returns>A task that completes with an <see cref="AuthProviderConfig"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="args"/> is null or
        /// invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while creating
        /// the provider configuration.</exception>
        /// <param name="args">Arguments that describe the new provider configuration.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> to create.</typeparam>
        public async Task<T> CreateProviderConfigAsync<T>(
            AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
            where T : AuthProviderConfig
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return await providerConfigManager.CreateProviderConfigAsync(args, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing auth provider configuration.
        /// </summary>
        /// <returns>A task that completes with the updated <see cref="AuthProviderConfig"/>.
        /// </returns>
        /// <exception cref="ArgumentException">If <paramref name="args"/> is null or
        /// invalid.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist or if an unexpected error occurs while performing the update.</exception>
        /// <param name="args">Properties to be updated in the provider configuration.</param>
        /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> to update.</typeparam>
        public async Task<T> UpdateProviderConfigAsync<T>(AuthProviderConfigArgs<T> args)
        where T : AuthProviderConfig
        {
            return await this.UpdateProviderConfigAsync(args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing auth provider configuration.
        /// </summary>
        /// <returns>A task that completes with the updated <see cref="AuthProviderConfig"/>.
        /// </returns>
        /// <exception cref="ArgumentException">If <paramref name="args"/> is null or
        /// invalid.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist or if an unexpected error occurs while performing the update.</exception>
        /// <param name="args">Properties to be updated in the provider configuration.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> to update.</typeparam>
        public async Task<T> UpdateProviderConfigAsync<T>(
            AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
            where T : AuthProviderConfig
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return await providerConfigManager.UpdateProviderConfigAsync(args, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the specified auth provider configuration.
        /// </summary>
        /// <returns>A task that completes when the provider configuration is deleted.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain either the <c>oidc.</c> or <c>saml.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">ID of the provider configuration to delete.</param>
        public async Task DeleteProviderConfigAsync(string providerId)
        {
            await this.DeleteProviderConfigAsync(providerId, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the specified auth provider configuration.
        /// </summary>
        /// <returns>A task that completes when the provider configuration is deleted.</returns>
        /// <exception cref="ArgumentException">If the provider ID is null, empty or does not
        /// contain either the <c>oidc.</c> or <c>saml.</c> prefix.</exception>
        /// <exception cref="FirebaseAuthException">If the specified provider config does not
        /// exist.</exception>
        /// <param name="providerId">ID of the provider configuration to delete.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task DeleteProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            await providerConfigManager
                .DeleteProviderConfigAsync(providerId, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an async enumerable to iterate or page through OIDC provider configurations
        /// starting from the specified page token. If the page token is null or unspecified,
        /// iteration starts from the first page. See
        /// <a href="https://googleapis.github.io/google-cloud-dotnet/docs/guides/page-streaming.html">
        /// Page Streaming</a> for more details on how to use this API.
        /// </summary>
        /// <param name="options">The options to control the starting point and page size. Pass
        /// null to list from the beginning with default settings.</param>
        /// <returns>A <see cref="PagedAsyncEnumerable{AuthProviderConfigs, OidcProviderConfig}"/>
        /// instance.</returns>
        public PagedAsyncEnumerable<AuthProviderConfigs<OidcProviderConfig>, OidcProviderConfig>
            ListOidcProviderConfigsAsync(ListProviderConfigsOptions options)
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return providerConfigManager.ListOidcProviderConfigsAsync(options);
        }

        /// <summary>
        /// Gets an async enumerable to iterate or page through SAML provider configurations
        /// starting from the specified page token. If the page token is null or unspecified,
        /// iteration starts from the first page. See
        /// <a href="https://googleapis.github.io/google-cloud-dotnet/docs/guides/page-streaming.html">
        /// Page Streaming</a> for more details on how to use this API.
        /// </summary>
        /// <param name="options">The options to control the starting point and page size. Pass
        /// null to list from the beginning with default settings.</param>
        /// <returns>A <see cref="PagedAsyncEnumerable{AuthProviderConfigs, SamlProviderConfig}"/>
        /// instance.</returns>
        public PagedAsyncEnumerable<AuthProviderConfigs<SamlProviderConfig>, SamlProviderConfig>
            ListSamlProviderConfigsAsync(ListProviderConfigsOptions options)
        {
            var providerConfigManager = this.IfNotDeleted(
                () => this.providerConfigManager.Value);
            return providerConfigManager.ListSamlProviderConfigsAsync(options);
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseAuth"/> service instance.
        /// </summary>
        internal override void Cleanup()
        {
            this.providerConfigManager.DisposeIfCreated();
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
            internal Lazy<ProviderConfigManager> ProviderConfigManager { get; set; }

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
