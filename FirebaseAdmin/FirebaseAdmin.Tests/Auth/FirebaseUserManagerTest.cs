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
using System.Net.Http.Headers;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseUserManagerTest
    {
        private const string MockProjectId = "project1";
        private const string CreateUserResponse = @"{""localId"": ""user1""}";
        private const string GetUserResponse = @"{""users"": [{""localId"": ""user1""}]}";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        private static readonly IClock MockClock = new MockClock();

        private static readonly IList<string> ListUsersResponse = new List<string>()
        {
            @"{
                ""nextPageToken"": ""token"",
                ""users"": [
                    {""localId"": ""user1""},
                    {""localId"": ""user2""},
                    {""localId"": ""user3""}
                ]
            }",
            @"{
                ""users"": [
                    {""localId"": ""user4""},
                    {""localId"": ""user5""},
                    {""localId"": ""user6""}
                ]
            }",
        };

        [Fact]
        public async Task GetUserById()
        {
            var handler = new MockMessageHandler()
            {
                Response = GetUserResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var userRecord = await auth.GetUserAsync("user1");

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

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("user1"), request["localId"]);
            this.AssertClientVersion(handler.LastRequestHeaders);
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
            var auth = this.CreateFirebaseAuth(handler);

            var userRecord = await auth.GetUserAsync("user1");

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

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task GetUserByIdUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with uid: user1", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task GetUserByIdNull()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserAsync(null));
        }

        [Fact]
        public async Task GetUserByIdEmpty()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserAsync(string.Empty));
        }

        [Fact]
        public async Task GetUserByEmail()
        {
            var handler = new MockMessageHandler()
            {
                Response = GetUserResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var userRecord = await auth.GetUserByEmailAsync("user@example.com");

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

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("user@example.com"), request["email"]);
            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task GetUserByEmailUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserByEmailAsync("user@example.com"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with email: user@example.com", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task GetUserByEmailNull()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByEmailAsync(null));
        }

        [Fact]
        public async Task GetUserByEmailEmpty()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByEmailAsync(string.Empty));
        }

        [Fact]
        public async Task GetUserByPhoneNumber()
        {
            var handler = new MockMessageHandler()
            {
                Response = GetUserResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var userRecord = await auth.GetUserByPhoneNumberAsync("+1234567890");

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

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(handler.LastRequestBody);
            Assert.Equal(new JArray("+1234567890"), request["phoneNumber"]);
            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task GetUserByPhoneNumberUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""users"": []}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserByPhoneNumberAsync("+1234567890"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UserNotFound, exception.AuthErrorCode);
            Assert.Equal("Failed to get user with phone number: +1234567890", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task GetUserByPhoneNumberNull()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByPhoneNumberAsync(null));
        }

        [Fact]
        public async Task GetUserByPhoneNumberEmpty()
        {
            var auth = this.CreateFirebaseAuth(new MockMessageHandler());
            await Assert.ThrowsAsync<ArgumentException>(() => auth.GetUserByPhoneNumberAsync(string.Empty));
        }

        [Fact]
        public async Task ListUsers()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var users = new List<ExportedUserRecord>();

            var pagedEnumerable = auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                users.Add(enumerator.Current);
                if (users.Count % 3 == 0)
                {
                    Assert.Equal(users.Count / 3, handler.Requests.Count);
                }
            }

            Assert.Equal(6, users.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("1000", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public void ListUsersForEach()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
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
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("1000", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public void ListUsersCustomOptions()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
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
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["maxResults"]);
            Assert.Equal("custom-token", query["nextPageToken"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task ListUsersReadPage()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>()
                {
                    ListUsersResponse[0],
                    ListUsersResponse[0],
                },
            };
            var auth = this.CreateFirebaseAuth(handler);

            // Retrieve a page of users.
            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal("token", userPage.NextPageToken);
            Assert.Equal(3, userPage.Count());
            Assert.Equal(1, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("3", query["maxResults"]);

            // Retrieve the same page of users again.
            userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal("token", userPage.NextPageToken);
            Assert.Equal(3, userPage.Count());
            Assert.Equal(2, handler.Requests.Count);
            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Single(query);
            Assert.Equal("3", query["maxResults"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task ListUsersByPages()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var users = new List<ExportedUserRecord>();

            // Read page 1.
            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(3);

            Assert.Equal(3, userPage.Count());
            Assert.Equal("token", userPage.NextPageToken);
            Assert.Single(handler.Requests);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("3", query["maxResults"]);
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
            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("3", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);
            users.AddRange(userPage);

            Assert.Equal(6, users.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task ListUsersReadLargePageSize()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var userPage = await pagedEnumerable.ReadPageAsync(10);

            Assert.Null(userPage.NextPageToken);
            Assert.Equal(6, userPage.Count());
            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("10", query["maxResults"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("7", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task ListUsersAsRawResponses()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var users = new List<ExportedUserRecord>();
            var tokens = new List<string>();

            var pagedEnumerable = auth.ListUsersAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetEnumerator();
            while (await responses.MoveNext())
            {
                users.AddRange(responses.Current.Users);
                tokens.Add(responses.Current.NextPageToken);
                Assert.Equal(tokens.Count, handler.Requests.Count);
            }

            Assert.Equal(2, tokens.Count);
            Assert.Equal("token", tokens[0]);
            Assert.Null(tokens[1]);

            Assert.Equal(6, users.Count);
            for (int i = 0; i < 6; i++)
            {
                Assert.Equal($"user{i + 1}", users[i].Uid);
            }

            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("1000", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task ListUsersReadPageSizeTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var pagedEnumerable = auth.ListUsersAsync(null);

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await pagedEnumerable.ReadPageAsync(1001));

            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void ListUsersOptionsPageSizeTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var options = new ListUsersOptions()
            {
                PageSize = 1001,
            };

            Assert.Throws<ArgumentException>(() => auth.ListUsersAsync(options));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void ListUsersOptionsPageSizeTooSmall()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

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

        [Fact]
        public void ListUsersOptionsPageTokenEmpty()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);
            var options = new ListUsersOptions()
            {
                PageToken = string.Empty,
            };

            Assert.Throws<ArgumentException>(() => auth.ListUsersAsync(options));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task ListUsersHttpError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await pagedEnumerable.First());

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{{}}",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);

            Assert.Single(handler.Requests);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);
        }

        [Fact]
        public async Task ListUsersIntermittentHttpError()
        {
            var handler = new MockMessageHandler()
            {
                Response = ListUsersResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            for (int i = 0; i < 3; i++)
            {
                Assert.True(await enumerator.MoveNext());
            }

            handler.StatusCode = HttpStatusCode.InternalServerError;
            handler.Response = "{}";
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await enumerator.MoveNext());

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{{}}",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);

            Assert.Equal(2, handler.Requests.Count);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);

            query = this.ExtractQueryParams(handler.Requests[1]);
            Assert.Equal(2, query.Count);
            Assert.Equal("1000", query["maxResults"]);
            Assert.Equal("token", query["nextPageToken"]);
        }

        [Fact]
        public async Task ListUsersNonJsonResponse()
        {
            var handler = new MockMessageHandler()
            {
                Response = "not json",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var pagedEnumerable = auth.ListUsersAsync(null);
            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await pagedEnumerable.First());

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(
                $"Error while parsing Auth service response. {exception.InnerException.Message}: not json",
                exception.Message);
            Assert.NotNull(exception.HttpResponse);

            Assert.Single(handler.Requests);
            var query = this.ExtractQueryParams(handler.Requests[0]);
            Assert.Single(query);
            Assert.Equal("1000", query["maxResults"]);
        }

        [Fact]
        public async Task CreateUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

            var user = await auth.CreateUserAsync(new UserRecordArgs());

            Assert.Equal("user1", user.Uid);
            Assert.Equal(2, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Empty(request);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task CreateUserWithArgs()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

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
            Assert.Equal(2, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.True((bool)request["disabled"]);
            Assert.Equal("Test User", request["displayName"]);
            Assert.Equal("user@example.com", request["email"]);
            Assert.True((bool)request["emailVerified"]);
            Assert.Equal("secret", request["password"]);
            Assert.Equal("+1234567890", request["phoneNumber"]);
            Assert.Equal("https://example.com/user.png", request["photoUrl"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task CreateUserWithExplicitDefaults()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

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
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.False((bool)request["disabled"]);
            Assert.False((bool)request["emailVerified"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task CreateUserEmptyUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserLongUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = new string('a', 129),
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserEmptyEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserInvalidEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserEmptyPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserInvalidPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task CreateUserEmptyPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserInvalidPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserShortPassword()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task CreateUserIncorrectResponse()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs();

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.CreateUserAsync(args));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, exception.AuthErrorCode);
            Assert.Equal("Failed to create new user.", exception.Message);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task UpdateUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);
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
            Assert.Equal(2, handler.Requests.Count);
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

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task UpdateUserPartial()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(2, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.True((bool)request["emailVerified"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task UpdateUserRemoveAttributes()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                DisplayName = null,
                PhotoUrl = null,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(2, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "DISPLAY_NAME", "PHOTO_URL" },
                request["deleteAttribute"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task UpdateUserRemoveProviders()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { CreateUserResponse, GetUserResponse },
            };
            var auth = this.CreateFirebaseAuth(handler);

            var user = await auth.UpdateUserAsync(new UserRecordArgs()
            {
                PhoneNumber = null,
                Uid = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Equal(2, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.Requests[0].Body);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(
                new JArray() { "phone" },
                request["deleteProvider"]);

            this.AssertClientVersion(handler.Requests[0].Headers);
            this.AssertClientVersion(handler.Requests[1].Headers);
        }

        [Fact]
        public async Task UpdateUserSetCustomClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "admin", true },
                    { "level", 4 },
                    { "package", "gold" },
            };

            await auth.SetCustomUserClaimsAsync("user1", customClaims);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            var claims = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>((string)request["customAttributes"]);
            Assert.True((bool)claims["admin"]);
            Assert.Equal(4L, claims["level"]);
            Assert.Equal("gold", claims["package"]);

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task LargeClaimsUnderLimit()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var customClaims = new Dictionary<string, object>()
            {
                    { "testClaim", new string('a', 950) },
            };

            await auth.SetCustomUserClaimsAsync("user1", customClaims);
            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task EmptyClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            await auth.SetCustomUserClaimsAsync("user1", new Dictionary<string, object>());

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task NullClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            await auth.SetCustomUserClaimsAsync("user1", null);

            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal("{}", request["customAttributes"]);

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public void ReservedClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

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

        [Fact]
        public void UpdateUserNoUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
        }

        [Fact]
        public void UpdateUserInvalidUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var args = new UserRecordArgs()
            {
                EmailVerified = true,
                Uid = new string('a', 129),
            };
            Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
        }

        [Fact]
        public async Task UpdateUserEmptyUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Uid = string.Empty,
            };

            await Assert.ThrowsAsync<ArgumentException>(async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserEmptyEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserInvalidEmail()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Email = "not-an-email",
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserEmptyPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhoneNumber = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserInvalidPhoneNumber()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task UpdateUserEmptyPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = string.Empty,
                Uid = "user1",
            };

            await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserInvalidPhotoUrl()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                PhotoUrl = "not a url",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public async Task UpdateUserShortPassword()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var args = new UserRecordArgs()
            {
                Password = "only5",
                Uid = "user1",
            };

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.UpdateUserAsync(args));
            Assert.Empty(handler.Requests);
        }

        [Fact]
        public void EmptyNameClaims()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var emptyClaims = new Dictionary<string, object>()
            {
                    { string.Empty, "value" },
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.SetCustomUserClaimsAsync("user1", emptyClaims));
        }

        [Fact]
        public void LargeClaimsOverLimit()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);
            var largeClaims = new Dictionary<string, object>()
            {
                { "testClaim", new string('a', 1001) },
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.SetCustomUserClaimsAsync("user1", largeClaims));
        }

        [Fact]
        public async Task UpdateUserIncorrectResponseObject()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var auth = this.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task UpdateUserIncorrectResponseUid()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""localId"": ""notuser1""}",
            };
            var auth = this.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task UpdateUserHttpError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var auth = this.CreateFirebaseAuth(handler);
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

        [Fact]
        public async Task DeleteUser()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""kind"": ""identitytoolkit#DeleteAccountResponse""}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            await auth.DeleteUserAsync("user1");
            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task DeleteUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = @"{
                    ""error"": {""message"": ""USER_NOT_FOUND""}
                }",
            };
            var auth = this.CreateFirebaseAuth(handler);

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

        [Fact]
        public async Task RevokeRefreshTokens()
        {
            var handler = new MockMessageHandler()
            {
                Response = CreateUserResponse,
            };
            var auth = this.CreateFirebaseAuth(handler);

            await auth.RevokeRefreshTokensAsync("user1");

            Assert.Equal(1, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal("user1", request["localId"]);
            Assert.Equal(MockClock.UnixTimestamp(), request["validSince"]);

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public void RevokeRefreshTokensNoUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(null));
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(string.Empty));
        }

        [Fact]
        public void RevokeRefreshTokensInvalidUid()
        {
            var handler = new MockMessageHandler() { Response = CreateUserResponse };
            var auth = this.CreateFirebaseAuth(handler);

            var uid = new string('a', 129);
            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.RevokeRefreshTokensAsync(uid));
        }

        [Fact]
        public void CreateSessionCookieNoIdToken()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);
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
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);

            Assert.ThrowsAsync<ArgumentNullException>(
                async () => await auth.CreateSessionCookieAsync("idToken", null));
        }

        [Fact]
        public void CreateSessionCookieNoExpiresIn()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync(
                    "idToken", new SessionCookieOptions()));
        }

        [Fact]
        public void CreateSessionCookieExpiresInTooLow()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);
            var fiveMinutesInSeconds = TimeSpan.FromMinutes(5).TotalSeconds;
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromSeconds(fiveMinutesInSeconds - 1),
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync("idToken", options));
        }

        [Fact]
        public void CreateSessionCookieExpiresInTooHigh()
        {
            var handler = new MockMessageHandler() { Response = "{}" };
            var auth = this.CreateFirebaseAuth(handler);
            var fourteenDaysInSeconds = TimeSpan.FromDays(14).TotalSeconds;
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromSeconds(fourteenDaysInSeconds + 1),
            };

            Assert.ThrowsAsync<ArgumentException>(
                async () => await auth.CreateSessionCookieAsync("idToken", options));
        }

        [Fact]
        public async Task CreateSessionCookie()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{
                    ""sessionCookie"": ""cookie""
                }",
            };
            var auth = this.CreateFirebaseAuth(handler);
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromHours(1),
            };

            var result = await auth.CreateSessionCookieAsync("idToken", options);

            Assert.Equal("cookie", result);
            Assert.Equal(1, handler.Requests.Count);
            var request = NewtonsoftJsonSerializer.Instance.Deserialize<JObject>(handler.LastRequestBody);
            Assert.Equal(2, request.Count);
            Assert.Equal("idToken", request["idToken"]);
            Assert.Equal(3600, request["validDuration"]);

            this.AssertClientVersion(handler.LastRequestHeaders);
        }

        [Fact]
        public async Task ServiceUnvailable()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.Unavailable, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public async Task TransportError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Transport error"),
            };
            var auth = this.CreateFirebaseAuth(handler);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await auth.GetUserAsync("user1"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Null(exception.AuthErrorCode);
            Assert.Null(exception.HttpResponse);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        private FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler)
        {
            var userManager = new FirebaseUserManager(new FirebaseUserManager.Args
            {
                Credential = MockCredential,
                ProjectId = MockProjectId,
                ClientFactory = new MockHttpClientFactory(handler),
                RetryOptions = RetryOptions.NoBackOff,
                Clock = MockClock,
            });
            return new FirebaseAuth(new FirebaseAuth.FirebaseAuthArgs()
            {
                UserManager = new Lazy<FirebaseUserManager>(userManager),
                TokenFactory = new Lazy<FirebaseTokenFactory>(),
                IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(),
                SessionCookieVerifier = new Lazy<FirebaseTokenVerifier>(),
            });
        }

        private IDictionary<string, string> ExtractQueryParams(MockMessageHandler.IncomingRequest req)
        {
            return req.Url.Query.Substring(1).Split('&').ToDictionary(
                entry => entry.Split('=')[0], entry => entry.Split('=')[1]);
        }

        private void AssertClientVersion(HttpRequestHeaders header)
        {
            Assert.Equal(
                FirebaseUserManager.ClientVersion,
                header.GetValues(FirebaseUserManager.ClientVersionHeader).First());
        }
    }
}
