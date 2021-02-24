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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// Provides low-level methods for interacting with the
    /// <a href="https://developers.google.com/identity/toolkit/web/reference/relyingparty">
    /// Google Identity Toolkit v2 REST API</a>. Implemented as a simple wrapper of the
    /// <see cref="ErrorHandlingHttpClient{T}"/> class. This class has state that must be disposed
    /// after use.
    /// </summary>
    internal sealed class ApiClient : IDisposable
    {
        private const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/v2/projects/{0}";

        private const string ClientVersionHeader = "X-Client-Version";

        private static readonly string ClientVersion = $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private readonly ErrorHandlingHttpClient<FirebaseAuthException> httpClient;

        internal ApiClient(
            string projectId,
            string tenantId,
            ErrorHandlingHttpClientArgs<FirebaseAuthException> args)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to manage provider"
                    + " configurations.");
            }

            var baseUrl = string.Format(IdToolkitUrl, projectId);
            if (tenantId != null)
            {
                this.BaseUrl = $"{baseUrl}/tenants/{tenantId}";
            }
            else
            {
                this.BaseUrl = baseUrl;
            }

            this.httpClient = new ErrorHandlingHttpClient<FirebaseAuthException>(args);
        }

        internal string BaseUrl { get; }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        internal async Task<T> SendAndDeserializeAsync<T>(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (!request.RequestUri.IsAbsoluteUri)
            {
                request.RequestUri = new Uri($"{this.BaseUrl}/{request.RequestUri}");
            }

            if (!request.Headers.Contains(ClientVersionHeader))
            {
                request.Headers.Add(ClientVersionHeader, ClientVersion);
            }

            var response = await this.httpClient
                .SendAndDeserializeAsync<T>(request, cancellationToken)
                .ConfigureAwait(false);
            return response.Result;
        }

        internal async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return await this.httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
