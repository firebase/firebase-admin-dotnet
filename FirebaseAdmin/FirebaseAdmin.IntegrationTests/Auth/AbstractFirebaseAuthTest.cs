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
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Hash;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public abstract class AbstractFirebaseAuthTest<T>
    where T : AbstractFirebaseAuth
    {
        private const string ContinueUrl = "http://localhost/?a=1&b=2#c=3";

        private static readonly ActionCodeSettings EmailLinkSettings = new ActionCodeSettings()
        {
            Url = ContinueUrl,
            HandleCodeInApp = false,
        };

        private readonly AbstractAuthFixture<T> fixture;
        private readonly TemporaryUserBuilder userBuilder;

        public AbstractFirebaseAuthTest(AbstractAuthFixture<T> fixture)
        {
            this.fixture = fixture;
            this.userBuilder = fixture.UserBuilder;
            this.Auth = fixture.Auth;
        }

        protected T Auth { get; }

        [Fact]
        public async Task CreateCustomToken()
        {
            var customToken = await this.Auth.CreateCustomTokenAsync("testuser");

            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.fixture.TenantId);
            await this.AssertValidIdTokenAsync(idToken);
        }

        [Fact]
        public async Task CreateCustomTokenWithClaims()
        {
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };

            var customToken = await this.Auth.CreateCustomTokenAsync("testuser", developerClaims);

            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.fixture.TenantId);
            var decoded = await this.AssertValidIdTokenAsync(idToken);
            foreach (var entry in developerClaims)
            {
                object value;
                Assert.True(decoded.Claims.TryGetValue(entry.Key, out value));
                Assert.Equal(entry.Value, value);
            }
        }

        [Fact]
        public async Task CreateCustomTokenWithoutServiceAccount()
        {
            var googleCred = FirebaseApp.DefaultInstance.Options.Credential;
            var serviceAcct = (ServiceAccountCredential)googleCred.UnderlyingCredential;
            var token = await ((ITokenAccess)googleCred).GetAccessTokenForRequestAsync();
            var app = FirebaseApp.Create(
                new AppOptions()
                {
                    Credential = GoogleCredential.FromAccessToken(token),
                    ServiceAccountId = serviceAcct.Id,
                    ProjectId = serviceAcct.ProjectId,
                }, "IAMSignApp");

            try
            {
                var auth = this.fixture.AuthFromApp(app);

                var customToken = await this.Auth.CreateCustomTokenAsync("testuser");

                var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                    customToken, this.fixture.TenantId);
                await this.AssertValidIdTokenAsync(idToken);
            }
            finally
            {
                app.Delete();
            }
        }

        [Fact]
        public async Task RevokeRefreshTokens()
        {
            var customToken = await this.Auth.CreateCustomTokenAsync("testuser");
            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.fixture.TenantId);
            await this.AssertValidIdTokenAsync(idToken, true);
            await Task.Delay(1000);

            await this.Auth.RevokeRefreshTokensAsync("testuser");

            await this.AssertValidIdTokenAsync(idToken);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.VerifyIdTokenAsync(idToken, true));
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.RevokedIdToken, exception.AuthErrorCode);

            idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(
                customToken, this.fixture.TenantId);
            await this.AssertValidIdTokenAsync(idToken, true);
        }

        [Fact]
        public async Task SetCustomUserClaims()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await this.Auth.SetCustomUserClaimsAsync(user.Uid, customClaims);

            user = await this.Auth.GetUserAsync(user.Uid);
            Assert.True((bool)user.CustomClaims["admin"]);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithEmptyClaims()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>();

            await this.Auth.SetCustomUserClaimsAsync(user.Uid, customClaims);

            user = await this.Auth.GetUserAsync(user.Uid);
            Assert.Empty(user.CustomClaims);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithWrongUid()
        {
            var customClaims = new Dictionary<string, object>();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.SetCustomUserClaimsAsync("non.existing", customClaims));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
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

            // Cannot recreate the same user.
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.CreateUserAsync(args));

            Assert.Equal(AuthErrorCode.UidAlreadyExists, exception.AuthErrorCode);
        }

        [Fact]
        public async Task CreateUser()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());

            Assert.NotEmpty(user.Uid);
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
        }

        [Fact]
        public async Task GetUser()
        {
            var original = await this.userBuilder.CreateUserAsync(new UserRecordArgs());

            var user = await this.Auth.GetUserAsync(original.Uid);

            Assert.Equal(original.Uid, user.Uid);
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
        }

        [Fact]
        public async Task UpdateUser()
        {
            var original = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var updateArgs = TemporaryUserBuilder.RandomUserRecordArgs();
            updateArgs.Uid = original.Uid;
            updateArgs.EmailVerified = true;

            var user = await this.Auth.UpdateUserAsync(updateArgs);

            Assert.Equal(updateArgs.Uid, user.Uid);
            Assert.Equal(updateArgs.Email, user.Email);
            Assert.Equal(updateArgs.PhoneNumber, user.PhoneNumber);
            Assert.Equal("Random User", user.DisplayName);
            Assert.Equal("https://example.com/photo.png", user.PhotoUrl);
            Assert.True(user.EmailVerified);
            Assert.False(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Equal(2, user.ProviderData.Length);
            Assert.Empty(user.CustomClaims);
        }

        [Fact]
        public async Task GetUserByEmail()
        {
            var original = await this.userBuilder.CreateRandomUserAsync();

            var user = await this.Auth.GetUserByEmailAsync(original.Email);

            Assert.Equal(original.Uid, user.Uid);
            Assert.Equal(original.Email, user.Email);
        }

        [Fact]
        public async Task GetUserByPhoneNumber()
        {
            var original = await this.userBuilder.CreateRandomUserAsync();

            var user = await this.Auth.GetUserByPhoneNumberAsync(original.PhoneNumber);

            Assert.Equal(original.Uid, user.Uid);
            Assert.Equal(original.PhoneNumber, user.PhoneNumber);
        }

        [Fact]
        public async Task DisableUser()
        {
            var original = await this.userBuilder.CreateRandomUserAsync();

            var disableArgs = new UserRecordArgs
            {
                Uid = original.Uid,
                Disabled = true,
                DisplayName = null,
                PhoneNumber = null,
                PhotoUrl = null,
            };
            var user = await this.Auth.UpdateUserAsync(disableArgs);

            Assert.Equal(original.Uid, user.Uid);
            Assert.Equal(original.Email, user.Email);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.DisplayName);
            Assert.Null(user.PhotoUrl);
            Assert.False(user.EmailVerified);
            Assert.True(user.Disabled);
            Assert.NotNull(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
            Assert.Single(user.ProviderData);
            Assert.Empty(user.CustomClaims);
        }

        [Fact]
        public async Task DeleteUser()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());

            await this.Auth.DeleteUserAsync(user.Uid);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.GetUserAsync(user.Uid));
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task GetUserNonExistingUid()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.GetUserAsync("non.existing"));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task GetUserNonExistingEmail()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.GetUserByEmailAsync("non.existing@definitely.non.existing"));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task LastRefreshTime()
        {
            var newUserRecord = await this.userBuilder.CreateRandomUserAsync();

            // New users should not have a LastRefreshTimestamp set.
            Assert.Null(newUserRecord.UserMetaData.LastRefreshTimestamp);

            // Login to cause the LastRefreshTimestamp to be set.
            await AuthIntegrationUtils.SignInWithPasswordAsync(
                newUserRecord.Email, "password", this.fixture.TenantId);

            // Attempt to retrieve the user 3 times (with a small delay between each attempt).
            // Occassionally, this call retrieves the user data without the
            // lastLoginTime/lastRefreshTime set; possibly because it's hitting a different
            // server than the login request uses.
            UserRecord userRecord = null;
            for (int i = 0; i < 3; i++)
            {
                userRecord = await this.Auth.GetUserAsync(newUserRecord.Uid);
                if (userRecord.UserMetaData.LastRefreshTimestamp != null)
                {
                    break;
                }

                await Task.Delay(1000 * (int)Math.Pow(2, i));
            }

            // Ensure the LastRefreshTimstamp is approximately "now" (with a tollerance of 10 minutes).
            var now = DateTime.UtcNow;
            int tolleranceMinutes = 10;
            var minTime = now.AddMinutes(-tolleranceMinutes);
            var maxTime = now.AddMinutes(tolleranceMinutes);
            Assert.NotNull(userRecord.UserMetaData.LastRefreshTimestamp);
            Assert.InRange(
                    userRecord.UserMetaData.LastRefreshTimestamp.Value,
                    minTime,
                    maxTime);
        }

        [Fact]
        public async Task UpdateUserNonExistingUid()
        {
            var args = new UserRecordArgs()
            {
                Uid = "non.existing",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await this.Auth.UpdateUserAsync(args));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task DeleteUserNonExistingUid()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.Auth.DeleteUserAsync("non.existing"));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task DeleteUsers()
        {
            var user1 = await this.userBuilder.CreateRandomUserAsync();
            var user2 = await this.userBuilder.CreateRandomUserAsync();
            var user3 = await this.userBuilder.CreateRandomUserAsync();

            var deleteUsersResult = await this.SlowDeleteUsersAsync(
                new List<string> { user1.Uid, user2.Uid, user3.Uid });

            Assert.Equal(3, deleteUsersResult.SuccessCount);
            Assert.Equal(0, deleteUsersResult.FailureCount);
            Assert.Empty(deleteUsersResult.Errors);

            var getUsersResult = await this.Auth.GetUsersAsync(
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
        public async Task DeleteExistingAndNonExistingUsers()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var deleteUsersResult = await this.SlowDeleteUsersAsync(
                new List<string> { user.Uid, "uid-that-doesnt-exist" });

            Assert.Equal(2, deleteUsersResult.SuccessCount);
            Assert.Equal(0, deleteUsersResult.FailureCount);
            Assert.Empty(deleteUsersResult.Errors);

            var getUsersResult = await this.Auth.GetUsersAsync(
                new List<UserIdentifier>
                {
                    new UidIdentifier(user.Uid),
                    new UidIdentifier("uid-that-doesnt-exist"),
                });

            Assert.Empty(getUsersResult.Users);
            Assert.Equal(2, getUsersResult.NotFound.Count());
        }

        [Fact]
        public async Task DeleteUsersIsIdempotent()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var result = await this.SlowDeleteUsersAsync(
                new List<string> { user.Uid });

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);

            // Delete the user again, ensuring that everything still counts as a success.
            result = await this.SlowDeleteUsersAsync(
                new List<string> { user.Uid });

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ListUsers()
        {
            var users = new List<string>();
            for (int i = 0; i < 3; i++)
            {
                var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs()
                {
                    Password = "password",
                });
                users.Add(user.Uid);
            }

            var pagedEnumerable = this.Auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();

            var listedUsers = new List<string>();
            while (await enumerator.MoveNextAsync())
            {
                var uid = enumerator.Current.Uid;
                if (users.Contains(uid) && !listedUsers.Contains(uid))
                {
                    listedUsers.Add(uid);
                    var errMsgTemplate = "Missing {0} field. A common cause would be "
                        + "forgetting to add the 'Firebase Authentication Admin' permission. "
                        + "See instructions in CONTRIBUTING.md";
                    AssertWithMessage.NotNull(
                        enumerator.Current.PasswordHash,
                        string.Format(errMsgTemplate, "PasswordHash"));
                    AssertWithMessage.NotNull(
                        enumerator.Current.PasswordSalt,
                        string.Format(errMsgTemplate, "PasswordSalt"));
                }
            }

            Assert.Equal(3, listedUsers.Count);
        }

        [Fact]
        public async Task ImportUsers()
        {
            var randomUser = TemporaryUserBuilder.RandomUserRecordArgs();
            var args = new ImportUserRecordArgs()
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
            };
            var usersLst = new List<ImportUserRecordArgs>() { args };

            var resp = await this.Auth.ImportUsersAsync(usersLst);
            this.userBuilder.AddUid(randomUser.Uid);

            Assert.Equal(1, resp.SuccessCount);
            Assert.Equal(0, resp.FailureCount);

            var user = await this.Auth.GetUserAsync(randomUser.Uid);
            Assert.Equal(randomUser.Email, user.Email);
        }

        [Fact]
        public async Task ImportUsersWithPassword()
        {
            var randomUser = TemporaryUserBuilder.RandomUserRecordArgs();
            var args = new ImportUserRecordArgs()
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
                PasswordSalt = Encoding.ASCII.GetBytes("NaCl"),
                PasswordHash = Convert.FromBase64String("V358E8LdWJXAO7muq0CufVpEOXaj8aFiC7"
                    + "T/rcaGieN04q/ZPJ08WhJEHGjj9lz/2TT+/86N5VjVoc5DdBhBiw=="),
                CustomClaims = new Dictionary<string, object>()
                {
                    { "admin", true },
                },
                UserProviders = new List<UserProvider>
                {
                    new UserProvider()
                    {
                        Uid = randomUser.Uid,
                        Email = randomUser.Email,
                        DisplayName = "John Doe",
                        PhotoUrl = "http://example.com/123/photo.png",
                        ProviderId = "google.com",
                    },
                    new UserProvider()
                    {
                        Uid = "fb.uid",
                        Email = "johndoe@gmail.com",
                        DisplayName = "John Doe",
                        PhotoUrl = "http://example.com/123/photo.png",
                        ProviderId = "facebook.com",
                    },
                },
            };

            var options = new UserImportOptions()
            {
                Hash = new Scrypt()
                {
                    Key = Convert.FromBase64String("jxspr8Ki0RYycVU8zykbdLGjFQ3McFUH0uiiTvC"
                        + "8pVMXAn210wjLNmdZJzxUECKbm0QsEmYUSDzZvpjeJ9WmXA=="),
                    SaltSeparator = Convert.FromBase64String("Bw=="),
                    Rounds = 8,
                    MemoryCost = 14,
                },
            };
            var usersLst = new List<ImportUserRecordArgs>() { args };

            var resp = await this.Auth.ImportUsersAsync(usersLst, options);
            this.userBuilder.AddUid(randomUser.Uid);

            Assert.Equal(1, resp.SuccessCount);
            Assert.Equal(0, resp.FailureCount);

            var user = await this.Auth.GetUserAsync(randomUser.Uid);
            Assert.Equal(randomUser.Email, user.Email);
            var idToken = await AuthIntegrationUtils.SignInWithPasswordAsync(
                randomUser.Email, "password", this.fixture.TenantId);
            Assert.False(string.IsNullOrEmpty(idToken));
        }

        [Fact]
        public async Task EmailVerificationLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await this.Auth.GenerateEmailVerificationLinkAsync(
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

            var link = await this.Auth.GeneratePasswordResetLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);

            var request = new ResetPasswordRequest()
            {
                Email = user.Email,
                OldPassword = "password",
                NewPassword = "NewP@$$w0rd",
                OobCode = query["oobCode"],
                TenantId = this.fixture.TenantId,
            };
            var resetEmail = await AuthIntegrationUtils.ResetPasswordAsync(request);
            Assert.Equal(user.Email, resetEmail);

            // Password reset also verifies the user's email
            user = await this.Auth.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task SignInWithEmailLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await this.Auth.GenerateSignInWithEmailLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);

            var idToken = await AuthIntegrationUtils.SignInWithEmailLinkAsync(
                user.Email, query["oobCode"], this.fixture.TenantId);
            Assert.NotEmpty(idToken);

            // Sign in with link also verifies the user's email
            user = await this.Auth.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        private async Task<FirebaseToken> AssertValidIdTokenAsync(
            string idToken, bool checkRevoked = false)
        {
            var decoded = await this.Auth.VerifyIdTokenAsync(idToken, checkRevoked);
            Assert.Equal("testuser", decoded.Uid);
            Assert.Equal(this.fixture.TenantId, decoded.TenantId);
            return decoded;
        }

        /// <summary>
        /// The <c>batchDelete</c> endpoint is currently rate limited to 1qps. Use this test helper
        /// to ensure you don't run into quota exceeded errors.
        /// </summary>
        private async Task<DeleteUsersResult> SlowDeleteUsersAsync(IReadOnlyList<string> uids)
        {
            // TODO(rsgowman): When/if the rate limit is relaxed, eliminate this helper.
            await Task.Delay(1000);
            return await this.Auth.DeleteUsersAsync(uids);
        }
    }

    /// <summary>
    /// Additional Xunit style asserts that allow specifying an error message upon failure.
    /// </summary>
    internal static class AssertWithMessage
    {
        internal static void NotNull(object obj, string msg)
        {
            if (obj == null)
            {
                throw new Xunit.Sdk.XunitException("Assert.NotNull() Failure: " + msg);
            }
        }
    }
}
