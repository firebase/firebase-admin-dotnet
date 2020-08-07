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
        private readonly Lazy<FirebaseTokenVerifier> idTokenVerifier;
        private readonly Lazy<FirebaseTokenVerifier> sessionCookieVerifier;
        private readonly Lazy<FirebaseUserManager> userManager;
        private readonly Lazy<ProviderConfigManager> providerConfigManager;
        private readonly Lazy<TenantManager> tenantManager;

        internal FirebaseAuth(Args args)
        : base(args)
        {
            this.idTokenVerifier = args.IdTokenVerifier.ThrowIfNull(nameof(args.IdTokenVerifier));
            this.sessionCookieVerifier = args.SessionCookieVerifier.ThrowIfNull(
                nameof(args.SessionCookieVerifier));
            this.userManager = args.UserManager.ThrowIfNull(nameof(args.UserManager));
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
        /// Parses and verifies a Firebase ID token.
        /// <para>A Firebase client app can identify itself to a trusted backend server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The backend server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken)
        {
            return await this.VerifyIdTokenAsync(idToken, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase ID token.
        /// <para>A Firebase client app can identify itself to a trusted backend server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The backend server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(
            string idToken, CancellationToken cancellationToken)
        {
            return await this.VerifyIdTokenAsync(idToken, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase ID token.
        ///
        /// <para>A Firebase client app can identify itself to a trusted backend server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The backend server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        ///
        /// <para>If <c>checkRevoked</c> is set to true, this method performs an additional check
        /// to see if the ID token has been revoked since it was issued. This requires making an
        /// additional remote API call.</para>
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        /// <param name="checkRevoked">A boolean indicating whether to check if the tokens were revoked.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(string idToken, bool checkRevoked)
        {
            return await this.VerifyIdTokenAsync(idToken, checkRevoked, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Parses and verifies a Firebase ID token.
        ///
        /// <para>A Firebase client app can identify itself to a trusted backend server by sending
        /// its Firebase ID Token (accessible via the <c>getIdToken()</c> API in the Firebase
        /// client SDK) with its requests. The backend server can then use this method
        /// to verify that the token is valid. This method ensures that the token is correctly
        /// signed, has not expired, and it was issued against the Firebase project associated with
        /// this <c>FirebaseAuth</c> instance.</para>
        ///
        /// <para>If <c>checkRevoked</c> is set to true, this method performs an additional check
        /// to see if the ID token has been revoked since it was issued. This requires making an
        /// additional remote API call.</para>
        ///
        /// <para>See <a href="https://firebase.google.com/docs/auth/admin/verify-id-tokens">Verify
        /// ID Tokens</a> for code samples and detailed documentation.</para>
        /// </summary>
        /// <returns>A task that completes with a <see cref="FirebaseToken"/> representing
        /// the verified and decoded ID token.</returns>
        /// <exception cref="ArgumentException">If ID token argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If the ID token fails to verify.</exception>
        /// <param name="idToken">A Firebase ID token string to parse and verify.</param>
        /// <param name="checkRevoked">A boolean indicating whether to check if the tokens were revoked.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<FirebaseToken> VerifyIdTokenAsync(
            string idToken, bool checkRevoked, CancellationToken cancellationToken)
        {
            var idTokenVerifier = this.IfNotDeleted(() => this.idTokenVerifier.Value);
            var decodedToken = await idTokenVerifier.VerifyTokenAsync(idToken, cancellationToken)
                .ConfigureAwait(false);
            if (checkRevoked)
            {
                var revoked = await this.IsRevokedAsync(decodedToken, cancellationToken);
                if (revoked)
                {
                    throw new FirebaseAuthException(
                        ErrorCode.InvalidArgument,
                        "Firebase ID token has been revoked.",
                        AuthErrorCode.RevokedIdToken);
                }
            }

            return decodedToken;
        }

        /// <summary>
        /// Creates a new user account with the attributes contained in the specified <see cref="UserRecordArgs"/>.
        /// </summary>
        /// <param name="args">Attributes to add to the new user account.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// the newly created user account.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.</exception>
        /// <exception cref="ArgumentException">If any of the values in <paramref name="args"/> are invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while creating the user account.</exception>
        public async Task<UserRecord> CreateUserAsync(UserRecordArgs args)
        {
            return await this.CreateUserAsync(args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new user account with the attributes contained in the specified <see cref="UserRecordArgs"/>.
        /// </summary>
        /// <param name="args">Attributes to add to the new user account.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// the newly created user account.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.</exception>
        /// <exception cref="ArgumentException">If any of the values in <paramref name="args"/> are invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while creating the user account.</exception>
        public async Task<UserRecord> CreateUserAsync(
            UserRecordArgs args, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var uid = await userManager.CreateUserAsync(args, cancellationToken)
                .ConfigureAwait(false);
            return await userManager.GetUserByIdAsync(uid, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user who's
        /// user ID was specified in <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">The user ID for the user who's data is to be retrieved.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified user ID.</returns>
        /// <exception cref="ArgumentException">If user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified user ID.</exception>
        public async Task<UserRecord> GetUserAsync(string uid)
        {
            return await this.GetUserAsync(uid, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user who's
        /// user ID was specified in <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">The user ID for the user who's data is to be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified user ID.</returns>
        /// <exception cref="ArgumentException">If user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified user ID.</exception>
        public async Task<UserRecord> GetUserAsync(
            string uid, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return await userManager.GetUserByIdAsync(uid, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user identified by
        /// <paramref name="email"/>.
        /// </summary>
        /// <param name="email">The email of the user who's data is to be retrieved.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified email.</returns>
        /// <exception cref="ArgumentException">If the email argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified email.</exception>
        public async Task<UserRecord> GetUserByEmailAsync(string email)
        {
            return await this.GetUserByEmailAsync(email, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user identified by
        /// <paramref name="email"/>.
        /// </summary>
        /// <param name="email">The email of the user who's data is to be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified email.</returns>
        /// <exception cref="ArgumentException">If the email argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified email.</exception>
        public async Task<UserRecord> GetUserByEmailAsync(
            string email, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return await userManager.GetUserByEmailAsync(email, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user identified by
        /// <paramref name="phoneNumber"/>.
        /// </summary>
        /// <param name="phoneNumber">The phone number of the user who's data is to be retrieved.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified phone number.</returns>
        /// <exception cref="ArgumentException">If the phone number argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified phone number.</exception>
        public async Task<UserRecord> GetUserByPhoneNumberAsync(string phoneNumber)
        {
            return await this.GetUserByPhoneNumberAsync(phoneNumber, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets a <see cref="UserRecord"/> object containing information about the user identified by
        /// <paramref name="phoneNumber"/>.
        /// </summary>
        /// <param name="phoneNumber">The phone number of the user who's data is to be retrieved.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// a user with the specified phone number.</returns>
        /// <exception cref="ArgumentException">If the phone number argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a user cannot be found with the specified phone number.</exception>
        public async Task<UserRecord> GetUserByPhoneNumberAsync(
            string phoneNumber, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return await userManager.GetUserByPhoneNumberAsync(phoneNumber, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the user data corresponding to the specified identifiers.
        /// <para>
        /// There are no ordering guarantees; in particular, the nth entry in the users result list
        /// is not guaranteed to correspond to the nth entry in the input parameters list.
        /// </para>
        /// <para>
        /// A maximum of 100 identifiers may be supplied. If more than 100 identifiers are specified,
        /// this method throws an <c>ArgumentException</c>.
        /// </para>
        /// </summary>
        /// <param name="identifiers">The identifiers used to indicate which user records should be
        /// returned. Must have 100 entries or fewer.</param>
        /// <returns>A task that resolves to the corresponding user records.</returns>
        /// <exception cref="ArgumentException">If any of the identifiers are invalid or if more
        /// than 100 identifiers are specified.</exception>
        public async Task<GetUsersResult> GetUsersAsync(
            IReadOnlyCollection<UserIdentifier> identifiers)
        {
            return await this.GetUsersAsync(identifiers, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the user data corresponding to the specified identifiers.
        /// <para>
        /// There are no ordering guarantees; in particular, the nth entry in the users result list
        /// is not guaranteed to correspond to the nth entry in the input parameters list.
        /// </para>
        /// <para>
        /// A maximum of 100 identifiers may be supplied. If more than 100 identifiers are specified,
        /// this method throws an <c>ArgumentException</c>.
        /// </para>
        /// </summary>
        /// <param name="identifiers">The identifiers used to indicate which user records should be
        /// returned. Must have 100 entries or fewer.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that resolves to the corresponding user records.</returns>
        /// <exception cref="ArgumentException">If any of the identifiers are invalid or if more
        /// than 100 identifiers are specified.</exception>
        public async Task<GetUsersResult> GetUsersAsync(
            IReadOnlyCollection<UserIdentifier> identifiers, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            GetAccountInfoResponse response = await userManager.GetAccountInfoByIdentifiersAsync(identifiers, cancellationToken)
                .ConfigureAwait(false);

            return new GetUsersResult(response, identifiers);
        }

        /// <summary>
        /// Updates an existing user account with the attributes contained in the specified <see cref="UserRecordArgs"/>.
        /// The <see cref="UserRecordArgs.Uid"/> property must be specified.
        /// </summary>
        /// <param name="args">The attributes to update.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// the updated user account.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.</exception>
        /// <exception cref="ArgumentException">If any of the values in <paramref name="args"/> are invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while updating the user account.</exception>
        public async Task<UserRecord> UpdateUserAsync(UserRecordArgs args)
        {
            return await this.UpdateUserAsync(args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing user account with the attributes contained in the specified <see cref="UserRecordArgs"/>.
        /// The <see cref="UserRecordArgs.Uid"/> property must be specified.
        /// </summary>
        /// <param name="args">The attributes to update.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="UserRecord"/> representing
        /// the updated user account.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.</exception>
        /// <exception cref="ArgumentException">If any of the values in <paramref name="args"/> are invalid.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while updating the user account.</exception>
        public async Task<UserRecord> UpdateUserAsync(
            UserRecordArgs args, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var uid = await userManager.UpdateUserAsync(args, cancellationToken)
                .ConfigureAwait(false);
            return await userManager.GetUserByIdAsync(uid, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Revokes all refresh tokens for the specified user.
        ///
        /// <para>Updates the user's <c>tokensValidAfterTimestamp</c> to the current UTC time expressed in
        /// seconds since the epoch and truncated to 1 second accuracy. It is important that
        /// the server on which this is called has its clock set correctly and synchronized.</para>
        ///
        /// <para>While this will revoke all sessions for a specified user and disable any new ID tokens
        /// for existing sessions from getting minted, existing ID tokens may remain active until
        /// their natural expiration (one hour).</para>
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <returns>A task that completes when the user's refresh tokens have been revoked.</returns>
        /// <exception cref="ArgumentException">If the user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while revoking the tokens.</exception>
        public async Task RevokeRefreshTokensAsync(string uid)
        {
            await this.RevokeRefreshTokensAsync(uid, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Revokes all refresh tokens for the specified user.
        ///
        /// <para>Updates the user's <c>tokensValidAfterTimestamp</c> to the current UTC time expressed in
        /// seconds since the epoch and truncated to 1 second accuracy. It is important that
        /// the server on which this is called has its clock set correctly and synchronized.</para>
        ///
        /// <para>While this will revoke all sessions for a specified user and disable any new ID tokens
        /// for existing sessions from getting minted, existing ID tokens may remain active until
        /// their natural expiration (one hour).</para>
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes when the user's refresh tokens have been revoked.</returns>
        /// <exception cref="ArgumentException">If the user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while revoking the tokens.</exception>
        public async Task RevokeRefreshTokensAsync(string uid, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            await userManager.RevokeRefreshTokensAsync(uid, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the user identified by the specified <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <returns>A task that completes when the user account has been deleted.</returns>
        /// <exception cref="ArgumentException">If the user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while deleting the user.</exception>
        public async Task DeleteUserAsync(string uid)
        {
            await this.DeleteUserAsync(uid, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the user identified by the specified <paramref name="uid"/>.
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes when the user account has been deleted.</returns>
        /// <exception cref="ArgumentException">If the user ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while deleting the user.</exception>
        public async Task DeleteUserAsync(string uid, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            await userManager.DeleteUserAsync(uid, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the users specified by the given identifiers.
        /// <para>
        /// Deleting a non-existing user won't generate an error. (i.e. this method is idempotent.)
        /// Non-existing users will be considered to be successfully deleted, and will therefore be
        /// counted in the `DeleteUserResult.SuccessCount` value.
        /// </para>
        /// <para>
        /// A maximum of 1000 identifiers may be supplied. If more than 1000 identifiers are
        /// specified, this method throws an <c>ArgumentException</c>.
        /// </para>
        /// <para>
        /// This API is currently rate limited at the server to 1 QPS. If you exceed this, you may
        /// get a quota exceeded error. Therefore, if you want to delete more than 1000 users, you
        /// may need to add a delay to ensure you don't go over this limit.
        /// </para>
        /// </summary>
        /// <param name="uids">The uids of the users to be deleted. Must have 1000 or fewer entries.
        /// </param>
        /// <returns>A task that resolves to the total number of successful/failed
        /// deletions, as well as the array of errors that correspond to the failed deletions.
        /// </returns>
        /// <exception cref="ArgumentException">If any of the identifiers are invalid or if more
        /// than 1000 identifiers are specified.</exception>
        public async Task<DeleteUsersResult> DeleteUsersAsync(IReadOnlyList<string> uids)
        {
            return await this.DeleteUsersAsync(uids, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the users specified by the given identifiers.
        /// <para>
        /// Deleting a non-existing user won't generate an error. (i.e. this method is idempotent.)
        /// Non-existing users will be considered to be successfully deleted, and will therefore be
        /// counted in the `DeleteUserResult.SuccessCount` value.
        /// </para>
        /// <para>
        /// A maximum of 1000 identifiers may be supplied. If more than 1000 identifiers are
        /// specified, this method throws an <c>ArgumentException</c>.
        /// </para>
        /// <para>
        /// This API is currently rate limited at the server to 1 QPS. If you exceed this, you may
        /// get a quota exceeded error. Therefore, if you want to delete more than 1000 users, you
        /// may need to add a delay to ensure you don't go over this limit.
        /// </para>
        /// </summary>
        /// <param name="uids">The uids of the users to be deleted. Must have 1000 or fewer entries.
        /// </param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that resolves to the total number of successful/failed
        /// deletions, as well as the array of errors that correspond to the failed deletions.
        /// </returns>
        /// <exception cref="ArgumentException">If any of the identifiers are invalid or if more
        /// than 1000 identifiers are specified.</exception>
        public async Task<DeleteUsersResult> DeleteUsersAsync(IReadOnlyList<string> uids, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return await userManager.DeleteUsersAsync(uids, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Imports the provided list of users into Firebase Auth. You can import a maximum of
        /// 1000 users at a time. This operation is optimized for bulk imports and does not
        /// check identifier uniqueness, which could result in duplications.
        ///
        /// <para><a cref="UserImportOptions">UserImportOptions</a> is required to import users with
        /// passwords. See <a cref="FirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs}, UserImportOptions, CancellationToken)">
        /// FirebaseAuth.ImportUsersAsync</a>.</para>
        /// </summary>
        /// <param name="users"> A non-empty list of users to be imported. Length
        /// must not exceed 1000.</param>
        /// <returns> A <a cref="UserImportResult">UserImportResult</a> instance.</returns>
        /// <exception cref="ArgumentException">If the users list is null, empty or has more than
        /// 1000 elements. Or if at least one user specifies a password, with no hashing algorithm
        /// set.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while importing users.</exception>
        public async Task<UserImportResult> ImportUsersAsync(
          IEnumerable<ImportUserRecordArgs> users)
        {
          return await this.ImportUsersAsync(users, default(CancellationToken)).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports the provided list of users into Firebase Auth. You can import a maximum of
        /// 1000 users at a time. This operation is optimized for bulk imports and does not
        /// check identifier uniqueness, which could result in duplications.
        ///
        /// <para><a cref="UserImportOptions">UserImportOptions</a> is required to import users with
        /// passwords. See <a cref="FirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs}, UserImportOptions, CancellationToken)">
        /// FirebaseAuth.ImportUsersAsync</a>.</para>
        /// </summary>
        /// <param name="users"> A non-empty list of users to be imported. Length
        /// must not exceed 1000.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns> A <a cref="UserImportResult">UserImportResult</a> instance.</returns>
        /// <exception cref="ArgumentException">If the users list is null, empty or has more than
        /// 1000 elements. Or if at least one user specifies a password, with no hashing algorithm
        /// set.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while importing users.</exception>
        public async Task<UserImportResult> ImportUsersAsync(
          IEnumerable<ImportUserRecordArgs> users,
          CancellationToken cancellationToken)
        {
          return await this.ImportUsersAsync(users, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports the provided list of users into Firebase Auth. You can import a maximum of
        /// 1000 users at a time. This operation is optimized for bulk imports and does not
        /// check identifier uniqueness, which could result in duplications.</summary>
        /// <param name="users"> A non-empty list of users to be imported.
        /// Length must not exceed 1000.</param>
        /// <param name="options"> A <a cref="UserImportOptions">UserImportOptions</a> instance or
        /// null. Required when importing users with passwords.</param>
        /// <returns> A <a cref="UserImportResult">UserImportResult</a> instance.</returns>
        /// <exception cref="ArgumentException">If the users list is null, empty or has more than
        /// 1000 elements. Or if at least one user specifies a password, with no hashing algorithm
        /// set.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while importing users.</exception>
        public async Task<UserImportResult> ImportUsersAsync(
            IEnumerable<ImportUserRecordArgs> users,
            UserImportOptions options)
        {
          return await this.ImportUsersAsync(users, options, default(CancellationToken)).ConfigureAwait(false);
        }

        /// <summary>
        /// Imports the provided list of users into Firebase Auth. You can import a maximum of
        /// 1000 users at a time. This operation is optimized for bulk imports and does not
        /// check identifier uniqueness, which could result in duplications.</summary>
        /// <param name="users"> A non-empty list of users to be imported.
        /// Length must not exceed 1000.</param>
        /// <param name="options"> A <a cref="UserImportOptions">UserImportOptions</a> instance or
        /// null. Required when importing users with passwords.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns> A <a cref="UserImportResult">UserImportResult</a> instance.</returns>
        /// <exception cref="ArgumentException">If the users list is null, empty or has more than
        /// 1000 elements. Or if at least one user specifies a password, with no hashing algorithm
        /// set.</exception>
        /// <exception cref="FirebaseAuthException">If an error occurs while importing users.</exception>
        public async Task<UserImportResult> ImportUsersAsync(
            IEnumerable<ImportUserRecordArgs> users,
            UserImportOptions options,
            CancellationToken cancellationToken)
        {
          var request = new UserImportRequest(users, options);
          var userManager = this.IfNotDeleted(() => this.userManager.Value);
          return await userManager.ImportUsersAsync(request, cancellationToken);
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
        /// <exception cref="FirebaseAuthException">
        /// If an error occurs while setting custom claims. </exception>
        /// <param name="uid">The user ID string for the custom claims will be set. Must not be null
        /// or longer than 128 characters.
        /// </param>
        /// <param name="claims">The claims to be stored on the user account, and made
        /// available to Firebase security rules. These must be serializable to JSON, and the
        /// serialized claims should not be larger than 1000 characters.</param>
        public async Task SetCustomUserClaimsAsync(
            string uid, IReadOnlyDictionary<string, object> claims)
        {
            await this.SetCustomUserClaimsAsync(uid, claims, default(CancellationToken))
                .ConfigureAwait(false);
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
        /// <exception cref="FirebaseAuthException">
        /// If an error occurs while setting custom claims. </exception>
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
            var user = new UserRecordArgs()
            {
                Uid = uid,
                CustomClaims = claims,
            };

            await userManager.UpdateUserAsync(user, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an async enumerable to iterate or page through users starting from the specified
        /// page token. If the page token is null or unspecified, iteration starts from the first
        /// page. See <a href="https://googleapis.github.io/google-cloud-dotnet/docs/guides/page-streaming.html">
        /// Page Streaming</a> for more details on how to use this API.
        /// </summary>
        /// <param name="options">The options to control the starting point and page size. Pass null
        /// to list from the beginning with default settings.</param>
        /// <returns>A <see cref="PagedAsyncEnumerable{ExportedUserRecords, ExportedUserRecord}"/> instance.</returns>
        public PagedAsyncEnumerable<ExportedUserRecords, ExportedUserRecord> ListUsersAsync(
            ListUsersOptions options)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);

            return userManager.ListUsers(options);
        }

        /// <summary>
        /// Generates the out-of-band email action link for email verification flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while generating the link.</exception>
        /// <param name="email">The email of the user to be verified.</param>
        /// <returns>A task that completes with the email verification link.</returns>
        public async Task<string> GenerateEmailVerificationLinkAsync(string email)
        {
            return await this.GenerateEmailVerificationLinkAsync(email, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for email verification flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while generating the link.</exception>
        /// <param name="email">The email of the user to be verifed.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <returns>A task that completes with the email verification link.</returns>
        public async Task<string> GenerateEmailVerificationLinkAsync(
            string email, ActionCodeSettings settings)
        {
            return await this.GenerateEmailVerificationLinkAsync(email, settings, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for email verification flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while generating the link.</exception>
        /// <param name="email">The email of the user to be verified.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with the email verification reset link.</returns>
        public async Task<string> GenerateEmailVerificationLinkAsync(
            string email, ActionCodeSettings settings, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var request = EmailActionLinkRequest.EmailVerificationLinkRequest(email, settings);
            return await userManager.GenerateEmailActionLinkAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for password reset flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while setting custom claims.</exception>
        /// <param name="email">The email of the user whose password is to be reset.</param>
        /// <returns>A task that completes with the password reset link.</returns>
        public async Task<string> GeneratePasswordResetLinkAsync(string email)
        {
            return await this.GeneratePasswordResetLinkAsync(email, null)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for password reset flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while setting custom claims.</exception>
        /// <param name="email">The email of the user whose password is to be reset.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <returns>A task that completes with the password reset link.</returns>
        public async Task<string> GeneratePasswordResetLinkAsync(
            string email, ActionCodeSettings settings)
        {
            return await this.GeneratePasswordResetLinkAsync(email, settings, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for password reset flows for the specified
        /// email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while setting custom claims.</exception>
        /// <param name="email">The email of the user whose password is to be reset.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with the password reset link.</returns>
        public async Task<string> GeneratePasswordResetLinkAsync(
            string email, ActionCodeSettings settings, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var request = EmailActionLinkRequest.PasswordResetLinkRequest(email, settings);
            return await userManager.GenerateEmailActionLinkAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for email link sign-in flows for the
        /// specified email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while generating the link.</exception>
        /// <param name="email">The email of the user signing in.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <returns>A task that completes with the email sign in link.</returns>
        public async Task<string> GenerateSignInWithEmailLinkAsync(
            string email, ActionCodeSettings settings)
        {
            return await this.GenerateSignInWithEmailLinkAsync(email, settings, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Generates the out-of-band email action link for email link sign-in flows for the
        /// specified email address.
        /// </summary>
        /// <exception cref="FirebaseAuthException">If an error occurs while generating the link.</exception>
        /// <param name="email">The email of the user signing in.</param>
        /// <param name="settings">The action code settings object that defines whether
        /// the link is to be handled by a mobile app and the additional state information to be
        /// passed in the deep link.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with the email sign in link.</returns>
        public async Task<string> GenerateSignInWithEmailLinkAsync(
            string email, ActionCodeSettings settings, CancellationToken cancellationToken)
        {
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            var request = EmailActionLinkRequest.EmailSignInLinkRequest(email, settings);
            return await userManager.GenerateEmailActionLinkAsync(request, cancellationToken)
                .ConfigureAwait(false);
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
            var userManager = this.IfNotDeleted(() => this.userManager.Value);
            return await userManager.CreateSessionCookieAsync(idToken, options, cancellationToken)
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
            var sessionCookieVerifier = this.IfNotDeleted(() => this.sessionCookieVerifier.Value);
            var decodedToken = await sessionCookieVerifier
                .VerifyTokenAsync(sessionCookie, cancellationToken)
                .ConfigureAwait(false);
            if (checkRevoked)
            {
                var revoked = await this.IsRevokedAsync(decodedToken, cancellationToken);
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
            this.userManager.DisposeIfCreated();
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
                    () => FirebaseTokenVerifier.CreateIDTokenVerifier(app), true),
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

        private async Task<bool> IsRevokedAsync(
            FirebaseToken token, CancellationToken cancellationToken)
        {
            var user = await this.GetUserAsync(token.Uid, cancellationToken);
            var cutoff = user.TokensValidAfterTimestamp.Subtract(UserRecord.UnixEpoch)
                .TotalSeconds;
            return token.IssuedAtTimeSeconds < cutoff;
        }

        internal sealed new class Args : AbstractFirebaseAuth.Args
        {
            internal Lazy<FirebaseTokenVerifier> IdTokenVerifier { get; set; }

            internal Lazy<FirebaseTokenVerifier> SessionCookieVerifier { get; set; }

            internal Lazy<FirebaseUserManager> UserManager { get; set; }

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
