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
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Api.Gax;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// The facade for managing auth provider configurations in a Firebase project. It is used by
    /// other high-level classes like <see cref="FirebaseAuth"/>. Remote API calls are delegated
    /// to the appropriate <see cref="ProviderConfigClient{T}"/> implementations, with
    /// <see cref="ApiClient"/> providing the necessary HTTP primitives.
    /// </summary>
    internal sealed class ProviderConfigManager : IDisposable
    {
        private readonly ApiClient apiClient;

        internal ProviderConfigManager(Args args)
        {
            args.ThrowIfNull(nameof(args));
            this.TenantId = args.TenantId;
            if (this.TenantId == string.Empty)
            {
                throw new ArgumentException("Tenant ID must not be empty.");
            }

            var clientArgs = new ErrorHandlingHttpClientArgs<FirebaseAuthException>()
            {
                HttpClientFactory = args.ClientFactory,
                Credential = args.Credential,
                ErrorResponseHandler = AuthErrorHandler.Instance,
                RequestExceptionHandler = AuthErrorHandler.Instance,
                DeserializeExceptionHandler = AuthErrorHandler.Instance,
                RetryOptions = args.RetryOptions,
            };
            this.apiClient = new ApiClient(args.ProjectId, this.TenantId, clientArgs);
        }

        internal string TenantId { get; }

        public void Dispose()
        {
            this.apiClient.Dispose();
        }

        internal static ProviderConfigManager Create(FirebaseApp app, string tenantId = null)
        {
            var args = new Args
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                ProjectId = app.GetProjectId(),
                RetryOptions = RetryOptions.Default,
                TenantId = tenantId,
            };

            return new ProviderConfigManager(args);
        }

        internal async Task<OidcProviderConfig> GetOidcProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            return await OidcProviderConfigClient.Instance
                .GetProviderConfigAsync(this.apiClient, providerId, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<SamlProviderConfig> GetSamlProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            return await SamlProviderConfigClient.Instance
                .GetProviderConfigAsync(this.apiClient, providerId, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<T> CreateProviderConfigAsync<T>(
            AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
            where T : AuthProviderConfig
        {
            var client = args.ThrowIfNull(nameof(args)).GetClient();
            return await client.CreateProviderConfigAsync(this.apiClient, args, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task<T> UpdateProviderConfigAsync<T>(
            AuthProviderConfigArgs<T> args, CancellationToken cancellationToken)
            where T : AuthProviderConfig
        {
            var client = args.ThrowIfNull(nameof(args)).GetClient();
            return await client.UpdateProviderConfigAsync(this.apiClient, args, cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task DeleteProviderConfigAsync(
            string providerId, CancellationToken cancellationToken)
        {
            providerId.ThrowIfNullOrEmpty(nameof(providerId));
            if (providerId.StartsWith("oidc."))
            {
                await OidcProviderConfigClient.Instance
                    .DeleteProviderConfigAsync(this.apiClient, providerId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (providerId.StartsWith("saml."))
            {
                await SamlProviderConfigClient.Instance
                    .DeleteProviderConfigAsync(this.apiClient, providerId, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new ArgumentException(
                    "Provider ID must have 'oidc.' or 'saml.' as the prefix.");
            }
        }

        internal PagedAsyncEnumerable<AuthProviderConfigs<OidcProviderConfig>, OidcProviderConfig>
            ListOidcProviderConfigsAsync(ListProviderConfigsOptions options)
        {
            return OidcProviderConfigClient.Instance.ListProviderConfigsAsync(
                this.apiClient, options);
        }

        internal PagedAsyncEnumerable<AuthProviderConfigs<SamlProviderConfig>, SamlProviderConfig>
            ListSamlProviderConfigsAsync(ListProviderConfigsOptions options)
        {
            return SamlProviderConfigClient.Instance.ListProviderConfigsAsync(
                this.apiClient, options);
        }

        internal sealed class Args
        {
            internal HttpClientFactory ClientFactory { get; set; }

            internal GoogleCredential Credential { get; set; }

            internal string ProjectId { get; set; }

            internal string TenantId { get; set; }

            internal RetryOptions RetryOptions { get; set; }
        }
    }
}
