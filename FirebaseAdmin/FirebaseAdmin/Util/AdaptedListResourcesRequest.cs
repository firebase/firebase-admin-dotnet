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

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An implementation of <see cref="ListResourcesRequest{TResult}"/> that uses the
    /// <see cref="ErrorHandlingHttpClient{T}"/> API to communicate with the backend server.
    /// </summary>
    internal abstract class AdaptedListResourcesRequest<TResult, TException>
    : ListResourcesRequest<TResult>
    where TException : FirebaseException
    {
        private readonly ErrorHandlingHttpClient<TException> httpClient;

        public AdaptedListResourcesRequest(
            string baseUrl,
            string pageToken,
            int? pageSize,
            ErrorHandlingHttpClient<TException> httpClient)
        : base(baseUrl, pageToken, pageSize)
        {
            this.httpClient = httpClient;
        }

        public override async Task<Stream> ExecuteAsStreamAsync(CancellationToken cancellationToken)
        {
            var request = this.CreateRequest();
            var response = await this.httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        private protected async Task<T> SendAndDeserializeAsync<T>(
            CancellationToken cancellationToken)
        {
            var request = this.CreateRequest();
            var response = await this.httpClient
                .SendAndDeserializeAsync<T>(request, cancellationToken)
                .ConfigureAwait(false);
            return response.Result;
        }
    }
}
