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
using FirebaseAdmin.Util;
using Google.Apis.Discovery;
using Google.Apis.Requests;
using Google.Apis.Services;
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// A request made using the Google API client to list all auth provider configurations in a
    /// Firebase project.
    /// </summary>
    internal abstract class ListProviderConfigsRequest<T>
    : IClientServiceRequest<AuthProviderConfigs<T>>
    where T : AuthProviderConfig
    {
        private const int MaxListResults = 100;

        private readonly string baseUrl;
        private readonly ErrorHandlingHttpClient<FirebaseAuthException> httpClient;

        protected ListProviderConfigsRequest(
            string baseUrl,
            ErrorHandlingHttpClient<FirebaseAuthException> httpClient,
            ListProviderConfigsOptions options)
        {
            this.baseUrl = baseUrl;
            this.httpClient = httpClient;
            this.RequestParameters = new Dictionary<string, IParameter>();
            this.SetPageSize(options?.PageSize);
            this.SetPageToken(options?.PageToken);
        }

        public abstract string MethodName { get; }

        public abstract string RestPath { get; }

        public string HttpMethod => "GET";

        public IDictionary<string, IParameter> RequestParameters { get; }

        public IClientService Service { get; }

        public HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
        {
            var queryParameters = string.Join("&", this.RequestParameters.Select(
                kvp => $"{kvp.Key}={kvp.Value.DefaultValue}"));
            var request = new HttpRequestMessage()
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"{this.baseUrl}/{this.RestPath}?{queryParameters}"),
            };
            request.Headers.Add(
                ProviderConfigManager.ClientVersionHeader, ProviderConfigManager.ClientVersion);
            return request;
        }

        public async Task<Stream> ExecuteAsStreamAsync(CancellationToken cancellationToken)
        {
            var response = await this.httpClient.SendAsync(this.CreateRequest(), cancellationToken)
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

        public async Task<AuthProviderConfigs<T>> ExecuteAsync(CancellationToken cancellationToken)
        {
            var jObject = await this.SendAndDeserializeAsync(cancellationToken)
                .ConfigureAwait(false);
            return this.BuildProviderConfigs(jObject);
        }

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

        protected abstract AuthProviderConfigs<T> BuildProviderConfigs(JObject json);

        private async Task<JObject> SendAndDeserializeAsync(CancellationToken cancellationToken)
        {
            var request = this.CreateRequest();
            var response = await this.httpClient
                .SendAndDeserializeAsync<JObject>(request, cancellationToken)
                .ConfigureAwait(false);
            return response.Result;
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
}
