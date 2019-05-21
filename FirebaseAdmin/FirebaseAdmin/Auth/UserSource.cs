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
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a source of user data that can be queried to load a batch of users.
    /// </summary>
    internal class UserSource : ISource<UserRecord, DownloadAccountResponse>
    {
        private readonly CancellationToken cancellationToken;

        private readonly FirebaseUserManager userManager;

        private UserSource(FirebaseUserManager userManager, CancellationToken cancellationToken)
        {
            this.userManager = userManager;
            this.cancellationToken = cancellationToken;
        }

        public static UserSource DefaultUserSource(FirebaseUserManager userManager, CancellationToken cancellationToken)
        {
            return new UserSource(userManager, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<DownloadAccountResponse> FetchRaw(int maxResults, string pageToken)
        {
            return await this.userManager.ListUsers(maxResults, pageToken, this.cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Tuple<string, IEnumerable<UserRecord>>> Fetch(int maxResults, string pageToken)
        {
            var response = await this.FetchRaw(maxResults, pageToken);

            if (string.IsNullOrEmpty(response.NextPageToken))
            {
                return new Tuple<string, IEnumerable<UserRecord>>(string.Empty, Enumerable.Empty<UserRecord>());
            }

            var users = new List<UserRecord>();
            foreach (var userRecord in response.Users)
            {
                users.Add(new UserRecord(userRecord));
            }

            return new Tuple<string, IEnumerable<UserRecord>>(response.NextPageToken, users);
        }
    }
}