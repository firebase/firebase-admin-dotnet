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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseAuthTest
    {
        private const string EmailLinkSignInUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/emailLinkSignin";

        private const string ResetPasswordUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/resetPassword";

        private const string VerifyCustomTokenUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken";

        private const string ContinueUrl = "http://localhost/?a=1&b=2#c=3";

        private static readonly ActionCodeSettings EmailLinkSettings = new ActionCodeSettings()
        {
            Url = ContinueUrl,
            HandleCodeInApp = false,
        };

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
            var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            try
            {
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, customClaims);
                user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
                Assert.True((bool)user.CustomClaims["admin"]);
            }
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
        }

        [Fact]
        public async Task SetCustomUserClaimsWithEmptyClaims()
        {
            var user = await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs());
            var customClaims = new Dictionary<string, object>();

            try
            {
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, customClaims);
                user = await FirebaseAuth.DefaultInstance.GetUserAsync(user.Uid);
                Assert.Empty(user.CustomClaims);
            }
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
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

                // Update user with new properties as well as a provider to link to the user
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
                    ProviderToLink = randomUser.ProviderUser,
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
                Assert.Equal(3, user.ProviderData.Length);
                var providerIds = new HashSet<string>();
                foreach (ProviderUserInfo providerData in user.ProviderData)
                {
                    providerIds.Add(providerData.ProviderId);
                }

                Assert.Equal(providerIds, new HashSet<string>() { "phone", "password", "google.com" });
                Assert.Empty(user.CustomClaims);

                // Get user by email
                user = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(randomUser.Email);
                Assert.Equal(uid, user.Uid);

                // Delete the linked provider and phone number
                var unlinkArgs = new UserRecordArgs()
                {
                    Uid = uid,
                    DisplayName = "Updated Name",
                    Email = randomUser.Email,
                    PhoneNumber = null,
                    PhotoUrl = "https://example.com/photo.png",
                    EmailVerified = true,
                    Password = "secret",
                    ProvidersToDelete = new List<string>()
                    {
                       randomUser.ProviderUser.ProviderId,
                    },
                };
                user = await FirebaseAuth.DefaultInstance.UpdateUserAsync(unlinkArgs);
                Assert.Equal(uid, user.Uid);
                Assert.Equal(randomUser.Email, user.Email);
                Assert.Null(user.PhoneNumber);
                Assert.Equal("Updated Name", user.DisplayName);
                Assert.Equal("https://example.com/photo.png", user.PhotoUrl);
                Assert.True(user.EmailVerified);
                Assert.False(user.Disabled);
                Assert.NotNull(user.UserMetaData.CreationTimestamp);
                Assert.Null(user.UserMetaData.LastSignInTimestamp);
                Assert.Single(user.ProviderData);
                Assert.Equal("password", user.ProviderData.First().ProviderId);
                Assert.Empty(user.CustomClaims);

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
                Assert.Equal("password", user.ProviderData.First().ProviderId);
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

        [Fact]
        public async Task EmailVerificationLink()
        {
            var user = await CreateUserForActionLinksAsync();

            try
            {
                var link = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(
                    user.Email, EmailLinkSettings);

                var uri = new Uri(link);
                var query = HttpUtility.ParseQueryString(uri.Query);
                Assert.Equal(ContinueUrl, query["continueUrl"]);
                Assert.Equal("verifyEmail", query["mode"]);
            }
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
        }

        [Fact]
        public async Task PasswordResetLink()
        {
            var user = await CreateUserForActionLinksAsync();

            try
            {
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
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
        }

        [Fact]
        public async Task SignInWithEmailLink()
        {
            var user = await CreateUserForActionLinksAsync();

            try
            {
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
            finally
            {
                await FirebaseAuth.DefaultInstance.DeleteUserAsync(user.Uid);
            }
        }

        private static async Task<UserRecord> CreateUserForActionLinksAsync()
        {
            var randomUser = RandomUser.Create();
            return await FirebaseAuth.DefaultInstance.CreateUserAsync(new UserRecordArgs()
            {
                Uid = randomUser.Uid,
                Email = randomUser.Email,
                EmailVerified = false,
                Password = "password",
            });
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

    internal class RandomUser
    {
        internal string Uid { get; private set; }

        internal string Email { get; private set; }

        internal string PhoneNumber { get; private set; }

        internal ProviderUserInfo ProviderUser { get; private set; }

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

            var providerUser = new ProviderUserInfo()
            {
                Uid = "google_" + uid,
                ProviderId = "google.com",
            };

            return new RandomUser()
            {
                Uid = uid,
                Email = email,
                PhoneNumber = phone,
                ProviderUser = providerUser,
            };
        }
    }
}
