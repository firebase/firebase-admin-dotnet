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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    /// <summary>
    /// A utility for creating temporary user accounts during tests. Keeps track of the user
    /// accounts created, and deletes them on dispose. Other user accounts created outside this
    /// class can be marked for deletion using the <see cref="AddUid(string)"/> method. This class
    /// deletes user accounts in an idempotent manner. Therefore it's safe to delete any of the
    /// user accounts created by this class before this instance is disposed. This class is not
    /// thread safe. Any concurrent usage should be synchronized accordingly.
    /// <summary>
    public sealed class TemporaryUserBuilder : IDisposable
    {
        private readonly ISet<string> userIds = new HashSet<string>();
        private readonly AbstractFirebaseAuth auth;

        public TemporaryUserBuilder(AbstractFirebaseAuth auth)
        {
            this.auth = auth;
        }

        public static UserRecordArgs RandomUserRecordArgs()
        {
            var uid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var rand = new Random();
            var phoneDigits = Enumerable.Range(0, 10).Select(_ => rand.Next(10));

            return new UserRecordArgs()
            {
                Uid = uid,
                Email = $"test{uid.Substring(0, 12)}@example.{uid.Substring(12)}.com",
                PhoneNumber = $"+1{string.Join(string.Empty, phoneDigits)}",
                DisplayName = "Random User",
                PhotoUrl = "https://example.com/photo.png",
                Password = "password",
            };
        }

        public async Task<UserRecord> CreateRandomUserAsync()
        {
            return await this.CreateUserAsync(RandomUserRecordArgs());
        }

        public async Task<UserRecord> CreateUserAsync(UserRecordArgs args)
        {
            // Make sure we never create more than 1000 users in a single instance.
            // This allows us to delete all user accounts with a single call to DeleteUsers().
            // Should not ever occur in practice.
            if (this.userIds.Count() >= 1000)
            {
                throw new InvalidOperationException("Maximum number of users reached.");
            }

            var user = await this.auth.CreateUserAsync(args);
            this.AddUid(user.Uid);
            return user;
        }

        public bool AddUid(string uid)
        {
            return this.userIds.Add(uid);
        }

        public void Dispose()
        {
            Thread.Sleep(1000); // DeleteUsers is rate limited at 1qps.
            this.auth.DeleteUsersAsync(this.userIds.ToList()).Wait();
            this.userIds.Clear();
        }
    }
}
