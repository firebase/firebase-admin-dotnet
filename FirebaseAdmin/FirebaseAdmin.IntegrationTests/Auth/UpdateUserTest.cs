// Copyright 2023, Google Inc. All rights reserved.
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
    public class UpdateUserTest : IClassFixture<UpdateUserFixture>
    {
        private readonly UpdateUserFixture fixture;

        public UpdateUserTest(UpdateUserFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        public async void CanUpdateProviderForUser()
        {
            var expectedProviderUid = $"google_{this.fixture.TestUser.Uid}";
            const string expectedProviderId = "google.com";
            const string expectedDisplayName = "Test";
            const string expectedEmail = "tester@example.com";
            const string expectedPhone = "+11234567890";
            const string expectedPhotoUrl = "https://www.example.com/image.png";

            var userRecordArgs = new UserRecordArgs
            {
                Uid = this.fixture.TestUser.Uid,
                ProviderToLink = new ProviderUserInfoArgs
                {
                    Uid = expectedProviderUid,
                    ProviderId = expectedProviderId,
                    DisplayName = expectedDisplayName,
                    Email = expectedEmail,
                    PhoneNumber = expectedPhone,
                    PhotoUrl = expectedPhotoUrl,
                },
            };

            await FirebaseAuth.DefaultInstance.UpdateUserAsync(userRecordArgs);

            var userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(this.fixture.TestUser.Uid);

            var provider = userRecord.ProviderData.SingleOrDefault(x => x.ProviderId == expectedProviderId);

            Assert.NotNull(provider);

            Assert.Equal(expectedProviderUid, provider.Uid);
            Assert.Equal(expectedProviderId, provider.ProviderId);
            Assert.Equal(expectedEmail, provider.Email);
            // TODO: Apparently the accounts:update endpoint does not update the provider phone number even if
            // it is specified in the UpdateUserRequest.ProviderToLink object
            // Assert.Equal(expectedPhone, provider.PhoneNumber);
            Assert.Equal(expectedPhotoUrl, provider.PhotoUrl);
        }
    }

    public class UpdateUserFixture : IDisposable
    {
        private readonly TemporaryUserBuilder userBuilder;

        public UpdateUserFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            this.userBuilder = new TemporaryUserBuilder(FirebaseAuth.DefaultInstance);
            this.TestUser = this.userBuilder.CreateRandomUserAsync().Result;
            this.ImportUserUid = this.ImportUserAsync().Result;
        }

        public UserRecord TestUser { get; }

        public string ImportUserUid { get; }

        public void Dispose()
        {
            this.userBuilder.Dispose();
        }

        private async Task<string> ImportUserAsync()
        {
            var randomArgs = TemporaryUserBuilder.RandomUserRecordArgs();
            var importUser = new ImportUserRecordArgs()
            {
                Uid = randomArgs.Uid,
                Email = randomArgs.Email,
                PhoneNumber = randomArgs.PhoneNumber,
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
