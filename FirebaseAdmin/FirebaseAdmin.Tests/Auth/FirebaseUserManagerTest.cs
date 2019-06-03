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

        private static readonly string CreateUserResponse = @"{""localId"": ""user1""}";

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
        public async Task CreateUser()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            var uid = await userManager.CreateUserAsync(new UserRecordArgs());

            Assert.Equal("user1", uid);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Empty(request);
        }

        [Fact]
        public async Task CreateUserWithArgs()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            var uid = await userManager.CreateUserAsync(new UserRecordArgs()
            {
                Disabled = true,
                DisplayName = "Test User",
                Email = "user@example.com",
                EmailVerified = true,
                Password = "secret",
                PhoneNumber = "+1234567890",
                PhotoUrl = "https://example.com/user.png",
                Uid = "user1",
            });

            Assert.Equal("user1", uid);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.True((bool)request["disabled"]);
            Assert.Equal("Test User", request["displayName"]);
            Assert.Equal("user@example.com", request["email"]);
            Assert.True((bool)request["emailVerified"]);
            Assert.Equal("secret", request["password"]);
            Assert.Equal("+1234567890", request["phoneNumber"]);
            Assert.Equal("https://example.com/user.png", request["photoUrl"]);
        }

        [Fact]
        public async Task CreateUserWithExplicitDefaults()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            var uid = await userManager.CreateUserAsync(new UserRecordArgs()
            {
                Disabled = false,
                DisplayName = null,
                Email = null,
                EmailVerified = false,
                Password = null,
                PhoneNumber = null,
                PhotoUrl = null,
                Uid = null,
            });

            Assert.Equal("user1", uid);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal(2, request.Count);
            Assert.False((bool)request["disabled"]);
            Assert.False((bool)request["emailVerified"]);
        }

        [Fact]
        public async Task CreateUserEmptyUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserLongUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Uid = new string('a', 129),
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserEmptyEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserInvalidEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserEmptyPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserInvalidPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = "1234567890",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Equal(
                "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.",
                exception.Message);
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserEmptyPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserInvalidPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserShortPassword()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.CreateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task CreateUserIncorrectResponse()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var userManager = this.CreateFirebaseUserManager(handler);

            var args = new UserRecordArgs();
            await Assert.ThrowsAsync<FirebaseException>(async () => await userManager.CreateUserAsync(args));
        }

        [Fact]
        public async Task UpdateUser()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                CustomClaims = customClaims,
                Disabled = true,
                DisplayName = "Test User",
                Email = "user@example.com",
                EmailVerified = true,
                Password = "secret",
                PhoneNumber = "+1234567890",
                PhotoUrl = "https://example.com/user.png",
                Uid = "user1",
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal("user1", request["localId"]);
            Assert.True((bool)request["disableUser"]);
            Assert.Equal("Test User", request["displayName"]);
            Assert.Equal("user@example.com", request["email"]);
            Assert.True((bool)request["emailVerified"]);
            Assert.Equal("secret", request["password"]);
            Assert.Equal("+1234567890", request["phoneNumber"]);
            Assert.Equal("https://example.com/user.png", request["photoUrl"]);

            var claims = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>((string)request["customAttributes"]);
            Assert.True((bool)claims["admin"]);
            Assert.Equal(4L, claims["level"]);
            Assert.Equal("gold", claims["package"]);
        }

        [Fact]
        public async Task UpdateUserPartial()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = "user1",
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.True((bool)request["emailVerified"]);
        }

        [Fact]
        public async Task UpdateUserRemoveAttributes()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                DisplayName = null,
                PhotoUrl = null,
                Uid = "user1",
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "DISPLAY_NAME", "PHOTO_URL" },
                request["deleteAttribute"]);
        }

        [Fact]
        public async Task UpdateUserRemoveProviders()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                PhoneNumber = null,
                Uid = "user1",
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "phone" },
                request["deleteProvider"]);
        }

        [Fact]
        public async Task UpdateUserSetCustomClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            });

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Request);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            var claims = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>((string)request["customAttributes"]);
            Assert.True((bool)claims["admin"]);
            Assert.Equal(4L, claims["level"]);
            Assert.Equal("gold", claims["package"]);
        }

        [Fact]
        public async Task LargeClaimsUnderLimit()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "testClaim", new string('a', 950) },
            };

            await userManager.UpdateUserAsync(new UserRecordArgs()
            {
                Uid = "user1",
                CustomClaims = customClaims,
            });
        }

        [Fact]
        public async Task EmptyClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserRecordArgs()
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
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            await userManager.UpdateUserAsync(new UserRecordArgs()
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
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var customClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };

                var args = new UserRecordArgs()
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
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public void UpdateUserInvalidUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = new string('a', 129),
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public async Task UpdateUserEmptyUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserEmptyEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserInvalidEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserEmptyPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserInvalidPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = "1234567890",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Equal(
                "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.",
                exception.Message);
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserEmptyPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserInvalidPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public async Task UpdateUserShortPassword()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await userManager.UpdateUserAsync(args));
            Assert.Null(handler.Request);
        }

        [Fact]
        public void EmptyNameClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var emptyClaims = new Dictionary<string, object>()
            {
                    { string.Empty, "value" },
            };

            var args = new UserRecordArgs()
            {
                Uid = "user1",
                CustomClaims = emptyClaims,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await userManager.UpdateUserAsync(args));
        }

        [Fact]
        public void LargeClaimsOverLimit()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var userManager = this.CreateFirebaseUserManager(handler);
            var largeClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 1001) },
            };

            var args = new UserRecordArgs()
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

            var args = new UserRecordArgs()
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

            var args = new UserRecordArgs()
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
            var args = new UserRecordArgs()
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
