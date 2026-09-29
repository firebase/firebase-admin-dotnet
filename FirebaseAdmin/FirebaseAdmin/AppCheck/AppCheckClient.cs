// Copyright 2026, Google Inc. All rights reserved.
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
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// A client for the App Check backend service. Used to consume limited-use App Check tokens
    /// for replay protection.
    /// </summary>
    internal sealed class AppCheckClient : IDisposable
    {
        private const string VerifyTokenUrlFormat =
            "https://firebaseappcheck.googleapis.com/v1beta/projects/{0}:verifyAppCheckToken";

        private readonly ErrorHandlingHttpClient<FirebaseAppCheckException> httpClient;
        private readonly string verifyTokenUrl;

        internal AppCheckClient(
            HttpClientFactory clientFactory, GoogleCredential credential, string projectId)
        {
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Project ID is required to access App Check service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.");
            }

            // No retries: a retried request may report a first-time token as already consumed.
            this.httpClient = new ErrorHandlingHttpClient<FirebaseAppCheckException>(
                new ErrorHandlingHttpClientArgs<FirebaseAppCheckException>()
                {
                    HttpClientFactory = clientFactory.ThrowIfNull(nameof(clientFactory)),
                    Credential = credential.ThrowIfNull(nameof(credential)),
                    ErrorResponseHandler = AppCheckErrorHandler.Instance,
                    RequestExceptionHandler = AppCheckErrorHandler.Instance,
                    DeserializeExceptionHandler = AppCheckErrorHandler.Instance,
                });
            this.verifyTokenUrl = string.Format(VerifyTokenUrlFormat, projectId);
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Consumes the given App Check token, and returns whether it had already been consumed.
        /// The token must be verified locally before calling this method.
        /// </summary>
        internal async Task<bool> VerifyReplayProtectionAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(this.verifyTokenUrl),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(
                    new VerifyTokenRequest() { AppCheckToken = token }),
            };
            var response = await this.httpClient
                .SendAndDeserializeAsync<VerifyTokenResponse>(request, cancellationToken)
                .ConfigureAwait(false);
            return response.Result?.AlreadyConsumed ?? false;
        }

        private sealed class VerifyTokenRequest
        {
            [JsonProperty("app_check_token")]
            internal string AppCheckToken { get; set; }
        }

        private sealed class VerifyTokenResponse
        {
            [JsonProperty("alreadyConsumed")]
            internal bool? AlreadyConsumed { get; set; }
        }
    }
}
