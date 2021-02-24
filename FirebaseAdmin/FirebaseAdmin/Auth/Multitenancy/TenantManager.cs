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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Api.Gax;
using Google.Api.Gax.Rest;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;

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

        private readonly IDictionary<string, TenantAwareFirebaseAuth> tenants =
            new Dictionary<string, TenantAwareFirebaseAuth>();

        private readonly object tenantsLock = new object();

        private readonly FirebaseApp app;

        private readonly string baseUrl;

        private readonly ErrorHandlingHttpClient<FirebaseAuthException> httpClient;

        private bool disposed;

        internal TenantManager(Args args)
        {
            if (string.IsNullOrEmpty(args.ProjectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to manage tenants.");
            }

            this.app = args.App;
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
            var args = await this.SendAndDeserializeAsync<TenantArgs>(request, cancellationToken)
                .ConfigureAwait(false);
            return new Tenant(args);
        }

        /// <summary>
        /// Creates a new tenant.
        /// </summary>
        /// <param name="args">Arguments that describe the new tenant configuration.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while creating
        /// the tenant.</exception>
        public async Task<Tenant> CreateTenantAsync(TenantArgs args)
        {
            return await this.CreateTenantAsync(args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates a new tenant.
        /// </summary>
        /// <param name="args">Arguments that describe the new tenant configuration.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while creating
        /// the tenant.</exception>
        public async Task<Tenant> CreateTenantAsync(
            TenantArgs args, CancellationToken cancellationToken)
        {
            args.ThrowIfNull(nameof(args));
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.baseUrl}/tenants"),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(args),
            };
            var resp = await this.SendAndDeserializeAsync<TenantArgs>(request, cancellationToken)
                .ConfigureAwait(false);
            return new Tenant(resp);
        }

        /// <summary>
        /// Updates an existing tenant.
        /// </summary>
        /// <param name="tenantId">ID of the tenant to be updated.</param>
        /// <param name="args">Properties to be updated in the tenant.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="tenantId"/> is null or empty,
        /// or if <paramref name="args"/> does not contain any values.
        /// </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while updating
        /// the tenant.</exception>
        public async Task<Tenant> UpdateTenantAsync(string tenantId, TenantArgs args)
        {
            return await this.UpdateTenantAsync(tenantId, args, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Updates an existing tenant.
        /// </summary>
        /// <param name="tenantId">ID of the tenant to be updated.</param>
        /// <param name="args">Properties to be updated in the tenant.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="Tenant"/>.</returns>
        /// <exception cref="ArgumentException">If <paramref name="tenantId"/> is null or empty,
        /// or if <paramref name="args"/> does not contain any values.
        /// </exception>
        /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If an unexpected error occurs while updating
        /// the tenant.</exception>
        public async Task<Tenant> UpdateTenantAsync(
            string tenantId, TenantArgs args, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty.");
            }

            var updateMask = HttpUtils.CreateUpdateMask(args.ThrowIfNull(nameof(args)));
            if (updateMask.Count == 0)
            {
                throw new ArgumentException("At least one field must be specified for update.");
            }

            var queryString = HttpUtils.EncodeQueryParams(new Dictionary<string, object>()
            {
                { "updateMask", string.Join(",", updateMask) },
            });

            var request = new HttpRequestMessage()
            {
                Method = HttpUtils.Patch,
                RequestUri = new Uri($"{this.baseUrl}/tenants/{tenantId}{queryString}"),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(args),
            };
            var resp = await this.SendAndDeserializeAsync<TenantArgs>(request, cancellationToken)
                .ConfigureAwait(false);
            return new Tenant(resp);
        }

        /// <summary>
        /// Deletes the tenant corresponding to the given <paramref name="tenantId"/>.
        /// </summary>
        /// <param name="tenantId">A tenant identifier string.</param>
        /// <returns>A task that completes when the tenant is deleted.</returns>
        /// <exception cref="ArgumentException">If the tenant ID argument is null or empty.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If a tenant cannot be found with the specified
        /// ID.</exception>
        public async Task DeleteTenantAsync(string tenantId)
        {
            await this.DeleteTenantAsync(tenantId, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes the tenant corresponding to the given <paramref name="tenantId"/>.
        /// </summary>
        /// <param name="tenantId">A tenant identifier string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes when the tenant is deleted.</returns>
        /// <exception cref="ArgumentException">If the tenant ID argument is null or empty.
        /// </exception>
        /// <exception cref="FirebaseAuthException">If a tenant cannot be found with the specified
        /// ID.</exception>
        public async Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty.");
            }

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Delete,
                RequestUri = new Uri($"{this.baseUrl}/tenants/{tenantId}"),
            };
            await this.SendAndDeserializeAsync<object>(request, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Gets an async enumerable to iterate or page through tenants
        /// starting from the specified page token. If the page token is null or unspecified,
        /// iteration starts from the first page. See
        /// <a href="https://googleapis.github.io/google-cloud-dotnet/docs/guides/page-streaming.html">
        /// Page Streaming</a> for more details on how to use this API.
        /// </summary>
        /// <param name="options">The options to control the starting point and page size. Pass
        /// null to list from the beginning with default settings.</param>
        /// <returns>A <see cref="PagedAsyncEnumerable{TenantsPage, Tenant}"/>
        /// instance.</returns>
        public PagedAsyncEnumerable<TenantsPage, Tenant> ListTenantsAsync(ListTenantsOptions options)
        {
            Func<ListTenantsRequest> validateAndCreate = () => new ListTenantsRequest(
                this.baseUrl, options, this.httpClient);
            validateAndCreate();
            return new RestPagedAsyncEnumerable
                <ListTenantsRequest, TenantsPage, Tenant>(validateAndCreate, new PageManager());
        }

        /// <summary>
        /// Gets a <see cref="TenantAwareFirebaseAuth"/> instance scoped to the specified tenant.
        /// </summary>
        /// <param name="tenantId">A tenant identifier string.</param>
        /// <returns>An object that can be used to perform tenant-aware operations.</returns>
        /// <exception cref="ArgumentException">If the tenant ID argument is null or empty.
        /// </exception>
        public TenantAwareFirebaseAuth AuthForTenant(string tenantId)
        {
            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentException("Tenant ID cannot be null or empty.");
            }

            TenantAwareFirebaseAuth auth;
            lock (this.tenantsLock)
            {
                if (this.disposed)
                {
                    throw new ObjectDisposedException("TenantManager instance already disposed.");
                }

                if (!this.tenants.TryGetValue(tenantId, out auth))
                {
                    auth = TenantAwareFirebaseAuth.Create(this.app, tenantId);
                    this.tenants[tenantId] = auth;
                }
            }

            return auth;
        }

        /// <summary>
        /// Cleans up and invalidates this instance. For internal use only.
        /// </summary>
        void IDisposable.Dispose()
        {
            this.httpClient.Dispose();
            lock (this.tenantsLock)
            {
                this.disposed = true;
                foreach (var auth in this.tenants.Values)
                {
                    (auth as IFirebaseService).Delete();
                }

                this.tenants.Clear();
            }
        }

        internal static TenantManager Create(FirebaseApp app)
        {
            var args = new Args()
            {
                App = app,
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
            internal FirebaseApp App { get; set; }

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

        private sealed class PageManager : IPageManager<ListTenantsRequest, TenantsPage, Tenant>
        {
            public void SetPageSize(ListTenantsRequest request, int pageSize)
            {
                request.SetPageSize(pageSize);
            }

            public void SetPageToken(ListTenantsRequest request, string pageToken)
            {
                request.SetPageToken(pageToken);
            }

            public IEnumerable<Tenant> GetResources(TenantsPage response)
            {
                return response?.Tenants;
            }

            public string GetNextPageToken(TenantsPage response)
            {
                return string.IsNullOrEmpty(response.NextPageToken) ?
                    null : response.NextPageToken;
            }
        }

        private sealed class ListTenantsRequest
        : AdaptedListResourcesRequest<TenantsPage, FirebaseAuthException>
        {
            internal ListTenantsRequest(
                string baseUrl,
                ListTenantsOptions options,
                ErrorHandlingHttpClient<FirebaseAuthException> httpClient)
            : base(baseUrl, options?.PageToken, options?.PageSize, httpClient) { }

            public override string RestPath => "tenants";

            public override HttpRequestMessage CreateRequest(bool? overrideGZipEnabled = null)
            {
                var request = base.CreateRequest(overrideGZipEnabled);
                request.Headers.Add(ClientVersionHeader, ClientVersion);
                return request;
            }

            public override async Task<TenantsPage> ExecuteAsync(
                CancellationToken cancellationToken)
            {
                var response = await this
                    .SendAndDeserializeAsync<ListResponse>(cancellationToken)
                    .ConfigureAwait(false);
                var tenants = response.Tenants?.Select(args => new Tenant(args));
                return new TenantsPage
                {
                    NextPageToken = response.NextPageToken,
                    Tenants = tenants,
                };
            }
        }

        private sealed class ListResponse
        {
            [JsonProperty("nextPageToken")]
            public string NextPageToken { get; set; }

            [JsonProperty("tenants")]
            public IEnumerable<TenantArgs> Tenants { get; set; }
        }
    }
}
