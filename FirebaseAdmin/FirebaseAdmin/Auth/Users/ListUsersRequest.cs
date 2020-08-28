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

using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;

namespace FirebaseAdmin.Auth.Users
{
    /// <summary>
    /// Represents a request made using the Google API client to list all Firebase users in a
    /// project.
    /// </summary>
    internal sealed class ListUsersRequest
    : AdaptedListResourcesRequest<ExportedUserRecords, FirebaseAuthException>
    {
        internal ListUsersRequest(
            string baseUrl,
            ListUsersOptions options,
            ErrorHandlingHttpClient<FirebaseAuthException> httpClient)
        : base(baseUrl, options?.PageToken, options?.PageSize, httpClient) { }

        public override string RestPath => "accounts:batchGet";

        protected override string PageSizeParam => "maxResults";

        protected override string PageTokenParam => "nextPageToken";

        protected override int MaxResults => 1000;

        public override HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
        {
            var request = base.CreateRequest(overrideGZipEnabled);
            request.Headers.Add(
                FirebaseUserManager.ClientVersionHeader, FirebaseUserManager.ClientVersion);
            return request;
        }

        public override async Task<ExportedUserRecords> ExecuteAsync(
            CancellationToken cancellationToken)
        {
            var downloadAccountResponse = await this
                .SendAndDeserializeAsync<DownloadAccountResponse>(cancellationToken)
                .ConfigureAwait(false);
            var userRecords = downloadAccountResponse.Users?.Select(
                u => new ExportedUserRecord(u));
            return new ExportedUserRecords
            {
                NextPageToken = downloadAccountResponse.NextPageToken,
                Users = userRecords,
            };
        }
    }
}
