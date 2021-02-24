// Copyright 2020, Google Inc. All rights reserved.
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
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class GetUsersTest : IClassFixture<GetUsersFixture>
    {
        private readonly GetUsersFixture fixture;

        public GetUsersTest(GetUsersFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void VariousIdentifiers()
        {
            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>()
                {
                    new UidIdentifier(this.fixture.TestUser1.Uid),
                    new EmailIdentifier(this.fixture.TestUser2.Email),
                    new PhoneIdentifier(this.fixture.TestUser3.PhoneNumber),
                    new ProviderIdentifier("google.com", $"google_{this.fixture.ImportUserUid}"),
                });

            var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
            var expectedUids = new List<string>()
            {
                this.fixture.TestUser1.Uid,
                this.fixture.TestUser2.Uid,
                this.fixture.TestUser3.Uid,
                this.fixture.ImportUserUid,
            };
            Assert.Equal(expectedUids.Count(), uids.Count());
            Assert.All(expectedUids, expectedUid => uids.Contains(expectedUid));
            Assert.Empty(getUsersResult.NotFound);
        }

        [Fact]
        public async void IgnoresNonExistingUsers()
        {
            var doesntExistId = new UidIdentifier("uid_that_doesnt_exist");
            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>()
                {
                    new UidIdentifier(this.fixture.TestUser1.Uid),
                    doesntExistId,
                    new UidIdentifier(this.fixture.TestUser3.Uid),
                });

            var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
            var expectedUids = new List<string>()
            {
                this.fixture.TestUser1.Uid,
                this.fixture.TestUser3.Uid,
            };
            Assert.Equal(expectedUids.Count(), uids.Count());
            Assert.All(expectedUids, expectedUid => uids.Contains(expectedUid));
            Assert.Single(getUsersResult.NotFound, doesntExistId);
        }

        [Fact]
        public async void OnlyNonExistingUsers()
        {
            var doesntExistId = new UidIdentifier("uid_that_doesnt_exist");
            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>()
                {
                    doesntExistId,
                });

            Assert.Empty(getUsersResult.Users);
            Assert.Single(getUsersResult.NotFound, doesntExistId);
        }

        [Fact]
        public async void DedupsDuplicateUsers()
        {
            var getUsersResult = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>()
                {
                    new UidIdentifier(this.fixture.TestUser1.Uid),
                    new UidIdentifier(this.fixture.TestUser1.Uid),
                });

            var uids = getUsersResult.Users.Select(userRecord => userRecord.Uid);
            Assert.Single(uids, this.fixture.TestUser1.Uid);
            Assert.Empty(getUsersResult.NotFound);
        }
    }

    public class GetUsersFixture : IDisposable
    {
        private readonly TemporaryUserBuilder userBuilder;

        public GetUsersFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            this.userBuilder = new TemporaryUserBuilder(FirebaseAuth.DefaultInstance);
            this.TestUser1 = this.userBuilder.CreateRandomUserAsync().Result;
            this.TestUser2 = this.userBuilder.CreateRandomUserAsync().Result;
            this.TestUser3 = this.userBuilder.CreateRandomUserAsync().Result;
            this.ImportUserUid = this.ImportUserWithProviderAsync().Result;
        }

        public UserRecord TestUser1 { get; }

        public UserRecord TestUser2 { get; }

        public UserRecord TestUser3 { get; }

        public string ImportUserUid { get; }

        public void Dispose()
        {
            this.userBuilder.Dispose();
        }

        private async Task<string> ImportUserWithProviderAsync()
        {
            var randomArgs = TemporaryUserBuilder.RandomUserRecordArgs();
            var importUser = new ImportUserRecordArgs()
            {
                Uid = randomArgs.Uid,
                Email = randomArgs.Email,
                PhoneNumber = randomArgs.PhoneNumber,
                UserProviders = new List<UserProvider>()
                {
                    new UserProvider()
                    {
                        Uid = $"google_{randomArgs.Uid}",
                        ProviderId = "google.com",
                    },
                },
            };

            var result = await FirebaseAuth.DefaultInstance.ImportUsersAsync(
                new List<ImportUserRecordArgs>()
                {
                    importUser,
                });
            Assert.Equal(1, result.SuccessCount);
            this.userBuilder.AddUid(randomArgs.Uid);
            return randomArgs.Uid;
        }
    }
}
