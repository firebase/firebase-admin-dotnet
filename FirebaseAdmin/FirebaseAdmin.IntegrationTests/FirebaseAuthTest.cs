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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseAuthTest
    {
        private const string VerifyCustomTokenUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken";

        private const string VerifyPasswordUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyPassword";

        public FirebaseAuthTest()
        {
            IntegrationTestUtils.EnsureDefaultApp();
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
        public async Task SetCustomUserClaims()
        {
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("testuser", customClaims);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithEmptyClaims()
        {
            var customClaims = new Dictionary<string, object>();

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("testuser", customClaims);
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
            var randomUser = RandomUser.Create();
            var args = new UserRecordArgs()
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                PhoneNumber = randomUser.PhoneNumber,
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                EmailVerified = true,
                Password = "password",
            };

            var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);

            try
            {
                Assert.Equal(randomUser.Uid, user.Uid);
                Assert.Equal(randomUser.Email, user.Email);
                Assert.Equal(randomUser.PhoneNumber, user.PhoneNumber);
                Assert.Equal(args.DisplayName, user.DisplayName);
                Assert.Equal(args.PhotoUrl, user.PhotoUrl);
                Assert.True(user.EmailVerified);
                Assert.False(user.Disabled);

                // Cannot recreate the same user.
                var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                    async () => await FirebaseAuth.DefaultInstance.CreateUserAsync(args));

                Assert.Equal(AuthErrorCode.UidAlreadyExists, exception.AuthErrorCode);
            }
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
        }

        [Fact]
        public async Task UserLifecycle()
        {
            // Create user
            var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs());
            var uid = user.Uid;
            try
            {
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
                var randomUser = RandomUser.Create();
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
            }
            finally
            {
                // Delete user
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
                var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                    async () => await FirebaseAuth.DefaultInstance.GetUserAsync(uid));

                Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            }
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
            var newUserRecord = await NewUserWithParamsAsync();
            try
            {
                // New users should not have a LastRefreshTimestamp set.
                Assert.Null(newUserRecord.UserMetaData.LastRefreshTimestamp);

                // Login to cause the LastRefreshTimestamp to be set.
                await SignInWithPasswordAsync(newUserRecord.Email, "password");

                var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(newUserRecord.Uid);

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
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(newUserRecord.Uid);
            }
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
            UserRecord user1 = await NewUserWithParamsAsync();
            UserRecord user2 = await NewUserWithParamsAsync();
            UserRecord user3 = await NewUserWithParamsAsync();

            DeleteUsersResult deleteUsersResult = await this.SlowDeleteUsersAsync(
                new List<string> { user1.Uid, user2.Uid, user3.Uid });

            Assert.Equal(3, deleteUsersResult.SuccessCount);
            Assert.Equal(0, deleteUsersResult.FailureCount);
            Assert.Empty(deleteUsersResult.Errors);

            GetUsersResult getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
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
            UserRecord user1 = await NewUserWithParamsAsync();

            DeleteUsersResult deleteUsersResult = await this.SlowDeleteUsersAsync(
                new List<string> { user1.Uid, "uid-that-doesnt-exist" });

            Assert.Equal(2, deleteUsersResult.SuccessCount);
            Assert.Equal(0, deleteUsersResult.FailureCount);
            Assert.Empty(deleteUsersResult.Errors);

            GetUsersResult getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>
                {
                    new UidIdentifier(user1.Uid),
                    new UidIdentifier("uid-that-doesnt-exist"),
                });

            Assert.Empty(getUsersResult.Users);
            Assert.Equal(2, getUsersResult.NotFound.Count());
        }

        [Fact]
        public async Task DeleteUsersIsIdempotent()
        {
            UserRecord user1 = await NewUserWithParamsAsync();

            DeleteUsersResult result = await this.SlowDeleteUsersAsync(
                new List<string> { user1.Uid });

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);

            // Delete the user again, ensuring that everything still counts as a success.
            result = await this.SlowDeleteUsersAsync(
                new List<string> { user1.Uid });

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public async Task ListUsers()
        {
            var users = new List<string>();
            try
            {
                for (int i = 0; i < 3; i++)
                {
                    var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs()
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
            finally
            {
                var deleteTasks = users.Select((uid) => FirebaseAuth.DefaultInstance.DeleteUserAsync(uid));
                await Task.WhenAll(deleteTasks);
            }
        }

        private static async Task<UserRecord> NewUserWithParamsAsync()
        {
            // TODO(rsgowman): This function could be used throughout this file
            // (similar to the other ports).
            RandomUser randomUser = RandomUser.Create();
            var args = new UserRecordArgs()
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                PhoneNumber = randomUser.PhoneNumber,
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                Password = "password",
            };

            return await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
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

        /**
         * The {@code batchDelete} endpoint is currently rate limited to 1qps. Use this test helper
         * to ensure you don't run into quota exceeded errors.
         */
        // TODO(rsgowman): When/if the rate limit is relaxed, eliminate this helper.
        private async Task<DeleteUsersResult> SlowDeleteUsersAsync(IReadOnlyList<string> uids)
        {
            await Task.Delay(millisecondsDelay: 1000);
            return await FirebaseAuth.DefaultInstance.DeleteUsersAsync(uids);
        }

        public class GetUsersFixture : IDisposable
        {
            public GetUsersFixture()
            {
                IntegrationTestUtils.EnsureDefaultApp();

                this.TestUser1 = NewUserWithParamsAsync().Result;
                this.TestUser2 = NewUserWithParamsAsync().Result;
                this.TestUser3 = NewUserWithParamsAsync().Result;

                // The C# port doesn't support importing users, so unlike the other ports, there's
                // no way to create a user with a linked federated provider.
                // TODO(rsgowman): Once either FirebaseAuth.ImportUser() exists (or the UpdateUser()
                // method supports ProviderToLink (#143)), then use it here and
                // adjust the VariousIdentifiers() test below.
            }

            public UserRecord TestUser1 { get; }

            public UserRecord TestUser2 { get; }

            public UserRecord TestUser3 { get; }

            public void Dispose()
            {
                // TODO(rsgowman): deleteUsers (plural) would make more sense here, but it's
                // currently rate limited to 1qps. When/if that's relaxed, change this to just
                // delete them all at once.
                var auth = FirebaseAuth.DefaultInstance;
                auth.DeleteUserAsync(this.TestUser1.Uid).Wait();
                auth.DeleteUserAsync(this.TestUser2.Uid).Wait();
                auth.DeleteUserAsync(this.TestUser3.Uid).Wait();
            }
        }

        public class GetUsers : IClassFixture<GetUsersFixture>
        {
            private FirebaseAuth auth;
            private UserRecord testUser1;
            private UserRecord testUser2;
            private UserRecord testUser3;

            public GetUsers(GetUsersFixture fixture)
            {
                this.auth = FirebaseAuth.DefaultInstance;
                this.testUser1 = fixture.TestUser1;
                this.testUser2 = fixture.TestUser2;
                this.testUser3 = fixture.TestUser3;
            }

            [Fact]
            public async void VariousIdentifiers()
            {
                var getUsersResult = await this.auth.GetUsersAsync(new List<UserIdentifier>()
                {
                    new UidIdentifier(this.testUser1.Uid),
                    new EmailIdentifier(this.testUser2.Email),
                    new PhoneIdentifier(this.testUser3.PhoneNumber),
                    // TODO(rsgowman): Once we're able to create a user with a
                    // provider, do so above and fetch the user like this:
                    // new ProviderIdentifier("google.com", "google_" + importUserUid),
                });

                var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
                var expectedUids = new List<string>() { this.testUser1.Uid, this.testUser2.Uid, this.testUser3.Uid };
                Assert.True(expectedUids.All(expectedUid => uids.Contains(expectedUid)));
                Assert.Empty(getUsersResult.NotFound);
            }

            [Fact]
            public async void IgnoresNonExistingUsers()
            {
                var doesntExistId = new UidIdentifier("uid_that_doesnt_exist");
                var getUsersResult = await this.auth.GetUsersAsync(new List<UserIdentifier>()
                {
                    new UidIdentifier(this.testUser1.Uid),
                    doesntExistId,
                    new UidIdentifier(this.testUser3.Uid),
                });

                var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
                var expectedUids = new List<string>() { this.testUser1.Uid, this.testUser3.Uid };
                Assert.True(expectedUids.All(expectedUid => uids.Contains(expectedUid)));
                Assert.Equal(doesntExistId, getUsersResult.NotFound.Single());
            }

            [Fact]
            public async void OnlyNonExistingUsers()
            {
                var doesntExistId = new UidIdentifier("uid_that_doesnt_exist");
                var getUsersResult = await this.auth.GetUsersAsync(new List<UserIdentifier>()
                {
                    doesntExistId,
                });

                Assert.Empty(getUsersResult.Users);
                Assert.Equal(doesntExistId, getUsersResult.NotFound.Single());
            }

            [Fact]
            public async void DedupsDuplicateUsers()
            {
                var getUsersResult = await this.auth.GetUsersAsync(new List<UserIdentifier>()
                {
                    new UidIdentifier(this.testUser1.Uid),
                    new UidIdentifier(this.testUser1.Uid),
                });

                var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
                var expectedUids = new List<string>() { this.testUser3.Uid };
                Assert.Equal(this.testUser1.Uid, getUsersResult.Users.Single().Uid);
                Assert.Empty(getUsersResult.NotFound);
            }
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

    internal class RandomUser
    {
        internal string Uid { get; private set; }

        internal string Email { get; private set; }

        internal string PhoneNumber { get; private set; }

        internal static RandomUser Create()
        {
            var uid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var email = $"test{uid.Substring(0, 12)}@example.{uid.Substring(12)}.com";

            var phone = "+1";
            var rand = new Random();
            for (int i = 0; i < 10; i++)
            {
                phone += rand.Next(10);
            }

            return new RandomUser()
            {
                Uid = uid,
                Email = email,
                PhoneNumber = phone,
            };
        }
    }
}
