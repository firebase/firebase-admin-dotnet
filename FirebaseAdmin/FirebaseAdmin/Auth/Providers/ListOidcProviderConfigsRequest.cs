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

using System.Collections.Generic;
using System.Linq;
using FirebaseAdmin.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Auth.Providers
{
    /// <summary>
    /// A request made using the Google API client to list all OIDC provider configurations in a
    /// Firebase project.
    /// </summary>
    internal sealed class ListOidcProviderConfigsRequest
    : ListProviderConfigsRequest<OidcProviderConfig>
    {
        internal ListOidcProviderConfigsRequest(
            string baseUrl,
            ErrorHandlingHttpClient<FirebaseAuthException> httpClient,
            ListProviderConfigsOptions options)
            : base(baseUrl, httpClient, options) { }

        public override string MethodName => "ListOidcProviderConfigs";

        public override string RestPath => "oauthIdpConfigs";

        protected override AuthProviderConfigs<OidcProviderConfig> BuildProviderConfigs(JObject json)
        {
            var response = json.ToObject<Response>();
            var configs = response.Configs?.Select(config => new OidcProviderConfig(config));
            return new AuthProviderConfigs<OidcProviderConfig>
            {
                NextPageToken = response.NextPageToken,
                ProviderConfigs = configs,
            };
        }

        internal sealed class Response
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
