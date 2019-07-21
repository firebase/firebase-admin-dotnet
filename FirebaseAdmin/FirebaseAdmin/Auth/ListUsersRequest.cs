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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Discovery;
using Google.Apis.Http;
using Google.Apis.Requests;
using Google.Apis.Services;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a request made using the Google API client to list all Firebase users in a
    /// project.
    /// </summary>
    internal sealed class ListUsersRequest : IClientServiceRequest<ExportedUserRecords>
    {
        private const int MaxListUsersResults = 1000;

        private readonly string baseUrl;
        private readonly ConfigurableHttpClient httpClient;
        private readonly AuthErrorHandler errorHandler;

        private ListUsersRequest(
            string baseUrl, ConfigurableHttpClient httpClient, ListUsersOptions options)
        {
            this.baseUrl = baseUrl;
            this.httpClient = httpClient;
            this.errorHandler = new AuthErrorHandler();
            this.RequestParameters = new Dictionary<string, IParameter>();
            this.SetPageSize(options.PageSize);
            this.SetPageToken(options.PageToken);
        }

        public string MethodName => "ListUsers";

        public string RestPath => "accounts:batchGet";

        public string HttpMethod => "GET";

        public IDictionary<string, IParameter> RequestParameters { get; }

        public IClientService Service { get; }

        public HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
        {
            var queryParameters = string.Join("&", this.RequestParameters.Select(
                kvp => $"{kvp.Key}={kvp.Value.DefaultValue}"));
            return new HttpRequestMessage()
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri($"{this.baseUrl}/{this.RestPath}?{queryParameters}"),
            };
        }

        public async Task<Stream> ExecuteAsStreamAsync()
        {
            return await this.ExecuteAsStreamAsync(default).ConfigureAwait(false);
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

        public async Task<ExportedUserRecords> ExecuteAsync()
        {
            return await this.ExecuteAsync(default).ConfigureAwait(false);
        }

        public async Task<ExportedUserRecords> ExecuteAsync(CancellationToken cancellationToken)
        {
            var downloadAccountResponse = await this.SendAndDeserializeAsync(
                this.CreateRequest(), cancellationToken).ConfigureAwait(false);
            var userRecords = downloadAccountResponse.Users?.Select(
                u => new ExportedUserRecord(u));
            return new ExportedUserRecords
            {
                NextPageToken = downloadAccountResponse.NextPageToken,
                Users = userRecords,
            };
        }

        public ExportedUserRecords Execute()
        {
            return this.ExecuteAsync().Result;
        }

        internal void SetPageSize(int? pageSize)
        {
            this.AddOrUpdate("maxResults", CheckPageSize(pageSize).ToString());
        }

        internal void SetPageToken(string pageToken)
        {
            if (pageToken != null)
            {
                this.AddOrUpdate("nextPageToken", CheckPageToken(pageToken));
            }
            else
            {
                this.RequestParameters.Remove("nextPageToken");
            }
        }

        private static int CheckPageSize(int? pageSize)
        {
            if (pageSize > MaxListUsersResults)
            {
                throw new ArgumentException("Page size must not exceed 1000.");
            }
            else if (pageSize <= 0)
            {
                throw new ArgumentException("Page size must be positive.");
            }

            return pageSize ?? MaxListUsersResults;
        }

        private static string CheckPageToken(string token)
        {
            if (token == string.Empty)
            {
                throw new ArgumentException("Page token must not be empty.");
            }

            return token;
        }

        private void AddOrUpdate(string paramName, string value)
        {
            var parameter = new Parameter()
            {
                DefaultValue = value,
                IsRequired = true,
                Name = paramName,
            };

            if (!this.RequestParameters.ContainsKey(paramName))
            {
                this.RequestParameters.Add(paramName, parameter);
            }
            else
            {
                this.RequestParameters[paramName] = parameter;
            }
        }

        private async Task<DownloadAccountResponse> SendAndDeserializeAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await this.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
            return response.SafeDeserialize<DownloadAccountResponse>(
                FirebaseUserManager.HandleParseError).Result;
        }

        private async Task<Extensions.ResponseInfo> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await this.httpClient.SendAndReadAsync(
                request, cancellationToken, FirebaseUserManager.HandleHttpError)
                .ConfigureAwait(false);
            this.errorHandler.ThrowIfError(response.HttpResponse, response.Body);
            return response;
        }

        /// <summary>
        /// Factory class that validates arguments, and then creates new instances of the
        /// <see cref="ListUsersRequest"/> class.
        /// </summary>
        internal sealed class Factory
        {
            private readonly string baseUrl;
            private readonly ConfigurableHttpClient httpClient;
            private readonly ListUsersOptions options;

            internal Factory(
                string baseUrl, ConfigurableHttpClient httpClient, ListUsersOptions options = null)
            {
                this.baseUrl = baseUrl;
                this.httpClient = httpClient;
                this.options = new ListUsersOptions()
                {
                    PageSize = CheckPageSize(options?.PageSize),
                    PageToken = CheckPageToken(options?.PageToken),
                };
            }

            internal ListUsersRequest Create()
            {
                return new ListUsersRequest(this.baseUrl, this.httpClient, this.options);
            }
        }
    }
}
