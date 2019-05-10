// Copyright 2019, Google Inc. All rights reserved.
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
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseUserManagerTest
    {
        private const string MockProjectId = "project1";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void InvalidUidForUserRecord()
        {
            Assert.Throws<ArgumentException>(() => new UserRecord((string)null));
            Assert.Throws<ArgumentException>(() => new UserRecord((GetAccountInfoResponse.User)null));
            Assert.Throws<ArgumentException>(() => new UserRecord(string.Empty));
            Assert.Throws<ArgumentException>(() => new UserRecord(new string('a', 129)));
        }

        [Fact]
        public void ReservedClaims()
        {
            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var customClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };

                Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = customClaims });
            }
        }

        [Fact]
        public void EmptyClaims()
        {
            var emptyClaims = new Dictionary<string, object>()
            {
                    { string.Empty, "value" },
            };

            Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = emptyClaims });
        }

        [Fact]
        public void TooLargeClaimsPayload()
        {
            var customClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 1001) },
            };

            Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = customClaims });
        }

        [Fact]
        public async Task GetUserById()
        {
            var handler = new MockMessageHandler()
            {
                Response = new GetAccountInfoResponse()
                {
                    Kind = "identitytoolkit#GetAccountInfoResponse",
                    Users = new List<GetAccountInfoResponse.User>()
                    {
                        new GetAccountInfoResponse.User() { UserId = "user1" },
                    },
                },
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserById("user1");
            Assert.Equal("user1", userRecord.Uid);
            Assert.Null(userRecord.DisplayName);
            Assert.Null(userRecord.Email);
            Assert.Null(userRecord.PhoneNumber);
            Assert.Null(userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);
            Assert.Empty(userRecord.CustomClaims);
            Assert.Empty(userRecord.ProviderData);
            Assert.False(userRecord.Disabled);
            Assert.False(userRecord.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch, userRecord.TokensValidAfterTimestamp);
            Assert.Equal(DateTime.MinValue, userRecord.UserMetaData.CreationTimestamp);
            Assert.Equal(DateTime.MinValue, userRecord.UserMetaData.LastSignInTimestamp);
        }

        [Fact]
        public async Task GetUserByIdWithProperties()
        {
            var handler = new MockMessageHandler()
            {
                Response = new GetAccountInfoResponse()
                {
                    Kind = "identitytoolkit#GetAccountInfoResponse",
                    Users = new List<GetAccountInfoResponse.User>()
                    {
                        new GetAccountInfoResponse.User()
                        {
                            UserId = "user1",
                            DisplayName = "Test User",
                            Email = "user@domain.com",
                            PhoneNumber = "+11234567890",
                            PhotoUrl = "https://domain.com/user.png",
                            Disabled = true,
                            EmailVerified = true,
                            ValidSince = 3600,
                            CreatedAt = 100,
                            LastLoginAt = 150,
                            CustomClaims = @"{""admin"": true, ""level"": 10}",
                            Providers = new List<GetAccountInfoResponse.Provider>()
                            {
                                new GetAccountInfoResponse.Provider()
                                {
                                    ProviderID = "google.com",
                                    UserId = "googleuid",
                                },
                                new GetAccountInfoResponse.Provider()
                                {
                                    ProviderID = "other.com",
                                    UserId = "otheruid",
                                    DisplayName = "Other Name",
                                    Email = "user@other.com",
                                    PhotoUrl = "https://other.com/user.png",
                                    PhoneNumber = "+10987654321",
                                },
                            },
                        },
                    },
                },
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserById("user1");
            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal("Test User", userRecord.DisplayName);
            Assert.Equal("user@domain.com", userRecord.Email);
            Assert.Equal("+11234567890", userRecord.PhoneNumber);
            Assert.Equal("https://domain.com/user.png", userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);

            var claims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "level", 10L },
            };
            Assert.Equal(claims, userRecord.CustomClaims);

            Assert.Equal(2, userRecord.ProviderData.Length);
            var provider = userRecord.ProviderData[0];
            Assert.Equal("google.com", provider.ProviderId);
            Assert.Equal("googleuid", provider.Uid);
            Assert.Null(provider.DisplayName);
            Assert.Null(provider.Email);
            Assert.Null(provider.PhoneNumber);
            Assert.Null(provider.PhotoUrl);

            provider = userRecord.ProviderData[1];
            Assert.Equal("other.com", provider.ProviderId);
            Assert.Equal("otheruid", provider.Uid);
            Assert.Equal("Other Name", provider.DisplayName);
            Assert.Equal("user@other.com", provider.Email);
            Assert.Equal("+10987654321", provider.PhoneNumber);
            Assert.Equal("https://other.com/user.png", provider.PhotoUrl);

            Assert.True(userRecord.Disabled);
            Assert.True(userRecord.EmailVerified);

            Assert.Equal(UserRecord.UnixEpoch.AddSeconds(3600), userRecord.TokensValidAfterTimestamp);
            var metadata = userRecord.UserMetaData;
            Assert.NotNull(metadata);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(100), metadata.CreationTimestamp);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(150), metadata.LastSignInTimestamp);
        }

        [Fact]
        public async Task GetUserByIdUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.GetUserById("user1"));
        }

        [Fact]
        public async Task UpdateUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = new GetAccountInfoResponse()
                {
                    Kind = "identitytoolkit#GetAccountInfoResponse",
                    Users = new List<GetAccountInfoResponse.User>()
                    {
                        new GetAccountInfoResponse.User() { UserId = "user1" },
                    },
                },
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
            };

            await userManager.UpdateUserAsync(new UserRecord("user1") { CustomClaims = customClaims });
        }

        [Fact]
        public async Task UpdateUserIncorrectResponseObject()
        {
            var handler = new MockMessageHandler()
            {
                Response = new object(),
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.UpdateUserAsync(new UserRecord("user1") { CustomClaims = customClaims }));
        }

        [Fact]
        public async Task UpdateUserIncorrectResponseUid()
        {
            var handler = new MockMessageHandler()
            {
                Response = new UserRecord("testuser"),
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.UpdateUserAsync(new UserRecord("user1") { CustomClaims = customClaims }));
        }

        [Fact]
        public async Task UpdateUserHttpError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.UpdateUserAsync(new UserRecord("user1") { CustomClaims = customClaims }));
        }

        [Fact]
        public async Task DeleteUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = new Dictionary<string, string>()
                {
                    { "kind", "identitytoolkit#DeleteAccountResponse" },
                },
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.DeleteUserAsync("user1");
        }

        [Fact]
        public async Task DeleteUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            await Assert.ThrowsAsync<FirebaseException>(
               async () => await userManager.DeleteUserAsync("user1"));
        }

        private FirebaseUserManager CreateFirebaseUserManager(HttpMessageHandler handler)
        {
            var args = new FirebaseUserManagerArgs
            {
                Credential = MockCredential,
                ProjectId = MockProjectId,
                ClientFactory = new MockHttpClientFactory(handler),
            };
            return new FirebaseUserManager(args);
        }
    }
}
