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
using Google.Apis.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseUserManagerTest
    {
        private const string MockProjectId = "project1";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public async Task GetUserById()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": [
                    {
                        ""localId"": ""user1""
                    }
                ]}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserByIdAsync("user1");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Null(userRecord.DisplayName);
            Assert.Null(userRecord.Email);
            Assert.Null(userRecord.PhoneNumber);
            Assert.Null(userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);
            Assert.False(userRecord.Disabled);
            Assert.False(userRecord.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch, userRecord.TokensValidAfterTimestamp);
            Assert.Empty(userRecord.CustomClaims);
            Assert.Empty(userRecord.ProviderData);
            Assert.Null(userRecord.UserMetaData.CreationTimestamp);
            Assert.Null(userRecord.UserMetaData.LastSignInTimestamp);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.Request);
            Assert.Equal(new JArray("user1"), request["localId"]);
        }

        [Fact]
        public async Task GetUserByIdWithProperties()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": [
                    {
                        ""localId"": ""user1"",
                        ""displayName"": ""Test User"",
                        ""email"": ""user@domain.com"",
                        ""phoneNumber"": ""+11234567890"",
                        ""photoUrl"": ""https://domain.com/user.png"",
                        ""disabled"": true,
                        ""emailVerified"": true,
                        ""validSince"": 3600,
                        ""customAttributes"": ""{\""admin\"": true, \""level\"": 10}"",
                        ""providerUserInfo"": [
                            {
                                ""rawId"": ""googleuid"",
                                ""providerId"": ""google.com""
                            },
                            {
                                ""rawId"": ""otheruid"",
                                ""providerId"": ""other.com"",
                                ""displayName"": ""Other Name"",
                                ""email"": ""user@other.com"",
                                ""phoneNumber"": ""+10987654321"",
                                ""photoUrl"": ""https://other.com/user.png""
                            }
                        ],
                        ""createdAt"": 100,
                        ""lastLoginAt"": 150,
                    }
                ]}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserByIdAsync("user1");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal("Test User", userRecord.DisplayName);
            Assert.Equal("user@domain.com", userRecord.Email);
            Assert.Equal("+11234567890", userRecord.PhoneNumber);
            Assert.Equal("https://domain.com/user.png", userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);
            Assert.True(userRecord.Disabled);
            Assert.True(userRecord.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch.AddSeconds(3600), userRecord.TokensValidAfterTimestamp);

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
                Response = @"{""users"": []}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.GetUserByIdAsync("user1"));
            Assert.Equal("Failed to get user with uid: user1", exception.Message);
        }

        [Fact]
        public async Task GetUserByIdNull()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByIdAsync(null));
        }

        [Fact]
        public async Task GetUserByIdEmpty()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByIdAsync(string.Empty));
        }

        [Fact]
        public async Task GetUserByEmail()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": [
                    {
                        ""localId"": ""user1""
                    }
                ]}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserByEmailAsync("user@example.com");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Null(userRecord.DisplayName);
            Assert.Null(userRecord.Email);
            Assert.Null(userRecord.PhoneNumber);
            Assert.Null(userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);
            Assert.False(userRecord.Disabled);
            Assert.False(userRecord.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch, userRecord.TokensValidAfterTimestamp);
            Assert.Empty(userRecord.CustomClaims);
            Assert.Empty(userRecord.ProviderData);
            Assert.Null(userRecord.UserMetaData.CreationTimestamp);
            Assert.Null(userRecord.UserMetaData.LastSignInTimestamp);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.Request);
            Assert.Equal(new JArray("user@example.com"), request["email"]);
        }

        [Fact]
        public async Task GetUserByEmailUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.GetUserByEmailAsync("user@example.com"));
            Assert.Equal("Failed to get user with email: user@example.com", exception.Message);
        }

        [Fact]
        public async Task GetUserByEmailNull()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByEmailAsync(null));
        }

        [Fact]
        public async Task GetUserByEmailEmpty()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByEmailAsync(string.Empty));
        }

        [Fact]
        public async Task GetUserByPhoneNumber()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": [
                    {
                        ""localId"": ""user1""
                    }
                ]}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var userRecord = await userManager.GetUserByPhoneNumberAsync("+1234567890");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Null(userRecord.DisplayName);
            Assert.Null(userRecord.Email);
            Assert.Null(userRecord.PhoneNumber);
            Assert.Null(userRecord.PhotoUrl);
            Assert.Equal("firebase", userRecord.ProviderId);
            Assert.False(userRecord.Disabled);
            Assert.False(userRecord.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch, userRecord.TokensValidAfterTimestamp);
            Assert.Empty(userRecord.CustomClaims);
            Assert.Empty(userRecord.ProviderData);
            Assert.Null(userRecord.UserMetaData.CreationTimestamp);
            Assert.Null(userRecord.UserMetaData.LastSignInTimestamp);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.Request);
            Assert.Equal(new JArray("+1234567890"), request["phoneNumber"]);
        }

        [Fact]
        public async Task GetUserByPhoneNumberUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.GetUserByPhoneNumberAsync("+1234567890"));
            Assert.Equal("Failed to get user with phone number: +1234567890", exception.Message);
        }

        [Fact]
        public async Task GetUserByPhoneNumberNull()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByPhoneNumberAsync(null));
        }

        [Fact]
        public async Task GetUserByPhoneNumberEmpty()
        {
            var userManager = this.CreateFirebaseUserManager(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => userManager.GetUserByPhoneNumberAsync(string.Empty));
        }

        [Fact]
        public async Task UpdateUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            await userManager.UpdateUserAsync(new UserArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal("user1", request["localId"]);
            var claims = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>((string)request["customAttributes"]);
            Assert.True((bool)claims["admin"]);
            Assert.Equal(4L, claims["level"]);
            Assert.Equal("gold", claims["package"]);
        }

        [Fact]
        public async Task LargeClaimsUnderLimit()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "testClaim", new string('a', 950) },
            };

            await userManager.UpdateUserAsync(new UserArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            });
        }

        [Fact]
        public async Task EmptyClaims()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserArgs()
            {
                Uid = "user1",
                CustomClaims = new Dictionary<string, object>(),
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);
        }

        [Fact]
        public async Task NullClaims()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserArgs()
            {
                Uid = "user1",
                CustomClaims = null,
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);
        }

        [Fact]
        public void ReservedClaims()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var customClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };

                var args = new UserArgs()
                {
                    Uid = "user1",
                    CustomClaims = customClaims,
                };
                Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
            }
        }

        [Fact]
        public void UpdateUserNoUid()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "key", "value" },
            };

            var args = new UserArgs()
            {
                CustomClaims = customClaims,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public void UpdateUserInvalidUid()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "key", "value" },
            };

            var args = new UserArgs()
            {
                Uid = new string('a', 129),
                CustomClaims = customClaims,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public void EmptyNameClaims()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var emptyClaims = new Dictionary<string, object>()
            {
                    { string.Empty, "value" },
            };

            var args = new UserArgs()
            {
                Uid = "user1",
                CustomClaims = emptyClaims,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public void LargeClaimsOverLimit()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""user1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var largeClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 1001) },
            };

            var args = new UserArgs()
            {
                Uid = "user1",
                CustomClaims = largeClaims,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public async Task UpdateUserIncorrectResponseObject()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            var args = new UserArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            };
            await Assert.ThrowsAsync<FirebaseException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public async Task UpdateUserIncorrectResponseUid()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""notuser1""}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            var args = new UserArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            };
            await Assert.ThrowsAsync<FirebaseException>(async () => await userManager.UpdateUserAsync(args));
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
            var args = new UserArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            };

            await Assert.ThrowsAsync<FirebaseException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public async Task DeleteUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""kind"": ""identitytoolkit#DeleteAccountResponse""}",
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
