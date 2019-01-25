// Copyright 2019, Google Inc. All rights reserved.
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
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// FirebaseUserManager provides methods for interacting with the
    /// <a href="https://developers.google.com/identity/toolkit/web/reference/relyingparty">
    /// Google Identity Toolkit</a> via its REST API. This class does not hold any mutable state,
    /// and is thread safe.
    /// </summary>
    internal class FirebaseUserManager : IDisposable
    {
        private const string IdTooklitUrl = "https://identitytoolkit.googleapis.com/v1/projects/{0}";

        private readonly ConfigurableHttpClient httpClient;
        private readonly string baseUrl;

        internal FirebaseUserManager(FirebaseUserManagerArgs args)
        {
            this.httpClient = args.ClientFactory.CreateAuthorizedHttpClient(args.Credential);
            this.baseUrl = string.Format(IdTooklitUrl, args.ProjectId);
        }

        public static FirebaseUserManager Create(FirebaseApp app)
        {
            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to manage users.");
            }

            var args = new FirebaseUserManagerArgs
            {
                ClientFactory = new HttpClientFactory(),
                Credential = app.Options.Credential,
                ProjectId = projectId,
            };

            return new FirebaseUserManager(args);
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <exception cref="FirebaseException">If the server responds that cannot update the user.</exception>
        /// <param name="user">The user which we want to update.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task UpdateUserAsync(
            UserRecord user, CancellationToken cancellationToken = default(CancellationToken))
        {
            const string updatePath = "accounts:update";
            var response = await this.PostAndDeserializeAsync<JObject>(
                updatePath, user, cancellationToken).ConfigureAwait(false);
            if (user.Uid != (string)response["localId"])
            {
                throw new FirebaseException($"Failed to update user: {user.Uid}");
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private async Task<TResult> PostAndDeserializeAsync<TResult>(
            string path, object body, CancellationToken cancellationToken)
        {
            try
            {
                var json = await this.PostAsync(path, body, cancellationToken)
                    .ConfigureAwait(false);
                return NewtonsoftJsonSerializer.Instance.Deserialize<TResult>(json);
            }
            catch (FirebaseException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new FirebaseException("Error while calling Firebase Auth service", e);
            }
        }

        private async Task<string> PostAsync(
            string path, object body, CancellationToken cancellationToken)
        {
            var requestUri = $"{this.baseUrl}/{path}";
            var response = await this.httpClient
                .PostJsonAsync(requestUri, body, cancellationToken)
                .ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                var error = "Response status code does not indicate success: "
                        + $"{(int)response.StatusCode} ({response.StatusCode})"
                        + $"{Environment.NewLine}{json}";
                throw new FirebaseException(error);
            }

            return json;
        }
    }
}
