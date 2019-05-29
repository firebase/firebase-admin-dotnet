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

            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
            var userRecord = await userManager.GetUserById("user1");
            Assert.Equal("user1", userRecord.Uid);
        }

        [Fact]
        public async Task GetUserByIdUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
            };
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await userManager.GetUserById("user1"));
        }

        [Fact]
        public async Task ListUsers()
        {
            var nextPageToken = Guid.NewGuid().ToString();
            var handler = new MockMessageHandler()
            {
                Response = new DownloadAccountResponse()
                {
                    NextPageToken = nextPageToken,
                    Users = new List<GetAccountInfoResponse.User>()
                    {
                        new GetAccountInfoResponse.User() { UserId = "user1" },
                        new GetAccountInfoResponse.User() { UserId = "user2" },
                        new GetAccountInfoResponse.User() { UserId = "user3" },
                    },
                },
            };

            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
            var listUsersRequest = userManager.CreateListUserRequest(new ListUsersOptions());
            var userRecords = await listUsersRequest.ExecuteAsync();
            Assert.Equal(nextPageToken, userRecords.NextPageToken);
            Assert.Equal(3, userRecords.Users.Count);
            Assert.Equal("user1", userRecords.Users[0].Uid);
            Assert.Equal("user2", userRecords.Users[1].Uid);
            Assert.Equal("user3", userRecords.Users[2].Uid);
        }

        [Fact]
        public void ListUsersRequestOptionsAreSet()
        {
            var handler = new MockMessageHandler()
            {
            };

            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });

            var listUsersRequest = userManager.CreateListUserRequest(new ListUsersOptions());

            // by default they are set
            Assert.True(listUsersRequest.RequestParameters.ContainsKey("maxResults"));
            Assert.True(listUsersRequest.RequestParameters.ContainsKey("nextPageToken"));
            Assert.Equal(FirebaseUserManager.MaxListUsersResults, int.Parse(listUsersRequest.RequestParameters["maxResults"].DefaultValue));
            Assert.Null(listUsersRequest.RequestParameters["nextPageToken"].DefaultValue);

            // change the values and check again
            listUsersRequest.SetPageSize(10);
            listUsersRequest.SetPageToken("theNewNextPageToken");
            Assert.Equal(10, int.Parse(listUsersRequest.RequestParameters["maxResults"].DefaultValue));
            Assert.Equal("theNewNextPageToken", listUsersRequest.RequestParameters["nextPageToken"].DefaultValue);
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
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
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
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
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
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
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
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
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
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
            await userManager.DeleteUser("user1");
        }

        [Fact]
        public async Task DeleteUserNotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
            };
            var factory = new MockHttpClientFactory(handler);
            var userManager = new FirebaseUserManager(
                new FirebaseUserManagerArgs
                {
                    Credential = MockCredential,
                    ProjectId = MockProjectId,
                    ClientFactory = factory,
                });
            await Assert.ThrowsAsync<FirebaseException>(
               async () => await userManager.DeleteUser("user1"));
        }
    }
}
