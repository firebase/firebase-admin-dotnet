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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Api.Gax.Rest;
using Google.Apis.Discovery;
using Google.Apis.Json;
using Google.Apis.Requests;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;

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
        private static readonly HttpMethod Patch = new HttpMethod("PATCH");

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
            var updateMask = CreateUpdateMask(content);
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
                Method = Patch,
                RequestUri = BuildUri($"{this.ResourcePath}/{providerId}", query),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(content),
            };
            return await this.SendAndDeserializeAsync(client, request, cancellationToken)
                .ConfigureAwait(false);
        }

        internal PagedAsyncEnumerable<AuthProviderConfigs<T>, T>
            ListProviderConfigsAsync(ApiClient client, ListProviderConfigsOptions options)
        {
            var request = this.CreateListRequest(client, options);
            return new RestPagedAsyncEnumerable
                <AbstractListRequest, AuthProviderConfigs<T>, T>(() => request, new PageManager());
        }

        protected abstract string ValidateProviderId(string providerId);

        protected abstract Task<T> SendAndDeserializeAsync(
            ApiClient client, HttpRequestMessage request, CancellationToken cancellationToken);

        protected abstract AbstractListRequest CreateListRequest(
            ApiClient client, ListProviderConfigsOptions options);

        private static IList<string> CreateUpdateMask(AuthProviderConfig.Request request)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(request);
            var dictionary = JObject.Parse(json);
            var mask = CreateUpdateMask(dictionary);
            mask.Sort();
            return mask;
        }

        private static Uri BuildUri(string path, IDictionary<string, object> queryParams = null)
        {
            var uriString = $"{path}{EncodeQueryParams(queryParams)}";
            return new Uri(uriString, UriKind.Relative);
        }

        private static string EncodeQueryParams(IDictionary<string, object> queryParams)
        {
            var queryString = string.Empty;
            if (queryParams != null && queryParams.Count > 0)
            {
                var list = queryParams.Select(kvp => $"{kvp.Key}={kvp.Value}");
                queryString = "?" + string.Join("&", list);
            }

            return queryString;
        }

        private static List<string> CreateUpdateMask(JObject dictionary)
        {
            var mask = new List<string>();
            foreach (var entry in dictionary)
            {
                if (entry.Value.Type == JTokenType.Object)
                {
                    var childMask = CreateUpdateMask((JObject)entry.Value);
                    mask.AddRange(childMask.Select((item) => $"{entry.Key}.{item}"));
                }
                else
                {
                    mask.Add(entry.Key);
                }
            }

            return mask;
        }

        /// <summary>
        /// A class for making batch get requests to list a specific type of auth provider
        /// configurations. An instance of this class is used by the Google API client to provide
        /// pagination support.
        /// </summary>
        protected abstract class AbstractListRequest
        : IClientServiceRequest<AuthProviderConfigs<T>>
        {
            private const int MaxListResults = 100;

            private readonly ApiClient client;

            protected AbstractListRequest(
                ApiClient client, ListProviderConfigsOptions options)
            {
                this.client = client;
                this.RequestParameters = new Dictionary<string, IParameter>();
                this.SetPageSize(options?.PageSize);
                this.SetPageToken(options?.PageToken);
            }

            public abstract string MethodName { get; }

            public abstract string RestPath { get; }

            public string HttpMethod => "GET";

            public IDictionary<string, IParameter> RequestParameters { get; }

            public IClientService Service { get; }

            protected ApiClient ApiClient => this.client;

            public HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
            {
                var query = this.RequestParameters.ToDictionary(
                    entry => entry.Key, entry => entry.Value.DefaultValue as object);
                return new HttpRequestMessage()
                {
                    Method = System.Net.Http.HttpMethod.Get,
                    RequestUri = BuildUri(this.RestPath, query),
                };
            }

            public async Task<Stream> ExecuteAsStreamAsync(CancellationToken cancellationToken)
            {
                var request = this.CreateRequest();
                var response = await this.ApiClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            }

            public Stream ExecuteAsStream()
            {
                return this.ExecuteAsStreamAsync().Result;
            }

            public async Task<Stream> ExecuteAsStreamAsync()
            {
                return await this.ExecuteAsStreamAsync(default).ConfigureAwait(false);
            }

            public AuthProviderConfigs<T> Execute()
            {
                return this.ExecuteAsync().Result;
            }

            public async Task<AuthProviderConfigs<T>> ExecuteAsync()
            {
                return await this.ExecuteAsync(default).ConfigureAwait(false);
            }

            public abstract Task<AuthProviderConfigs<T>> ExecuteAsync(
                CancellationToken cancellationToken);

            internal void SetPageSize(int? pageSize)
            {
                if (pageSize > MaxListResults)
                {
                    throw new ArgumentException($"Page size must not exceed {MaxListResults}.");
                }
                else if (pageSize <= 0)
                {
                    throw new ArgumentException("Page size must be positive.");
                }

                this.AddOrUpdate("pageSize", (pageSize ?? MaxListResults).ToString());
            }

            internal void SetPageToken(string pageToken)
            {
                if (pageToken != null)
                {
                    if (pageToken == string.Empty)
                    {
                        throw new ArgumentException("Page token must not be empty.");
                    }

                    this.AddOrUpdate("pageToken", pageToken);
                }
                else
                {
                    this.RequestParameters.Remove("pageToken");
                }
            }

            private void AddOrUpdate(string paramName, string value)
            {
                this.RequestParameters[paramName] = new Parameter()
                {
                    DefaultValue = value,
                    IsRequired = true,
                    Name = paramName,
                };
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
