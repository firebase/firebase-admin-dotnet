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
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Api.Gax;
using Google.Api.Gax.Rest;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// A client that supports managing auth provider configurations by making remote API calls to
    /// the Firebase Auth backend services. This class is stateless, and the HTTP transport
    /// capabilities are provided by the <see cref="ApiClient"/>.
    /// </summary>
    /// <typeparam name="T">Type of <see cref="AuthProviderConfig"/> that can be managed using this
    /// client.</typeparam>
    internal abstract class ProviderConfigClient<T>
    where T : AuthProviderConfig
    {
        protected abstract string ResourcePath { get; }

        protected abstract string ProviderIdParam { get; }

        internal async Task<T> GetProviderConfigAsync(
            ApiClient client, string providerId, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = BuildUri($"{this.ResourcePath}/{this.ValidateProviderId(providerId)}"),
            };
            return await this.SendAndDeserializeAsync(client, request, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<T> CreateProviderConfigAsync(
            ApiClient client, AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
        {
            var query = new Dictionary<string, object>()
            {
                { this.ProviderIdParam, this.ValidateProviderId(args.ProviderId) },
            };
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = BuildUri(this.ResourcePath, query),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(
                    args.ToCreateRequest()),
            };
            return await this.SendAndDeserializeAsync(client, request, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<T> UpdateProviderConfigAsync(
            ApiClient client, AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
        {
            var providerId = this.ValidateProviderId(args.ProviderId);
            var content = args.ToUpdateRequest();
            var updateMask = HttpUtils.CreateUpdateMask(content);
            if (updateMask.Count == 0)
            {
                throw new ArgumentException("At least one field must be specified for update.");
            }

            var query = new Dictionary<string, object>()
            {
                { "updateMask", string.Join(",", updateMask) },
            };
            var request = new HttpRequestMessage()
            {
                Method = HttpUtils.Patch,
                RequestUri = BuildUri($"{this.ResourcePath}/{providerId}", query),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(content),
            };
            return await this.SendAndDeserializeAsync(client, request, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task DeleteProviderConfigAsync(
            ApiClient client, string providerId, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Delete,
                RequestUri = BuildUri($"{this.ResourcePath}/{this.ValidateProviderId(providerId)}"),
            };
            await client.SendAndDeserializeAsync<object>(request, cancellationToken)
                .ConfigureAwait(false);
        }

        internal PagedAsyncEnumerable<AuthProviderConfigs<T>, T>
            ListProviderConfigsAsync(ApiClient client, ListProviderConfigsOptions options)
        {
            Func<AbstractListRequest> validateAndCreate = () => this.CreateListRequest(client, options);
            validateAndCreate();
            return new RestPagedAsyncEnumerable<AbstractListRequest, AuthProviderConfigs<T>, T>(
                validateAndCreate, new PageManager());
        }

        protected abstract string ValidateProviderId(string providerId);

        protected abstract Task<T> SendAndDeserializeAsync(
            ApiClient client, HttpRequestMessage request, CancellationToken cancellationToken);

        protected abstract AbstractListRequest CreateListRequest(
            ApiClient client, ListProviderConfigsOptions options);

        private static Uri BuildUri(string path, IDictionary<string, object> queryParams = null)
        {
            var uriString = $"{path}{HttpUtils.EncodeQueryParams(queryParams)}";
            return new Uri(uriString, UriKind.Relative);
        }

        /// <summary>
        /// A class for making batch GET requests to list a specific type of auth provider
        /// configuration. An instance of this class is used by the Google API client to provide
        /// pagination support.
        /// </summary>
        protected abstract class AbstractListRequest
        : ListResourcesRequest<AuthProviderConfigs<T>>
        {
            protected AbstractListRequest(ApiClient client, ListProviderConfigsOptions options)
            : base(client.BaseUrl, options?.PageToken, options?.PageSize)
            {
                this.ApiClient = client;
            }

            protected ApiClient ApiClient { get; }

            public override async Task<Stream> ExecuteAsStreamAsync(
                CancellationToken cancellationToken)
            {
                var request = this.CreateRequest();
                var response = await this.ApiClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// A Google API client utility for paging through a sequence of provider configurations.
        /// </summary>
        private class PageManager
        : IPageManager<AbstractListRequest, AuthProviderConfigs<T>, T>
        {
            public void SetPageSize(AbstractListRequest request, int pageSize)
            {
                request.SetPageSize(pageSize);
            }

            public void SetPageToken(AbstractListRequest request, string pageToken)
            {
                request.SetPageToken(pageToken);
            }

            public IEnumerable<T> GetResources(AuthProviderConfigs<T> response)
            {
                return response?.ProviderConfigs;
            }

            public string GetNextPageToken(AuthProviderConfigs<T> response)
            {
                return string.IsNullOrEmpty(response.NextPageToken) ? null : response.NextPageToken;
            }
        }
    }
}
