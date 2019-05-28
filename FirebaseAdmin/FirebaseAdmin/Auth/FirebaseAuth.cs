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
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Api.Gax.Rest;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// This is the entry point to all server-side Firebase Authentication operations. You can
    /// get an instance of this class via <c>FirebaseAuth.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseAuth : IFirebaseService
    {
        private readonly FirebaseApp app;
        private readonly Lazy<FirebaseTokenFactory> tokenFactory;
        private readonly Lazy<FirebaseTokenVerifier> idTokenVerifier;
        private readonly Lazy<FirebaseUserManager> userManager;
        private readonly object authLock = new object();
        private bool deleted;

        private FirebaseAuth(FirebaseApp app)
        {
            this.app = app;
            this.tokenFactory = new Lazy<FirebaseTokenFactory>(
                () => FirebaseTokenFactory.Create(this.app), true);
            this.idTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                () => FirebaseTokenVerifier.CreateIDTokenVerifier(this.app), true);
            this.userManager = new Lazy<FirebaseUserManager>(
                () => FirebaseUserManager.Create(this.app), true);
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
                return new FirebaseAuth(app);
            });
        }

        /// <summary>
        /// Creates a Firebase custom token for the given user ID. This token can then be sent
        /// back to a client application to be used with the
        /// <a href="https://firebase.google.com/docs/auth/admin/create-custom-tokens#sign_in_using_custom_tokens_on_clients">
        /// signInWithCustomToken</a> authentication API.
        /// <para>
        /// This method attempts to generate a token using:
        /// <list type="number">
        /// <item>
        /// <description>the private key of <see cref="FirebaseApp"/>'s service account
        /// credentials, if provided at initialization.</description>
        /// </item>
        /// <item>
        /// <description>the IAM service if a service accound ID was specified via
        /// <see cref="AppOptions"/></description>
        /// </item>
        /// <item>
        /// <description>the local metadata server if the code is deployed in a GCP-managed
        /// environment.</description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <returns>A task that completes with a Firebase custom token.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters.</exception>
        /// <exception cref="FirebaseException">If an error occurs while creating a custom
        /// token.</exception>
        /// <param name="uid">The UID to store in the token. This identifies the user to other
        /// Firebase services (Realtime Database, Firebase Auth, etc.). Must not be longer than
        /// 128 characters.</param>
        public async Task<string> CreateCustomTokenAsync(string uid)
        {
            return await this.CreateCustomTokenAsync(uid, default(CancellationToken));
        }

        /// <summary>
        /// Creates a Firebase custom token for the given user ID. This token can then be sent
        /// back to a client application to be used with the
        /// <a href="https://firebase.google.com/docs/auth/admin/create-custom-tokens#sign_in_using_custom_tokens_on_clients">
        /// signInWithCustomToken</a> authentication API.
        /// <para>
        /// This method attempts to generate a token using:
        /// <list type="number">
        /// <item>
        /// <description>the private key of <see cref="FirebaseApp"/>'s service account
        /// credentials, if provided at initialization.</description>
        /// </item>
        /// <item>
        /// <description>the IAM service if a service accound ID was specified via
        /// <see cref="AppOptions"/></description>
        /// </item>
        /// <item>
        /// <description>the local metadata server if the code is deployed in a GCP-managed
        /// environment.</description>
        /// </item>
        /// </list>
        /// </para>
        /// </summary>
        /// <returns>A task that completes with a Firebase custom token.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters.</exception>
        /// <exception cref="FirebaseException">If an error occurs while creating a custom
        /// token.</exception>
        /// <param name="uid">The UID to store in the token. This identifies the user to other
        /// Firebase services (Realtime Database, Firebase Auth, etc.). Must not be longer than
        /// 128 characters.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> CreateCustomTokenAsync(
            string uid, CancellationToken cancellationToken)
        {
            return await this.CreateCustomTokenAsync(uid, null, cancellationToken);
        }

        /// <summary>
        /// Creates a Firebase custom token for the given user ID containing the specified
        /// additional claims. This token can then be sent back to a client application to be used
        /// with the
        /// <a href="https://firebase.google.com/docs/auth/admin/create-custom-tokens#sign_in_using_custom_tokens_on_clients">
        /// signInWithCustomToken</a> authentication API.
        /// <para>This method uses the same mechanisms as
        /// <see cref="CreateCustomTokenAsync(string)"/> to sign custom tokens.</para>
        /// </summary>
        /// <returns>A task that completes with a Firebase custom token.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters. Or, if <paramref name="developerClaims"/> contains any standard
        /// JWT claims.</exception>
        /// <exception cref="FirebaseException">If an error occurs while creating a custom
        /// token.</exception>
        /// <param name="uid">The UID to store in the token. This identifies the user to other
        /// Firebase services (Realtime Database, Firebase Auth, etc.). Must not be longer than
        /// 128 characters.</param>
        /// <param name="developerClaims">Additional claims to be stored in the token, and made
        /// available to Firebase security rules. These must be serializable to JSON, and must not
        /// contain any standard JWT claims.</param>
        public async Task<string> CreateCustomTokenAsync(
            string uid, IDictionary<string, object> developerClaims)
        {
            return await this.CreateCustomTokenAsync(uid, developerClaims, default(CancellationToken));
        }

        /// <summary>
        /// Creates a Firebase custom token for the given user ID containing the specified
        /// additional claims. This token can then be sent back to a client application to be used
        /// with the
        /// <a href="https://firebase.google.com/docs/auth/admin/create-custom-tokens#sign_in_using_custom_tokens_on_clients">
        /// signInWithCustomToken</a> authentication API.
        /// <para>This method uses the same mechanisms as
        /// <see cref="CreateCustomTokenAsync(string)"/> to sign custom tokens.</para>
        /// </summary>
        /// <returns>A task that completes with a Firebase custom token.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters. Or, if <paramref name="developerClaims"/> contains any standard
        /// JWT claims.</exception>
        /// <exception cref="FirebaseException">If an error occurs while creating a custom
        /// token.</exception>
        /// <param name="uid">The UID to store in the token. This identifies the user to other
        /// Firebase services (Realtime Database, Firebase Auth, etc.). Must not be longer than
        /// 128 characters.</param>
        /// <param name="developerClaims">Additional claims to be stored in the token, and made
        /// available to Firebase security rules. These must be serializable to JSON, and must not
        /// contain any standard JWT claims.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> CreateCustomTokenAsync(
            string uid,
            IDictionary<string, object> developerClaims,
            CancellationToken cancellationToken)
        {
            var tokenFactory = this.IfNotDeleted(() => this.tokenFactory.Value);
            return await tokenFactory.CreateCustomTokenAsync(
                uid, developerClaims, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase ID token.
        /// <para>A Firebase client app can identify itself to a trusted back-end server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The back-end server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            return await this.VerifyIdTokenAsync(idToken, default(CancellationToken));
        }

        /// <summary>
        /// Parses and verifies a Firebase ID token.
        /// <para>A Firebase client app can identify itself to a trusted back-end server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The back-end server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(
            string idToken, CancellationToken cancellationToken)
        {
            var idTokenVerifier = this.IfNotDeleted(() => this.idTokenVerifier.Value);
            return await idTokenVerifier.VerifyTokenAsync(idToken, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containig information about the user who's
        /// user ID was specified in <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">The user ID for the user who's data is to be retrieved.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified user ID.</returns>
        /// <exception cref="ArgumentException">If user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseException">If a user cannot be found with the specified user ID.</exception>
        public async Task<UserRecord> GetUserAsync(string uid)
        {
            return await this.GetUserAsync(uid, default(CancellationToken));
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containig information about the user who's
        /// user ID was specified in <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">The user ID for the user who's data is to be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified user ID.</returns>
        /// <exception cref="ArgumentException">If user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseException">If a user cannot be found with the specified user ID.</exception>
        public async Task<UserRecord> GetUserAsync(
            string uid, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return await userManager.GetUserById(uid, cancellationToken);
        }

        /// <summary>
        /// Sets the specified custom claims on an existing user account. A null claims value
        /// removes any claims currently set on the user account. The claims must serialize into
        /// a valid JSON string. The serialized claims must not be larger than 1000 characters.
        /// </summary>
        /// <returns>A task that completes when the claims have been set.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters. Or, if the serialized <paramref name="claims"/> is larger than 1000
        /// characters.</exception>
        /// <param name="uid">The user ID string for the custom claims will be set. Must not be null
        /// or longer than 128 characters.
        /// </param>
        /// <param name="claims">The claims to be stored on the user account, and made
        /// available to Firebase security rules. These must be serializable to JSON, and the
        /// serialized claims should not be larger than 1000 characters.</param>
        public async Task SetCustomUserClaimsAsync(
            string uid, IReadOnlyDictionary<string, object> claims)
        {
            await this.SetCustomUserClaimsAsync(uid, claims, default(CancellationToken));
        }

        /// <summary>
        /// Sets the specified custom claims on an existing user account. A null claims value
        /// removes any claims currently set on the user account. The claims should serialize into
        /// a valid JSON string. The serialized claims must not be larger than 1000 characters.
        /// </summary>
        /// <returns>A task that completes when the claims have been set.</returns>
        /// <exception cref="ArgumentException">If <paramref name="uid"/> is null, empty or longer
        /// than 128 characters. Or, if the serialized <paramref name="claims"/> is larger than 1000
        /// characters.</exception>
        /// <param name="uid">The user ID string for the custom claims will be set. Must not be null
        /// or longer than 128 characters.
        /// </param>
        /// <param name="claims">The claims to be stored on the user account, and made
        /// available to Firebase security rules. These must be serializable to JSON, and after
        /// serialization it should not be larger than 1000 characters.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task SetCustomUserClaimsAsync(
            string uid, IReadOnlyDictionary<string, object> claims, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var user = new UserRecord(uid)
            {
                CustomClaims = claims,
            };

            await userManager.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an async enumerable to page users starting from the specified pageToken. If the pageToken is empty, it starts from the first page.
        /// </summary>
        /// <param name="requestOptions">The options for the next remote call.</param>
        /// <returns>A <see cref="PagedAsyncEnumerable{DownloadAccountResponse, UserRecord}"/> instance.</returns>
        public PagedAsyncEnumerable<ExportedUserRecords, ExportedUserRecord> ListUsersAsync(ListUsersOptions requestOptions)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            var restPagedAsyncEnumerable = new RestPagedAsyncEnumerable<ListUsersRequest, ExportedUserRecords, ExportedUserRecord>(
                () => userManager.CreateUserRecordServiceRequest(requestOptions),
                new ListUsersPageManager());

            return restPagedAsyncEnumerable;
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseAuth"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            lock (this.authLock)
            {
                this.deleted = true;
                this.tokenFactory.DisposeIfCreated();
                this.userManager.DisposeIfCreated();
            }
        }

        private TResult IfNotDeleted<TResult>(Func<TResult> func)
        {
            lock (this.authLock)
            {
                if (this.deleted)
                {
                    throw new InvalidOperationException("Cannot invoke after deleting the app.");
                }

                return func();
            }
        }
    }
}
