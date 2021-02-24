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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Hash;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Jwt.Tests;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Auth.Users.Tests
{
    public class FirebaseUserManagerTest
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { TestConfig.ForFirebaseAuth() },
            new object[] { TestConfig.ForTenantAwareFirebaseAuth("tenant1") },
        };

        private const string CreateUserResponse = @"{""localId"": ""user1""}";

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserById(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.GetUserResponse(),
            };
            var auth = config.CreateAuth(handler);

            var userRecord = await auth.GetUserAsync("user1");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal(config.TenantId, userRecord.TenantId);
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

            config.AssertRequest("accounts:lookup", handler.Requests[0]);
            var request = NewtonsoftJsonSerializer.Instance
                .Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("user1"), request["localId"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByIdWithProperties(TestConfig config)
        {
            var user = @"{
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
            }";
            var handler = new MockMessageHandler()
            {
                Response = config.GetUserResponse(user),
            };
            var auth = config.CreateAuth(handler);

            var userRecord = await auth.GetUserAsync("user1");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal(config.TenantId, userRecord.TenantId);
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

            config.AssertRequest("accounts:lookup", handler.Requests[0]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByIdUserNotFound(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with uid: user1", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByIdNull(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserAsync(null));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByIdEmpty(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByEmail(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.GetUserResponse(),
            };
            var auth = config.CreateAuth(handler);

            var userRecord = await auth.GetUserByEmailAsync("user@example.com");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal(config.TenantId, userRecord.TenantId);
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

            config.AssertRequest("accounts:lookup", handler.Requests[0]);
            var request = NewtonsoftJsonSerializer.Instance
                .Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("user@example.com"), request["email"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByEmailUserNotFound(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserByEmailAsync("user@example.com"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with email: user@example.com", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByEmailNull(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByEmailAsync(null));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByEmailEmpty(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByEmailAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByPhoneNumber(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.GetUserResponse(),
            };
            var auth = config.CreateAuth(handler);

            var userRecord = await auth.GetUserByPhoneNumberAsync("+1234567890");

            Assert.Equal("user1", userRecord.Uid);
            Assert.Equal(config.TenantId, userRecord.TenantId);
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

            config.AssertRequest("accounts:lookup", handler.Requests[0]);
            var request = NewtonsoftJsonSerializer.Instance
                .Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("+1234567890"), request["phoneNumber"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByPhoneNumberUserNotFound(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserByPhoneNumberAsync("+1234567890"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with phone number: +1234567890", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByPhoneNumberNull(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByPhoneNumberAsync(null));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUserByPhoneNumberEmpty(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByPhoneNumberAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUsersExceeds100(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var identifiers = new List<UserIdentifier>();
            for (int i = 0; i < 101; i++)
            {
                identifiers.Add(new UidIdentifier("id" + i));
            }

            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUsersAsync(identifiers));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUsersEmpty(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var identifiers = new List<UserIdentifier>();

            var getUsersResult = await auth.GetUsersAsync(identifiers);
            Assert.Empty(getUsersResult.Users);
            Assert.Empty(getUsersResult.NotFound);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUsersAllNonExisting(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { "{ \"users\": [] }" },
            };
            var auth = config.CreateAuth(handler);

            var notFoundIds = new List<UserIdentifier>()
            {
                new UidIdentifier("id that doesnt exist"),
            };

            var getUsersResult = await auth.GetUsersAsync(notFoundIds);
            Assert.Empty(getUsersResult.Users);
            Assert.Same(notFoundIds[0], getUsersResult.NotFound.Single());
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task GetUsersMultipleIdentifierTypes(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>()
                {
                    @"{
                        'users': [{
                            'localId': 'uid1',
                            'email': 'user1@example.com',
                            'phoneNumber': '+15555550001'
                        }, {
                            'localId': 'uid2',
                            'email': 'user2@example.com',
                            'phoneNumber': '+15555550002'
                        }, {
                            'localId': 'uid3',
                            'email': 'user3@example.com',
                            'phoneNumber': '+15555550003'
                        }, {
                            'localId': 'uid4',
                            'email': 'user4@example.com',
                            'phoneNumber': '+15555550004',
                            'providerUserInfo': [{
                                'providerId': 'google.com',
                                'rawId': 'google_uid4'
                            }]
                        }]
                    }".Replace("'", "\""),
                },
            };

            var doesntExist = new UidIdentifier("this-uid-doesnt-exist");
            var ids = new List<UserIdentifier>
            {
                new UidIdentifier("uid1"),
                new EmailIdentifier("user2@example.com"),
                new PhoneIdentifier("+15555550003"),
                new ProviderIdentifier("google.com", "google_uid4"),
                doesntExist,
            };

            var auth = config.CreateAuth(handler);
            var result = await auth.GetUsersAsync(ids);
            var uids = result.Users.Select(userRecord => userRecord.Uid);
            var expectedUids = new List<string> { "uid1", "uid2", "uid3", "uid4" };
            Assert.True(expectedUids.All(expectedUid => uids.Contains(expectedUid)));
            Assert.Single(result.NotFound);
            Assert.Contains(doesntExist, result.NotFound);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsers(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ExportedUserRecord>();

            var pagedEnumerable = auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                users.Add(enumerator.Current);
                if (users.Count % 3 == 0)
                {
                    Assert.Equal(users.Count / 3, handler.Requests.Count);
                }
            }

            Assert.Equal(6, users.Count);
            Assert.All(users, (user) => Assert.Equal(config.TenantId, user.TenantId));
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=1000", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=1000&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListUsersForEach(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ExportedUserRecord>();

            var pagedEnumerable = auth.ListUsersAsync(null);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                users.Add(user);
                if (users.Count % 3 == 0)
                {
                    Assert.Equal(users.Count / 3, handler.Requests.Count);
                }
            }

            Assert.Equal(6, users.Count);
            Assert.All(users, (user) => Assert.Equal(config.TenantId, user.TenantId));
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=1000", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=1000&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListUsersCustomOptions(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ExportedUserRecord>();
            var customOptions = new ListUsersOptions()
            {
                PageSize = 3,
                PageToken = "custom-token",
            };

            var pagedEnumerable = auth.ListUsersAsync(customOptions);
            foreach (var user in pagedEnumerable.ToEnumerable())
            {
                users.Add(user);
            }

            Assert.Equal(6, users.Count);
            Assert.All(users, (user) => Assert.Equal(config.TenantId, user.TenantId));
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest(
                "accounts:batchGet?maxResults=3&nextPageToken=custom-token", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=3&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersReadPage(TestConfig config)
        {
            var responsePages = config.ListUsersResponse();
            var handler = new MockMessageHandler()
            {
                Response = new List<string>()
                {
                    responsePages[0],
                    responsePages[0],
                },
            };
            var auth = config.CreateAuth(handler);

            // Retrieve a page of users.
            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal("token", userPage.NextPageToken);
            Assert.Equal(3, userPage.Count());
            Assert.Equal(1, handler.Requests.Count);
            config.AssertRequest(
                "accounts:batchGet?maxResults=3", Assert.Single(handler.Requests));

            // Retrieve the same page of users again.
            userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal("token", userPage.NextPageToken);
            Assert.Equal(3, userPage.Count());

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=3", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersByPages(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ExportedUserRecord>();

            // Read page 1.
            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, userPage.Count());
            Assert.Equal("token", userPage.NextPageToken);
            config.AssertRequest(
                "accounts:batchGet?maxResults=3", Assert.Single(handler.Requests));
            users.AddRange(userPage);

            // Read page 2.
            pagedEnumerable = auth.ListUsersAsync(new ListUsersOptions()
            {
                PageToken = userPage.NextPageToken,
            });
            userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, userPage.Count());
            Assert.Null(userPage.NextPageToken);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest(
                "accounts:batchGet?maxResults=3&nextPageToken=token", handler.Requests[1]);
            users.AddRange(userPage);

            Assert.Equal(6, users.Count);
            Assert.All(users, (user) => Assert.Equal(config.TenantId, user.TenantId));
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersReadLargePageSize(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(10);

            Assert.Null(userPage.NextPageToken);
            Assert.Equal(6, userPage.Count());

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=10", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=7&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersAsRawResponses(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ExportedUserRecord>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.ListUsersAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetAsyncEnumerator();
            while (await responses.MoveNextAsync())
            {
                users.AddRange(responses.Current.Users);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(2, tokens.Count);
            Assert.Equal("token", tokens[0]);
            Assert.Null(tokens[1]);

            Assert.Equal(6, users.Count);
            Assert.All(users, (user) => Assert.Equal(config.TenantId, user.TenantId));
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=1000", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=1000&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersReadPageSizeTooLarge(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var pagedEnumerable = auth.ListUsersAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(1001));

            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListUsersOptionsPageSizeTooLarge(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var options = new ListUsersOptions()
            {
                PageSize = 1001,
            };

            Assert.Throws<ArgumentException>(() => auth.ListUsersAsync(options));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListUsersOptionsPageSizeTooSmall(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);

            foreach (var pageSize in new int[] { 0, -1 })
            {
                var options = new ListUsersOptions()
                {
                    PageSize = pageSize,
                };

                Assert.Throws<ArgumentException>(() => auth.ListUsersAsync(options));
                Assert.Empty(handler.Requests);
            }
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ListUsersOptionsPageTokenEmpty(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var options = new ListUsersOptions()
            {
                PageToken = string.Empty,
            };

            Assert.Throws<ArgumentException>(() => auth.ListUsersAsync(options));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersHttpError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await pagedEnumerable.FirstAsync());

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{{}}",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);

            config.AssertRequest(
                "accounts:batchGet?maxResults=1000", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersIntermittentHttpError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = config.ListUsersResponse(),
            };
            var auth = config.CreateAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            for (int i = 0; i < 3; i++)
            {
                Assert.True(await enumerator.MoveNextAsync());
            }

            handler.StatusCode = HttpStatusCode.InternalServerError;
            handler.Response = "{}";
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await enumerator.MoveNextAsync());

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{{}}",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);

            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:batchGet?maxResults=1000", handler.Requests[0]);
            config.AssertRequest(
                "accounts:batchGet?maxResults=1000&nextPageToken=token", handler.Requests[1]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ListUsersNonJsonResponse(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "not json",
            };
            var auth = config.CreateAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await pagedEnumerable.FirstAsync());

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                $"Error while parsing Auth service response. {exception.InnerException.Message}: not json",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);

            config.AssertRequest(
                "accounts:batchGet?maxResults=1000", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUser(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string> { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.CreateUserAsync(new UserRecordArgs());

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Empty(request);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserWithArgs(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string> { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.CreateUserAsync(new UserRecordArgs()
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

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.True((bool)request["disabled"]);
            Assert.Equal("Test User", request["displayName"]);
            Assert.Equal("user@example.com", request["email"]);
            Assert.True((bool)request["emailVerified"]);
            Assert.Equal("secret", request["password"]);
            Assert.Equal("+1234567890", request["phoneNumber"]);
            Assert.Equal("https://example.com/user.png", request["photoUrl"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserWithExplicitDefaults(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string> { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.CreateUserAsync(new UserRecordArgs()
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

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            Assert.False((bool)request["disabled"]);
            Assert.False((bool)request["emailVerified"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserEmptyUid(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(new MockMessageHandler());
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserLongUid(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = new string('a', 129),
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserEmptyEmail(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserInvalidEmail(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserEmptyPhoneNumber(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserInvalidPhoneNumber(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = "1234567890",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Equal(
                "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.",
                exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserEmptyPhotoUrl(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserInvalidPhotoUrl(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserShortPassword(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task CreateUserIncorrectResponse(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.CreateUserAsync(args));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal("Failed to create new user.", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUser(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
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

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:update", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
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

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserPartial(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:update", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.True((bool)request["emailVerified"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserRemoveAttributes(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                DisplayName = null,
                PhotoUrl = null,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:update", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "DISPLAY_NAME", "PHOTO_URL" },
                request["deleteAttribute"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserRemoveProviders(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, config.GetUserResponse() },
            };
            var auth = config.CreateAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                PhoneNumber = null,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(config.TenantId, user.TenantId);
            Assert.Equal(2, handler.Requests.Count);
            config.AssertRequest("accounts:update", handler.Requests[0]);
            config.AssertRequest("accounts:lookup", handler.Requests[1]);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "phone" },
                request["deleteProvider"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserSetCustomClaims(TestConfig config)
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = config.CreateAuth(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            await auth.SetCustomUserClaimsAsync("user1", customClaims);

            config.AssertRequest("accounts:update", Assert.Single(handler.Requests));
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            var claims = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>((string)request["customAttributes"]);
            Assert.True((bool)claims["admin"]);
            Assert.Equal(4L, claims["level"]);
            Assert.Equal("gold", claims["package"]);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task LargeClaimsUnderLimit(TestConfig config)
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = config.CreateAuth(handler);
            var customClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 950) },
            };

            await auth.SetCustomUserClaimsAsync("user1", customClaims);

            config.AssertRequest("accounts:update", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task EmptyClaims(TestConfig config)
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = config.CreateAuth(handler);

            await auth.SetCustomUserClaimsAsync("user1", new Dictionary<string, object>());

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);

            config.AssertRequest("accounts:update", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task NullClaims(TestConfig config)
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = config.CreateAuth(handler);

            await auth.SetCustomUserClaimsAsync("user1", null);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);

            config.AssertRequest("accounts:update", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void ReservedClaims(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);

            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var customClaims = new Dictionary<string, object>()
                {
                    { key, "value" },
                };

                Assert.ThrowsAsync<ArgumentException>(
                    async () => await auth.SetCustomUserClaimsAsync("user1", customClaims));
            }
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void UpdateUserNoUid(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void UpdateUserInvalidUid(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = new string('a', 129),
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserEmptyUid(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserEmptyEmail(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserInvalidEmail(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserEmptyPhoneNumber(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserInvalidPhoneNumber(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = "1234567890",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Equal(
                "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.",
                exception.Message);
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserEmptyPhotoUrl(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserInvalidPhotoUrl(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserShortPassword(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void EmptyNameClaims(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var emptyClaims = new Dictionary<string, object>()
            {
                    { string.Empty, "value" },
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.SetCustomUserClaimsAsync("user1", emptyClaims));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void LargeClaimsOverLimit(TestConfig config)
        {
            var handler = new MockMessageHandler();
            var auth = config.CreateAuth(handler);
            var largeClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 1001) },
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.SetCustomUserClaimsAsync("user1", largeClaims));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserIncorrectResponseObject(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.UpdateUserAsync(args));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal("Failed to update user: user1", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserIncorrectResponseUid(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""notuser1""}",
            };
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.UpdateUserAsync(args));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal("Failed to update user: user1", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task UpdateUserHttpError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.UpdateUserAsync(args));

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{{}}",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUser(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""kind"": ""identitytoolkit#DeleteAccountResponse""}",
            };
            var auth = config.CreateAuth(handler);

            await auth.DeleteUserAsync("user1");

            config.AssertRequest("accounts:delete", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUserNotFound(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
               async () => await auth.DeleteUserAsync("user1"));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal(
                "No user record found for the given identifier (USER_NOT_FOUND).",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUsersExceeds1000(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var uids = new List<string>();
            for (int i = 0; i < 1001; i++)
            {
                uids.Add("id" + i);
            }

            await Assert.ThrowsAsync<ArgumentException>(() => auth.DeleteUsersAsync(uids));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUsersInvalidId(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var uids = new List<string>() { "too long " + new string('.', 128) };

            await Assert.ThrowsAsync<ArgumentException>(
                    () => auth.DeleteUsersAsync(uids));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUsersIndexesErrorsCorrectly(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>()
                {
                    @"{
                        'errors': [{
                            'index': 0,
                            'localId': 'uid1',
                            'message': 'NOT_DISABLED : Disable the account before batch deletion.'
                        }, {
                            'index': 2,
                            'localId': 'uid3',
                            'message': 'something awful'
                        }]
                    }".Replace("'", "\""),
                },
            };

            var auth = config.CreateAuth(handler);
            var deleteUsersResult = await auth.DeleteUsersAsync(
                    new List<string> { "uid1", "uid2", "uid3", "uid4" });

            Assert.Equal(2, deleteUsersResult.SuccessCount);
            Assert.Equal(2, deleteUsersResult.FailureCount);
            Assert.Equal(2, deleteUsersResult.Errors.Count);
            Assert.Equal(0, deleteUsersResult.Errors[0].Index);
            Assert.Equal(
                    "NOT_DISABLED : Disable the account before batch deletion.",
                    deleteUsersResult.Errors[0].Reason);
            Assert.Equal(2, deleteUsersResult.Errors[1].Index);
            Assert.Equal("something awful", deleteUsersResult.Errors[1].Reason);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task DeleteUsersSuccess(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { "{}" },
            };

            var auth = config.CreateAuth(handler);
            DeleteUsersResult result = await auth.DeleteUsersAsync(
                    new List<string> { "uid1", "uid2", "uid3" });

            Assert.Equal(3, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task RevokeRefreshTokens(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = CreateUserResponse,
            };
            var auth = config.CreateAuth(handler);

            await auth.RevokeRefreshTokensAsync("user1");

            Assert.Equal(1, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(TestConfig.Clock.UnixTimestamp(), request["validSince"]);

            config.AssertRequest("accounts:update", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void RevokeRefreshTokensNoUid(TestConfig config)
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = config.CreateAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(null));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(string.Empty));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public void RevokeRefreshTokensInvalidUid(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());

            var uid = new string('a', 129);
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(uid));
        }

        [Fact]
        public void CreateSessionCookieNoIdToken()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = (FirebaseAuth)config.CreateAuth(handler);
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromHours(1),
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync(null, options));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync(string.Empty, options));
        }

        [Fact]
        public void CreateSessionCookieNoOptions()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = (FirebaseAuth)config.CreateAuth(handler);

            Assert.ThrowsAsync<ArgumentNullException>(
                async () => await auth.CreateSessionCookieAsync("idToken", null));
        }

        [Fact]
        public void CreateSessionCookieNoExpiresIn()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = (FirebaseAuth)config.CreateAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync(
                    "idToken", new SessionCookieOptions()));
        }

        [Fact]
        public async Task CreateSessionCookieExpiresInTooLow()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = (FirebaseAuth)config.CreateAuth(handler);
            var fiveMinutesInSeconds = TimeSpan.FromMinutes(5).TotalSeconds;
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromSeconds(fiveMinutesInSeconds - 1),
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync("idToken", options));
        }

        [Fact]
        public async Task CreateSessionCookieExpiresInTooHigh()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = (FirebaseAuth)config.CreateAuth(handler);
            var fourteenDaysInSeconds = TimeSpan.FromDays(14).TotalSeconds;
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromSeconds(fourteenDaysInSeconds + 1),
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync("idToken", options));
        }

        [Fact]
        public async Task CreateSessionCookie()
        {
            var config = TestConfig.ForFirebaseAuth();
            var handler = new MockMessageHandler()
            {
                Response = @"{
                    ""sessionCookie"": ""cookie""
                }",
            };
            var auth = (FirebaseAuth)config.CreateAuth(handler);
            var idToken = await CreateIdTokenAsync(config.TenantId);
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromHours(1),
            };

            var result = await auth.CreateSessionCookieAsync(idToken, options);

            Assert.Equal("cookie", result);
            Assert.Equal(1, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal(idToken, request["idToken"]);
            Assert.Equal(3600, request["validDuration"]);

            config.AssertRequest(":createSessionCookie", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsers(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);
            var users = new List<ImportUserRecordArgs>()
            {
                new ImportUserRecordArgs() { Uid = "user1" },
                new ImportUserRecordArgs() { Uid = "user2" },
            };

            var result = await auth.ImportUsersAsync(users);

            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);

            config.AssertRequest("accounts:batchCreate", Assert.Single(handler.Requests));
            var expected = new JObject()
            {
                {
                    "users", new JArray()
                    {
                        new JObject() { { "localId", "user1" } },
                        new JObject() { { "localId", "user2" } },
                    }
                },
            };
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.True(JToken.DeepEquals(expected, request));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{
                    ""error"": [
                        {""index"": 1, ""message"": ""test error""}
                    ]
                }",
            };
            var auth = config.CreateAuth(handler);
            var usersList = new List<ImportUserRecordArgs>()
            {
                new ImportUserRecordArgs() { Uid = "user1" },
                new ImportUserRecordArgs() { Uid = "user2" },
            };

            var result = await auth.ImportUsersAsync(usersList);

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.Index);
            Assert.Equal("test error", error.Reason);

            config.AssertRequest("accounts:batchCreate", Assert.Single(handler.Requests));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersWithPassword(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);
            var password = Encoding.UTF8.GetBytes("password");
            var usersList = new List<ImportUserRecordArgs>()
            {
                new ImportUserRecordArgs() { Uid = "user1" },
                new ImportUserRecordArgs
                {
                    Uid = "user2",
                    PasswordHash = password,
                },
            };

            var result = await auth.ImportUsersAsync(usersList, new UserImportOptions()
            {
                Hash = new Bcrypt(),
            });

            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);

            config.AssertRequest("accounts:batchCreate", Assert.Single(handler.Requests));
            var expected = new JObject()
            {
                {
                    "users", new JArray()
                    {
                        new JObject() { { "localId", "user1" } },
                        new JObject()
                        {
                            { "localId", "user2" },
                            { "passwordHash", JwtUtils.UrlSafeBase64Encode(password) },
                        },
                    }
                },
                { "hashAlgorithm", "BCRYPT" },
            };
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(
                handler.LastRequestBody);
            Assert.True(JToken.DeepEquals(expected, request));
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersMissingHash(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var usersList = new List<ImportUserRecordArgs>()
            {
                new ImportUserRecordArgs() { Uid = "user1" },
                new ImportUserRecordArgs
                {
                    Uid = "user2",
                    PasswordHash = Encoding.UTF8.GetBytes("password"),
                },
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.ImportUsersAsync(usersList));

            Assert.Equal(
                "UserImportHash option is required when at least one user has a password.",
                exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersEmpty(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.ImportUsersAsync(new List<ImportUserRecordArgs>()));

            Assert.Equal("Users must not be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersNull(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.ImportUsersAsync(null));

            Assert.Equal("Users must not be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ImportUsersExceedLimit(TestConfig config)
        {
            var auth = config.CreateAuth(new MockMessageHandler());
            var users = new List<ImportUserRecordArgs>();
            for (int i = 0; i < 1001; i++)
            {
                users.Add(new ImportUserRecordArgs() { Uid = $"user{i}" });
            }

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => auth.ImportUsersAsync(users));

            Assert.Equal("Users list must not contain more than 1000 items.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task ServiceUnvailable(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.Unavailable, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        [Theory]
        [MemberData(nameof(TestConfigs))]
        public async Task TransportError(TestConfig config)
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Transport error"),
            };
            var auth = config.CreateAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Null(exception.HttpResponse);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public void NoProjectId()
        {
            var args = CreateArgs();
            args.ProjectId = null;

            Assert.Throws<ArgumentException>(() => new FirebaseUserManager(args));
        }

        [Fact]
        public void EmptyTenantId()
        {
            var args = CreateArgs();
            args.TenantId = string.Empty;

            Assert.Throws<ArgumentException>(() => new FirebaseUserManager(args));
        }

        private static FirebaseUserManager.Args CreateArgs()
        {
            return new FirebaseUserManager.Args
            {
                ProjectId = TestConfig.MockProjectId,
                ClientFactory = new MockHttpClientFactory(new MockMessageHandler()),
                RetryOptions = RetryOptions.NoBackOff,
                Clock = TestConfig.Clock,
            };
        }

        private static async Task<string> CreateIdTokenAsync(string tenantId)
        {
            var tokenBuilder = JwtTestUtils.IdTokenBuilder(tenantId);
            tokenBuilder.ProjectId = TestConfig.MockProjectId;
            return await tokenBuilder.CreateTokenAsync();
        }

        public class TestConfig
        {
            internal const string MockProjectId = "project1";

            internal static readonly IClock Clock = new MockClock();

            private readonly AuthBuilder authBuilder;

            private TestConfig(string tenantId = null)
            {
                this.authBuilder = new AuthBuilder
                {
                    ProjectId = MockProjectId,
                    Clock = Clock,
                    RetryOptions = RetryOptions.NoBackOff,
                    KeySource = JwtTestUtils.DefaultKeySource,
                    TenantId = tenantId,
                };
            }

            public string TenantId => this.authBuilder.TenantId;

            public static TestConfig ForFirebaseAuth()
            {
                return new TestConfig();
            }

            public static TestConfig ForTenantAwareFirebaseAuth(string tenantId)
            {
                return new TestConfig(tenantId);
            }

            public AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler = null)
            {
                var options = new TestOptions
                {
                    UserManagerRequestHandler = handler,
                };
                return this.authBuilder.Build(options);
            }

            public string GetUserResponse(string response = null)
            {
                var user = this.GetUserResponseDictionary(response);
                var fullResponse = new Dictionary<string, object>
                {
                    { "users", new List<object>() { user } },
                };
                return NewtonsoftJsonSerializer.Instance.Serialize(fullResponse);
            }

            public IList<string> ListUsersResponse()
            {
                var page1 = new Dictionary<string, object>
                {
                    { "nextPageToken", "token" },
                    {
                        "users",
                        new List<IDictionary<string, object>>
                        {
                            this.GetUserResponseDictionary(@"{""localId"": ""user1""}"),
                            this.GetUserResponseDictionary(@"{""localId"": ""user2""}"),
                            this.GetUserResponseDictionary(@"{""localId"": ""user3""}"),
                        }
                    },
                };
                var page2 = new Dictionary<string, object>
                {
                    {
                        "users",
                        new List<IDictionary<string, object>>
                        {
                            this.GetUserResponseDictionary(@"{""localId"": ""user4""}"),
                            this.GetUserResponseDictionary(@"{""localId"": ""user5""}"),
                            this.GetUserResponseDictionary(@"{""localId"": ""user6""}"),
                        }
                    },
                };

                return new List<string>
                {
                    NewtonsoftJsonSerializer.Instance.Serialize(page1),
                    NewtonsoftJsonSerializer.Instance.Serialize(page2),
                };
            }

            internal void AssertRequest(
                string expectedSuffix, MockMessageHandler.IncomingRequest request)
            {
                var tenantInfo = this.TenantId != null ? $"/tenants/{this.TenantId}" : string.Empty;
                var expectedPath = $"/v1/projects/{MockProjectId}{tenantInfo}/{expectedSuffix}";
                Assert.Equal(expectedPath, request.Url.PathAndQuery);
            }

            private IDictionary<string, object> GetUserResponseDictionary(string response = null)
            {
                IDictionary<string, object> user;
                if (response != null)
                {
                    user = NewtonsoftJsonSerializer.Instance
                        .Deserialize<Dictionary<string, object>>(response);
                }
                else
                {
                    user = new Dictionary<string, object>
                    {
                        { "localId", "user1" },
                    };
                }

                if (this.TenantId != null)
                {
                    user["tenantId"] = this.TenantId;
                }

                return user;
            }
        }
    }
}
