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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Api.Gax;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth.Providers
{
    internal sealed class SamlProviderConfigClient
    : ProviderConfigClient<SamlProviderConfig>
    {
        internal static readonly SamlProviderConfigClient Instance =
            new SamlProviderConfigClient();

        private SamlProviderConfigClient() { }

        internal override async Task<SamlProviderConfig> GetProviderConfigAsync(
            ApiClient client, string providerId, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = BuildUri($"inboundSamlConfigs/{this.ValidateProviderId(providerId)}"),
            };
            var response = await client
                .SendAndDeserializeAsync<SamlProviderConfig.Request>(request, cancellationToken)
                .ConfigureAwait(false);
            return new SamlProviderConfig(response);
        }

        internal override async Task<SamlProviderConfig> CreateProviderConfigAsync(
            ApiClient client,
            AuthProviderConfigArgs<SamlProviderConfig> args,
            CancellationToken cancellationToken)
        {
            var query = new Dictionary<string, object>()
            {
                { "inboundSamlConfigId", this.ValidateProviderId(args.ProviderId) },
            };
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = BuildUri("inboundSamlConfigs", query),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(
                    args.ToCreateRequest()),
            };
            var response = await client
                .SendAndDeserializeAsync<SamlProviderConfig.Request>(request, cancellationToken)
                .ConfigureAwait(false);
            return new SamlProviderConfig(response);
        }

        internal override Task<SamlProviderConfig> UpdateProviderConfigAsync(
            ApiClient client,
            AuthProviderConfigArgs<SamlProviderConfig> args,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal override PagedAsyncEnumerable<AuthProviderConfigs<SamlProviderConfig>, SamlProviderConfig>
            ListProviderConfigsAsync(ApiClient client, ListProviderConfigsOptions options)
        {
            throw new NotImplementedException();
        }

        private string ValidateProviderId(string providerId)
        {
            if (string.IsNullOrEmpty(providerId))
            {
                throw new ArgumentException("Provider ID cannot be null or empty.");
            }

            if (!providerId.StartsWith("saml."))
            {
                throw new ArgumentException("SAML provider ID must have the prefix 'saml.'.");
            }

            return providerId;
        }
    }
}
