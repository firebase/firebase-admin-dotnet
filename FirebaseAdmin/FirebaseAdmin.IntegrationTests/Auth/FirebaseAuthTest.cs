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
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Hash;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class FirebaseAuthTest : IClassFixture<TemporaryUserBuilder>
    {
        private const string EmailLinkSignInUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/emailLinkSignin";

        private const string ResetPasswordUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/resetPassword";

        private const string VerifyCustomTokenUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken";

        private const string VerifyPasswordUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyPassword";

        private const string ContinueUrl = "http://localhost/?a=1&b=2#c=3";

        private static readonly ActionCodeSettings EmailLinkSettings = new ActionCodeSettings()
        {
            Url = ContinueUrl,
            HandleCodeInApp = false,
        };

        private readonly TemporaryUserBuilder userBuilder;

        public FirebaseAuthTest(TemporaryUserBuilder userBuilder)
        {
            this.userBuilder = userBuilder;
        }

        [Fact]
        public async Task CreateCustomToken()
        {
            var customToken = await FirebaseAuth.DefaultInstance
                .CreateCustomTokenAsync("testuser");
            var idToken = await SignInWithCustomTokenAsync(customToken);
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            Assert.Equal("testuser", decoded.Uid);
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
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(
                "testuser", developerClaims);
            var idToken = await SignInWithCustomTokenAsync(customToken);
            Assert.False(string.IsNullOrEmpty(idToken));
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            Assert.Equal("testuser", decoded.Uid);
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
                }, "IAMSignApp");
            try
            {
                var customToken = await FirebaseAuth.GetAuth(app).CreateCustomTokenAsync(
                    "testuser");
                var idToken = await SignInWithCustomTokenAsync(customToken);
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                Assert.Equal("testuser", decoded.Uid);
            }
            finally
            {
                app.Delete();
            }
        }

        [Fact]
        public async Task RevokeRefreshTokens()
        {
            var customToken = await FirebaseAuth.DefaultInstance
                .CreateCustomTokenAsync("testuser");
            var idToken = await SignInWithCustomTokenAsync(customToken);
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, true);
            Assert.Equal("testuser", decoded.Uid);

            await Task.Delay(1000);
            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync("testuser");

            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, false);
            Assert.Equal("testuser", decoded.Uid);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, true));
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.RevokedIdToken, exception.AuthErrorCode);

            idToken = await SignInWithCustomTokenAsync(customToken);
            decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, true);
            Assert.Equal("testuser", decoded.Uid);
        }

        [Fact]
        public async Task SetCustomUserClaims()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, customClaims);
            user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
            Assert.True((bool)user.CustomClaims["admin"]);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithEmptyClaims()
        {
            var user = await this.userBuilder.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>();

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, customClaims);
            user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
            Assert.Empty(user.CustomClaims);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithWrongUid()
        {
            var customClaims = new Dictionary<string, object>();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("non.existing", customClaims));

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
                async () => await FirebaseAuth.DefaultInstance.CreateUserAsync(args));

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

            // Get user by ID
            user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
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

            // Update user
            var randomUser = TemporaryUserBuilder.RandomUserRecordArgs();
            var updateArgs = new UserRecordArgs()
            {
                Uid = uid,
                DisplayName = "Updated Name",
                Email = randomUser.Email,
                PhoneNumber = randomUser.PhoneNumber,
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
                Password = "secret",
            };
            user = await FirebaseAuth.DefaultInstance.UpdateUserAsync(updateArgs);
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

            // Get user by email
            user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(randomUser.Email);
            Assert.Equal(uid, user.Uid);

            // Disable user and remove properties
            var disableArgs = new UserRecordArgs()
            {
                Uid = uid,
                Disabled = true,
                DisplayName = null,
                PhoneNumber = null,
                PhotoUrl = null,
            };
            user = await FirebaseAuth.DefaultInstance.UpdateUserAsync(disableArgs);
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

            // Delete user
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserAsync(uid));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task GetUserNonExistingUid()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserAsync("non.existing"));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task GetUserNonExistingEmail()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserByEmailAsync("non.existing@definitely.non.existing"));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task LastRefreshTime()
        {
            var newUserRecord = await this.userBuilder.CreateRandomUserAsync();

            // New users should not have a LastRefreshTimestamp set.
            Assert.Null(newUserRecord.UserMetaData.LastRefreshTimestamp);

            // Login to cause the LastRefreshTimestamp to be set.
            await SignInWithPasswordAsync(newUserRecord.Email, "password");

            // Attempt to retrieve the user 3 times (with a small delay between each attempt).
            // Occassionally, this call retrieves the user data without the
            // lastLoginTime/lastRefreshTime set; possibly because it's hitting a different
            // server than the login request uses.
            UserRecord userRecord = null;
            for (int i = 0; i < 3; i++)
            {
                userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(newUserRecord.Uid);

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
                async () => await FirebaseAuth.DefaultInstance.UpdateUserAsync(args));

            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
        }

        [Fact]
        public async Task DeleteUserNonExistingUid()
        {
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.DeleteUserAsync("non.existing"));

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

            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
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

            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
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

            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();

            var listedUsers = new List<string>();
            while (await enumerator.MoveNext())
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

            var resp = await FirebaseAuth.DefaultInstance.ImportUsersAsync(usersLst);
            this.userBuilder.AddUid(randomUser.Uid);

            Assert.Equal(1, resp.SuccessCount);
            Assert.Equal(0, resp.FailureCount);

            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(randomUser.Uid);
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
            var resp = await FirebaseAuth.DefaultInstance.ImportUsersAsync(usersLst, options);
            this.userBuilder.AddUid(randomUser.Uid);

            Assert.Equal(1, resp.SuccessCount);
            Assert.Equal(0, resp.FailureCount);

            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(randomUser.Uid);
            Assert.Equal(randomUser.Email, user.Email);
            var idToken = await SignInWithPasswordAsync(randomUser.Email, "password");
            Assert.False(string.IsNullOrEmpty(idToken));
        }

        [Fact]
        public async Task EmailVerificationLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(
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

            var link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(
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
            };
            var resetEmail = await ResetPasswordAsync(request);
            Assert.Equal(user.Email, resetEmail);

            // Password reset also verifies the user's email
            user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task SignInWithEmailLink()
        {
            var user = await this.userBuilder.CreateRandomUserAsync();

            var link = await FirebaseAuth.DefaultInstance.GenerateSignInWithEmailLinkAsync(
                user.Email, EmailLinkSettings);

            var uri = new Uri(link);
            var query = HttpUtility.ParseQueryString(uri.Query);
            Assert.Equal(ContinueUrl, query["continueUrl"]);

            var idToken = await SignInWithEmailLinkAsync(user.Email, query["oobCode"]);
            Assert.NotEmpty(idToken);

            // Sign in with link also verifies the user's email
            user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
            Assert.True(user.EmailVerified);
        }

        [Fact]
        public async Task SessionCookie()
        {
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync("testuser");
            var idToken = await SignInWithCustomTokenAsync(customToken);

            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromHours(1),
            };
            var sessionCookie = await FirebaseAuth.DefaultInstance.CreateSessionCookieAsync(
                idToken, options);
            var decoded = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(sessionCookie);
            Assert.Equal("testuser", decoded.Uid);

            await Task.Delay(1000);
            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync("testuser");
            decoded = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(sessionCookie);
            Assert.Equal("testuser", decoded.Uid);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(
                    sessionCookie, true));
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.RevokedSessionCookie, exception.AuthErrorCode);

            idToken = await SignInWithCustomTokenAsync(customToken);
            sessionCookie = await FirebaseAuth.DefaultInstance.CreateSessionCookieAsync(
                idToken, options);
            decoded = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(sessionCookie, true);
            Assert.Equal("testuser", decoded.Uid);
        }

        private static async Task<string> SignInWithCustomTokenAsync(string customToken)
        {
            var rb = new Google.Apis.Requests.RequestBuilder()
            {
                Method = Google.Apis.Http.HttpConsts.Post,
                BaseUri = new Uri(VerifyCustomTokenUrl),
            };
            rb.AddParameter(RequestParameterType.Query, "key", IntegrationTestUtils.GetApiKey());
            var request = rb.CreateRequest();
            var jsonSerializer = Google.Apis.Json.NewtonsoftJsonSerializer.Instance;
            var payload = jsonSerializer.Serialize(new SignInRequest
            {
                CustomToken = customToken,
                ReturnSecureToken = true,
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = jsonSerializer.Deserialize<SignInResponse>(json);
                return parsed.IdToken;
            }
        }

        private static async Task<string> SignInWithPasswordAsync(string email, string password)
        {
            var rb = new Google.Apis.Requests.RequestBuilder()
            {
                Method = Google.Apis.Http.HttpConsts.Post,
                BaseUri = new Uri(VerifyPasswordUrl),
            };
            rb.AddParameter(RequestParameterType.Query, "key", IntegrationTestUtils.GetApiKey());
            var request = rb.CreateRequest();
            var jsonSerializer = Google.Apis.Json.NewtonsoftJsonSerializer.Instance;
            var payload = jsonSerializer.Serialize(new VerifyPasswordRequest
            {
                Email = email,
                Password = password,
                ReturnSecureToken = true,
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = jsonSerializer.Deserialize<VerifyPasswordResponse>(json);
                return parsed.IdToken;
            }
        }

        private static async Task<string> SignInWithEmailLinkAsync(string email, string oobCode)
        {
            var rb = new Google.Apis.Requests.RequestBuilder()
            {
                Method = Google.Apis.Http.HttpConsts.Post,
                BaseUri = new Uri(EmailLinkSignInUrl),
            };
            rb.AddParameter(RequestParameterType.Query, "key", IntegrationTestUtils.GetApiKey());

            var data = new Dictionary<string, object>()
            {
                { "email", email },
                { "oobCode", oobCode },
            };
            var jsonSerializer = Google.Apis.Json.NewtonsoftJsonSerializer.Instance;
            var payload = jsonSerializer.Serialize(data);

            var request = rb.CreateRequest();
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = jsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return (string)parsed["idToken"];
            }
        }

        private static async Task<string> ResetPasswordAsync(ResetPasswordRequest data)
        {
            var rb = new Google.Apis.Requests.RequestBuilder()
            {
                Method = Google.Apis.Http.HttpConsts.Post,
                BaseUri = new Uri(ResetPasswordUrl),
            };
            rb.AddParameter(RequestParameterType.Query, "key", IntegrationTestUtils.GetApiKey());

            var jsonSerializer = Google.Apis.Json.NewtonsoftJsonSerializer.Instance;
            var payload = jsonSerializer.Serialize(data);

            var request = rb.CreateRequest();
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = jsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return (string)parsed["email"];
            }
        }

        /**
         * The <c>batchDelete</c> endpoint is currently rate limited to 1qps. Use this test helper
         * to ensure you don't run into quota exceeded errors.
         */
        // TODO(rsgowman): When/if the rate limit is relaxed, eliminate this helper.
        private async Task<DeleteUsersResult> SlowDeleteUsersAsync(IReadOnlyList<string> uids)
        {
            await Task.Delay(millisecondsDelay: 1000);
            return await FirebaseAuth.DefaultInstance.DeleteUsersAsync(uids);
        }
    }

    /**
     * Additional Xunit style asserts that allow specifying an error message upon failure.
     */
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

    internal class ResetPasswordRequest
    {
        [Newtonsoft.Json.JsonProperty("email")]
        public string Email { get; set; }

        [Newtonsoft.Json.JsonProperty("oldPassword")]
        public string OldPassword { get; set; }

        [Newtonsoft.Json.JsonProperty("newPassword")]
        public string NewPassword { get; set; }

        [Newtonsoft.Json.JsonProperty("oobCode")]
        public string OobCode { get; set; }
    }

    internal class SignInRequest
    {
        [Newtonsoft.Json.JsonProperty("token")]
        public string CustomToken { get; set; }

        [Newtonsoft.Json.JsonProperty("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }

    internal class SignInResponse
    {
        [Newtonsoft.Json.JsonProperty("idToken")]
        public string IdToken { get; set; }
    }

    internal class VerifyPasswordRequest
    {
        [Newtonsoft.Json.JsonProperty("email")]
        public string Email { get; set; }

        [Newtonsoft.Json.JsonProperty("password")]
        public string Password { get; set; }

        [Newtonsoft.Json.JsonProperty("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }

    internal class VerifyPasswordResponse
    {
        [Newtonsoft.Json.JsonProperty("idToken")]
        public string IdToken { get; set; }
    }
}
