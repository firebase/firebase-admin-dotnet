// Copyright 2026, Google Inc. All rights reserved.
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
using Google.Apis.Util;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// This is the entry point to all server-side Firebase App Check operations. You can get an
    /// instance of this class via <c>FirebaseAppCheck.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseAppCheck : IFirebaseService
    {
        private readonly AppCheckClient client;
        private readonly AppCheckTokenVerifier verifier;
        private volatile bool deleted;

        private FirebaseAppCheck(FirebaseApp app)
        {
            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Project ID is required to access App Check service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.");
            }

            var clientFactory = app.Options.HttpClientFactory;
            this.client = new AppCheckClient(clientFactory, app.Options.Credential, projectId);
            this.verifier = new AppCheckTokenVerifier(
                projectId,
                new AppCheckPublicKeySource(SystemClock.Default, clientFactory),
                this.client,
                SystemClock.Default);
        }

        /// <summary>
        /// Gets the App Check instance associated with the default Firebase app. This property is
        /// <c>null</c> if the default app doesn't yet exist.
        /// </summary>
        public static FirebaseAppCheck DefaultInstance
        {
            get
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    return null;
                }

                return GetAppCheck(app);
            }
        }

        /// <summary>
        /// Returns the App Check instance for the specified app.
        /// </summary>
        /// <returns>The <see cref="FirebaseAppCheck"/> instance associated with the specified
        /// app.</returns>
        /// <exception cref="System.ArgumentNullException">If the app argument is null.</exception>
        /// <param name="app">An app instance.</param>
        public static FirebaseAppCheck GetAppCheck(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }

            return app.GetOrInit<FirebaseAppCheck>(typeof(FirebaseAppCheck).Name, () =>
            {
                return new FirebaseAppCheck(app);
            });
        }

        /// <summary>
        /// Verifies a Firebase App Check token.
        /// </summary>
        /// <returns>A task that completes with a <see cref="VerifyAppCheckTokenResponse"/>.</returns>
        /// <exception cref="ArgumentException">If the token is null or empty.</exception>
        /// <exception cref="FirebaseAppCheckException">If the token is invalid or expired, or
        /// the verification fails.</exception>
        /// <param name="token">The App Check token to verify.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public Task<VerifyAppCheckTokenResponse> VerifyTokenAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            return this.VerifyTokenAsync(token, null, cancellationToken);
        }

        /// <summary>
        /// Verifies a Firebase App Check token with the given options. If
        /// <see cref="VerifyAppCheckTokenOptions.Consume"/> is true, the token is also consumed,
        /// and <see cref="VerifyAppCheckTokenResponse.AlreadyConsumed"/> indicates whether it was
        /// already used.
        /// </summary>
        /// <returns>A task that completes with a <see cref="VerifyAppCheckTokenResponse"/>.</returns>
        /// <exception cref="ArgumentException">If the token is null or empty.</exception>
        /// <exception cref="FirebaseAppCheckException">If the token is invalid or expired, or
        /// the verification fails.</exception>
        /// <param name="token">The App Check token to verify.</param>
        /// <param name="options">Options for the verification.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<VerifyAppCheckTokenResponse> VerifyTokenAsync(
            string token,
            VerifyAppCheckTokenOptions options,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.deleted)
            {
                throw new InvalidOperationException("Cannot invoke after deleting the app.");
            }

            return await this.verifier.VerifyTokenAsync(token, options, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseAppCheck"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            this.deleted = true;
            this.client.Dispose();
        }
    }
}
