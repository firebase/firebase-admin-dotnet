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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

namespace FirebaseAdmin.Auth.Multitenancy
{
    /// <summary>
    /// The tenant manager facilitates GCIP multitenancy related operations. This includes:
    /// <list type="bullet">
    /// <item>
    /// <description>Creating, updating, retrieving and deleting tenants in the underlying
    /// project.</description>
    /// </item>
    /// <item>
    /// <description>Obtaining TenantAwareFirebaseAuth instances for performing operations (user
    /// management, provider configuration management, token verification, email link generation,
    /// etc) in the context of a specified tenant.</description>
    /// </item>
    /// </list>
    /// </summary>
    public sealed class TenantManager : IDisposable
    {
        private const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/v2/projects/{0}";

        private const string ClientVersionHeader = "X-Client-Version";

        private static readonly string ClientVersion = $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private readonly string baseUrl;

        private readonly ErrorHandlingHttpClient<FirebaseAuthException> httpClient;

        internal TenantManager(Args args)
        {
            if (string.IsNullOrEmpty(args.ProjectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to manage provider"
                    + " configurations.");
            }

            this.baseUrl = string.Format(IdToolkitUrl, args.ProjectId);
            this.httpClient = new ErrorHandlingHttpClient<FirebaseAuthException>(
                args.ToHttpClientArgs());
        }

        /// <summary>
        /// Gets the <see cref="Tenant"/> corresponding to the given <paramref name="tenantId"/>.
        /// </summary>
        /// <param name="tenantId">A tenant identifier string.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentException">If tenant ID argument is null or empty.</exception>
        /// <exception cref="FirebaseAuthException">If a tenant cannot be found with the specified
        /// ID.</exception>
        public async Task<Tenant> GetTenantAsync(string tenantId)
        {
            return await this.GetTenantAsync(tenantId, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets the <see cref="Tenant"/> corresponding to the given <paramref name="tenantId"/>.
        /// </summary>
        /// <param name="tenantId">A tenant identifier string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentException">If the tenant ID argument is null or empty.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If a tenant cannot be found with the specified
        /// ID.</exception>
        public async Task<Tenant> GetTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty.");
            }

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"{this.baseUrl}/tenants/{tenantId}"),
            };
            var args = await this.SendAndDeserializeAsync<Tenant.Args>(request, cancellationToken)
                .ConfigureAwait(false);
            return new Tenant(args);
        }

        /// <summary>
        /// Cleans up and invalidates this instance. For internal use only.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.httpClient.Dispose();
        }

        internal static TenantManager Create(FirebaseApp app)
        {
            var args = new Args
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                ProjectId = app.GetProjectId(),
                RetryOptions = RetryOptions.Default,
            };

            return new TenantManager(args);
        }

        private async Task<T> SendAndDeserializeAsync<T>(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(ClientVersionHeader, ClientVersion);
            var response = await this.httpClient
                .SendAndDeserializeAsync<T>(request, cancellationToken)
                .ConfigureAwait(false);
            return response.Result;
        }

        internal sealed class Args
        {
            internal HttpClientFactory ClientFactory { get; set; }

            internal GoogleCredential Credential { get; set; }

            internal string ProjectId { get; set; }

            internal RetryOptions RetryOptions { get; set; }

            internal ErrorHandlingHttpClientArgs<FirebaseAuthException> ToHttpClientArgs()
            {
                return new ErrorHandlingHttpClientArgs<FirebaseAuthException>()
                {
                    HttpClientFactory = this.ClientFactory,
                    Credential = this.Credential,
                    RetryOptions = this.RetryOptions,
                    ErrorResponseHandler = AuthErrorHandler.Instance,
                    RequestExceptionHandler = AuthErrorHandler.Instance,
                    DeserializeExceptionHandler = AuthErrorHandler.Instance,
                };
            }
        }
    }
}
