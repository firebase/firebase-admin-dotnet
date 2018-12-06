// Copyright 2018, Google Inc. All rights reserved.
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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

namespace FirebaseAdmin.Messaging
{
    internal sealed class FirebaseMessagingClient: IDisposable
    {
        private const string FcmUrl = "https://fcm.googleapis.com/v1/projects/{0}/messages:send";

        private readonly ConfigurableHttpClient _httpClient;
        private readonly string _sendUrl;

        internal FirebaseMessagingClient(
            HttpClientFactory clientFactory, GoogleCredential credential, string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                throw new FirebaseException(
                    "Project ID is required to access messaging service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.");
            }
            _httpClient = clientFactory.ThrowIfNull(nameof(clientFactory))
                .CreateAuthorizedHttpClient(credential);
            _sendUrl = string.Format(FcmUrl, projectId);
        }

        public async Task<string> SendAsync(Message message,
            bool dryRun = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new SendRequest()
            {
                Message = message.ThrowIfNull(nameof(message)).Validate(),
                ValidateOnly = dryRun,
            };
            try
            {
                var response = await _httpClient.PostJsonAsync(
                    _sendUrl, request, cancellationToken).ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var error = "Response status code does not indicate success: "
                            + $"{(int) response.StatusCode} ({response.StatusCode})"
                            + $"{Environment.NewLine}{json}";
                    throw new FirebaseException(error);   
                }
                var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<SendResponse>(json);
                return parsed.Name;
            }
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling the FCM service.", e);
            }
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }
    }

    internal sealed class SendRequest
    {
        [Newtonsoft.Json.JsonProperty("message")]
        public ValidatedMessage Message { get; set; }

        [Newtonsoft.Json.JsonProperty("validate_only")]
        public bool ValidateOnly { get; set; }
    }

    internal sealed class SendResponse
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }
    }
}
