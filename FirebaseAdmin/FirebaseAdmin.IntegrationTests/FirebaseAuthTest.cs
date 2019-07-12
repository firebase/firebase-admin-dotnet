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

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("non.existing", customClaims));
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
                await Assert.ThrowsAsync<FirebaseException>(
                    async () => await FirebaseAuth.DefaultInstance.CreateUserAsync(args));
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
                await Assert.ThrowsAsync<FirebaseException>(
                    async () => await FirebaseAuth.DefaultInstance.GetUserAsync(uid));
            }
        }

        [Fact]
        public async Task GetUserNonExistingUid()
        {
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserAsync("non.existing"));
        }

        [Fact]
        public async Task GetUserNonExistingEmail()
        {
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserByEmailAsync("non.existing@definitely.non.existing"));
        }

        [Fact]
        public async Task UpdateUserNonExistingUid()
        {
            var args = new UserRecordArgs()
            {
                Uid = "non.existing",
            };
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.UpdateUserAsync(args));
        }

        [Fact]
        public async Task DeleteUserNonExistingUid()
        {
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.DeleteUserAsync("non.existing"));
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
                        Assert.NotNull(enumerator.Current.PasswordHash);
                        Assert.NotNull(enumerator.Current.PasswordSalt);
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
