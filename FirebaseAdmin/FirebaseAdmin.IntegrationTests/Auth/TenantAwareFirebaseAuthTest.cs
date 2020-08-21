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
using System.Threading.Tasks;
using System.Web;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Multitenancy;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public class TenantAwareFirebaseAuthTest : IClassFixture<TenantFixture>
    {
        private const string ContinueUrl = "http://localhost/?a=1&b=2#c=3";

        private static readonly ActionCodeSettings EmailLinkSettings = new ActionCodeSettings()
        {
            Url = ContinueUrl,
            HandleCodeInApp = false,
        };

        private readonly TenantAwareFirebaseAuth auth;
        private readonly TemporaryUserBuilder userBuilder;
        private readonly string tenantId;

        public TenantAwareFirebaseAuthTest(TenantFixture fixture)
        {
            this.auth = fixture.Auth;
            this.userBuilder = fixture.UserBuilder;
            this.tenantId = fixture.TenantId;
        }

        [Fact]
        public async Task CustomToken()
        {
            var customToken = await this.auth.CreateCustomTokenAsync("testuser");

            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.tenantId);

            await this.AssertIdTokenIsValid(idToken);
        }

        [Fact]
        public async Task SetCustomUserClaims()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>
            {
                { "admin", true },
            };

            await this.auth.SetCustomUserClaimsAsync(user.Uid, customClaims);
            user = await this.auth.GetUserAsync(user.Uid);

            Assert.True((bool)user.CustomClaims["admin"]);
        }

        [Fact]
        public async Task CreateUserWithParams()
        {
            var args = TemporaryUserBuilder.RandomUserRecordArgs();
            args.EmailVerified = true;
            var user = await this.userBuilder.CreateUserAsync(args);

            Assert.Equal(args.Uid, user.Uid);
            Assert.Equal(args.Email, user.Email);
            Assert.Equal(args.PhoneNumber, user.PhoneNumber);
            Assert.Equal(args.DisplayName, user.DisplayName);
            Assert.Equal(args.PhotoUrl, user.PhotoUrl);
            Assert.True(user.EmailVerified);
            Assert.False(user.Disabled);
            Assert.Equal(this.tenantId, user.TenantId);

            // Cannot recreate the same user.
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await this.auth.CreateUserAsync(args));

            Assert.Equal(AuthErrorCode.UidAlreadyExists, exception.AuthErrorCode);
        }

        [Fact]
        public async Task UserLifecycle()
        {
            // Create user
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var uid = user.Uid;

            Assert.Null(user.Email);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.DisplayName);
            Assert.Null(user.PhotoUrl);
            Assert.False(user.EmailVerified);
            Assert.False(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Empty(user.ProviderData);
            Assert.Empty(user.CustomClaims);
            Assert.Equal(this.tenantId, user.TenantId);

            // Get user by ID
            user = await this.auth.GetUserAsync(uid);
            Assert.Equal(uid, user.Uid);
            Assert.Null(user.Email);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.DisplayName);
            Assert.Null(user.PhotoUrl);
            Assert.False(user.EmailVerified);
            Assert.False(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Empty(user.ProviderData);
            Assert.Empty(user.CustomClaims);
            Assert.Equal(this.tenantId, user.TenantId);

            // Update user
            var randomUser = TemporaryUserBuilder.RandomUserRecordArgs();
            var updateArgs = new UserRecordArgs
            {
                Uid = uid,
                DisplayName = "Updated Name",
                Email = randomUser.Email,
                PhoneNumber = randomUser.PhoneNumber,
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
                Password = "secret",
            };
            user = await this.auth.UpdateUserAsync(updateArgs);
            Assert.Equal(uid, user.Uid);
            Assert.Equal(randomUser.Email, user.Email);
            Assert.Equal(randomUser.PhoneNumber, user.PhoneNumber);
            Assert.Equal("Updated Name", user.DisplayName);
            Assert.Equal("https://example.com/photo.png", user.PhotoUrl);
            Assert.True(user.EmailVerified);
            Assert.False(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Equal(2, user.ProviderData.Length);
            Assert.Empty(user.CustomClaims);
            Assert.Equal(this.tenantId, user.TenantId);

            // Get user by email
            user = await this.auth.GetUserByEmailAsync(randomUser.Email);
            Assert.Equal(uid, user.Uid);
            Assert.Equal(this.tenantId, user.TenantId);

            // Disable user and remove properties
            var disableArgs = new UserRecordArgs
            {
                Uid = uid,
                Disabled = true,
                DisplayName = null,
                PhoneNumber = null,
                PhotoUrl = null,
            };
            user = await this.auth.UpdateUserAsync(disableArgs);
            Assert.Equal(uid, user.Uid);
            Assert.Equal(randomUser.Email, user.Email);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.DisplayName);
            Assert.Null(user.PhotoUrl);
            Assert.True(user.EmailVerified);
            Assert.True(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Single(user.ProviderData);
            Assert.Empty(user.CustomClaims);
            Assert.Equal(this.tenantId, user.TenantId);

            // Delete user
            await this.auth.DeleteUserAsync(uid);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await this.auth.GetUserAsync(uid));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task DeleteUsers()
        {
            var user1 = await this.userBuilder.CreateRandomUserAsync();
            var user2 = await this.userBuilder.CreateRandomUserAsync();
            var user3 = await this.userBuilder.CreateRandomUserAsync();

            await Task.Delay(millisecondsDelay: 1000);
            var deleteUsersResult = await this.auth.DeleteUsersAsync(
                new List<string> { user1.Uid, user2.Uid, user3.Uid });

            Assert.Equal(3, deleteUsersResult.SuccessCount);
            Assert.Equal(0, deleteUsersResult.FailureCount);
            Assert.Empty(deleteUsersResult.Errors);

            var getUsersResult = await this.auth.GetUsersAsync(
                new List<UserIdentifier>
                {
                    new UidIdentifier(user1.Uid),
                    new UidIdentifier(user2.Uid),
                    new UidIdentifier(user3.Uid),
                });

            Assert.Empty(getUsersResult.Users);
            Assert.Equal(3, getUsersResult.NotFound.Count());
        }

        [Fact]
        public async Task RevokeRefreshTokens()
        {
            var customToken = await this.auth.CreateCustomTokenAsync("testuser");
            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.tenantId);

            await this.AssertIdTokenIsValid(idToken, true);

            await Task.Delay(1000);
            await this.auth.RevokeRefreshTokensAsync("testuser");

            await this.AssertIdTokenIsValid(idToken);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await this.auth.VerifyIdTokenAsync(idToken, true));
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.RevokedIdToken, exception.AuthErrorCode);

            idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.tenantId);
            await this.AssertIdTokenIsValid(idToken, true);
        }

        [Fact]
        public async Task ListUsers()
        {
            var users = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs
                {
                    Password = "password",
                });
                users.Add(user.Uid);
            }

            var pagedEnumerable = this.auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();

            var listedUsers = new Dictionary<string, ExportedUserRecord>();
            while (await enumerator.MoveNext())
            {
                var uid = enumerator.Current.Uid;
                if (users.Contains(uid))
                {
                    listedUsers[uid] = enumerator.Current;
                }
            }

            Assert.Equal(users.Count(), listedUsers.Count());
            var errMsgTemplate = "Missing {0} field. A common cause would be "
                + "forgetting to add the 'Firebase Authentication Admin' permission. "
                + "See instructions in CONTRIBUTING.md";
            Assert.All(listedUsers.Values, (user) =>
                {
                    Assert.Equal(this.tenantId, user.TenantId);
                    AssertWithMessage.NotNull(
                        user.PasswordHash, string.Format(errMsgTemplate, "PasswordHash"));
                    AssertWithMessage.NotNull(
                        user.PasswordSalt, string.Format(errMsgTemplate, "PasswordSalt"));
                });
        }

        [Fact]
        public async Task ImportUsers()
        {
            var randomUser = TemporaryUserBuilder.RandomUserRecordArgs();
            var args = new ImportUserRecordArgs
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
            };
            var usersLst = new List<ImportUserRecordArgs>() { args };

            var resp = await this.auth.ImportUsersAsync(usersLst);
            this.userBuilder.AddUid(randomUser.Uid);

            Assert.Equal(1, resp.SuccessCount);
            Assert.Equal(0, resp.FailureCount);

            var user = await this.auth.GetUserAsync(randomUser.Uid);
            Assert.Equal(randomUser.Email, user.Email);
            Assert.Equal(this.tenantId, user.TenantId);
        }

        [Fact]
        public async Task EmailVerificationLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await this.auth.GenerateEmailVerificationLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);
            Assert.Equal("verifyEmail", query["mode"]);
        }

        [Fact]
        public async Task PasswordResetLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await this.auth.GeneratePasswordResetLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);

            var request = new ResetPasswordRequest
            {
                Email = user.Email,
                OldPassword = "password",
                NewPassword = "NewP@$$w0rd",
                OobCode = query["oobCode"],
                TenantId = user.TenantId,
            };
            var resetEmail = await AuthIntegrationUtils.ResetPasswordAsync(request);
            Assert.Equal(user.Email, resetEmail);

            // Password reset also verifies the user's email
            user = await this.auth.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task SignInWithEmailLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await this.auth.GenerateSignInWithEmailLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);

            var idToken = await AuthIntegrationUtils.SignInWithEmailLinkAsync(
                user.Email, query["oobCode"], user.TenantId);
            Assert.NotEmpty(idToken);

            // Sign in with link also verifies the user's email
            user = await this.auth.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        private async Task AssertIdTokenIsValid(string idToken, bool checkRevoked = false)
        {
            var decoded = await this.auth.VerifyIdTokenAsync(idToken, checkRevoked);
            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(this.tenantId, decoded.TenantId);
        }
    }
}
