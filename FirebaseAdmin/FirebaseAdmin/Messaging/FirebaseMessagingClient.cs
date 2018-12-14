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
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A client for making authorized HTTP calls to the FCM backend service. Handles request
    /// serialization, response parsing, and HTTP error handling.
    /// </summary>
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

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        internal async Task<string> SendAsync(Message message,
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
                var parsed = JsonConvert.DeserializeObject<SendResponse>(json);
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

    /// <summary>
    /// Represents the envelope message accepted by the FCM backend service, including the message
    /// payload and other options like <code>validate_only</code>.
    /// </summary>
    internal sealed class SendRequest
    {
        [Newtonsoft.Json.JsonProperty("message")]
        public ValidatedMessage Message { get; set; }

        [Newtonsoft.Json.JsonProperty("validate_only")]
        public bool ValidateOnly { get; set; }
    }

    /// <summary>
    /// Represents the response messages sent by the FCM backend service. Primarily consists of the
    /// message ID (Name) that indicates success handoff to FCM.
    /// </summary>
    internal sealed class SendResponse
    {
        [Newtonsoft.Json.JsonProperty("name")]
        public string Name { get; set; }
    }
}
