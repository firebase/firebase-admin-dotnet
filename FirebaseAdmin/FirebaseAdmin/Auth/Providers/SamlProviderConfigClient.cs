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

namespace FirebaseAdmin.Auth.Providers
{
    internal sealed class SamlProviderConfigClient
    : ProviderConfigClient<SamlProviderConfig>
    {
        internal static readonly SamlProviderConfigClient Instance =
            new SamlProviderConfigClient();

        private SamlProviderConfigClient() { }

        protected override string ResourcePath => "inboundSamlConfigs";

        protected override string ProviderIdParam => "inboundSamlConfigId";

        protected override string ValidateProviderId(string providerId)
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

        protected override async Task<SamlProviderConfig> SendAndDeserializeAsync(
            ApiClient client, HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = await client
                .SendAndDeserializeAsync<SamlProviderConfig.Request>(request, cancellationToken)
                .ConfigureAwait(false);
            return new SamlProviderConfig(response);
        }

        protected override AbstractListRequest CreateListRequest(
            ApiClient client, ListProviderConfigsOptions options)
        {
            throw new NotImplementedException();
        }
    }
}
