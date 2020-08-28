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
using Google.Apis.Discovery;
using Google.Apis.Requests;
using Google.Apis.Services;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// A Google client service request implementation that supports paginating through a list of
    /// resources. This parent class implements most of the argument validation and HTTP request
    /// marshaling logic, but leaves the actual transport behavior to be implemented by the child
    /// classes.
    /// </summary>
    internal abstract class ListResourcesRequest<TResult> : IClientServiceRequest<TResult>
    {
        private readonly string baseUrl;

        public ListResourcesRequest(string baseUrl, string pageToken, int? pageSize)
        {
            this.baseUrl = baseUrl;
            this.RequestParameters = new Dictionary<string, IParameter>();
            this.SetPageToken(pageToken);
            this.SetPageSize(pageSize);
        }

        public virtual string MethodName => "ListResources";

        public virtual string HttpMethod => "GET";

        public virtual IClientService Service { get; }

        public abstract string RestPath { get; }

        public IDictionary<string, IParameter> RequestParameters { get; }

        protected virtual int MaxResults => 100;

        protected virtual string PageSizeParam => "pageSize";

        protected virtual string PageTokenParam => "pageToken";

        public async Task<TResult> ExecuteAsync()
        {
            return await this.ExecuteAsync(default).ConfigureAwait(false);
        }

        public TResult Execute()
        {
            return this.ExecuteAsync().Result;
        }

        public Stream ExecuteAsStream()
        {
            return this.ExecuteAsStreamAsync().Result;
        }

        public async Task<Stream> ExecuteAsStreamAsync()
        {
            return await this.ExecuteAsStreamAsync(default).ConfigureAwait(false);
        }

        public abstract Task<TResult> ExecuteAsync(CancellationToken cancellationToken);

        public abstract Task<Stream> ExecuteAsStreamAsync(CancellationToken cancellationToken);

        public virtual HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
        {
            var query = this.RequestParameters.ToDictionary(
                entry => entry.Key, entry => entry.Value.DefaultValue as object);
            var uri = $"{this.baseUrl}/{this.RestPath}{HttpUtils.EncodeQueryParams(query)}";
            return new HttpRequestMessage()
            {
                Method = System.Net.Http.HttpMethod.Get,
                RequestUri = new Uri(uri),
            };
        }

        internal void SetPageSize(int? pageSize)
        {
            if (pageSize > this.MaxResults)
            {
                throw new ArgumentException($"Page size must not exceed {this.MaxResults}.");
            }
            else if (pageSize <= 0)
            {
                throw new ArgumentException("Page size must be positive.");
            }

            this.AddOrUpdate(this.PageSizeParam, (pageSize ?? this.MaxResults).ToString());
        }

        internal void SetPageToken(string pageToken)
        {
            if (pageToken != null)
            {
                if (pageToken == string.Empty)
                {
                    throw new ArgumentException("Page token must not be empty.");
                }

                this.AddOrUpdate(this.PageTokenParam, pageToken);
            }
            else
            {
                this.RequestParameters.Remove(this.PageTokenParam);
            }
        }

        protected void AddOrUpdate(string paramName, string value)
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
