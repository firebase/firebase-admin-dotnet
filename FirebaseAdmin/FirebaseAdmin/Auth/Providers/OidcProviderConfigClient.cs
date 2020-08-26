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
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth.Providers
{
    internal sealed class OidcProviderConfigClient
    : ProviderConfigClient<OidcProviderConfig>
    {
        internal static readonly OidcProviderConfigClient Instance =
            new OidcProviderConfigClient();

        private OidcProviderConfigClient() { }

        protected override string ResourcePath => "oauthIdpConfigs";

        protected override string ProviderIdParam => "oauthIdpConfigId";

        protected override string ValidateProviderId(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                throw new ArgumentException("Provider ID cannot be null or empty.");
            }

            if (!providerId.StartsWith("oidc."))
            {
                throw new ArgumentException("OIDC provider ID must have the prefix 'oidc.'.");
            }

            return providerId;
        }

        protected override async Task<OidcProviderConfig> SendAndDeserializeAsync(
            ApiClient client, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await client
                .SendAndDeserializeAsync<OidcProviderConfig.Request>(request, cancellationToken)
                .ConfigureAwait(false);
            return new OidcProviderConfig(response);
        }

        protected override AbstractListRequest CreateListRequest(
            ApiClient client, ListProviderConfigsOptions options)
        {
            return new ListRequest(client, options);
        }

        private sealed class ListRequest : AbstractListRequest
        {
            internal ListRequest(ApiClient client, ListProviderConfigsOptions options)
            : base(client, options) { }

            public override string RestPath => "oauthIdpConfigs";

            public override async Task<AuthProviderConfigs<OidcProviderConfig>> ExecuteAsync(
                CancellationToken cancellationToken)
            {
                var request = this.CreateRequest();
                var response = await this.ApiClient
                    .SendAndDeserializeAsync<ListResponse>(request, cancellationToken)
                    .ConfigureAwait(false);
                var configs = response.Configs?.Select(config => new OidcProviderConfig(config));
                return new AuthProviderConfigs<OidcProviderConfig>
                {
                    NextPageToken = response.NextPageToken,
                    ProviderConfigs = configs,
                };
            }
        }

        private sealed class ListResponse
        {
            /// <summary>
            /// Gets or sets the next page link.
            /// </summary>
            [JsonProperty("nextPageToken")]
            public string NextPageToken { get; set; }

            /// <summary>
            /// Gets or sets the users.
            /// </summary>
            [JsonProperty("oauthIdpConfigs")]
            public IEnumerable<OidcProviderConfig.Request> Configs { get; set; }
        }
    }
}
