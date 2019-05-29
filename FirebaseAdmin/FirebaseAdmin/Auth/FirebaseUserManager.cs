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
using System.Collections.Generic;
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
            if (string.IsNullOrEmpty(args.ProjectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to manage users.");
            }

            this.httpClient = args.ClientFactory.CreateAuthorizedHttpClient(args.Credential);
            this.baseUrl = string.Format(IdTooklitUrl, args.ProjectId);
        }

        public static FirebaseUserManager Create(FirebaseApp app)
        {
            var args = new FirebaseUserManagerArgs
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                ProjectId = app.GetProjectId(),
            };

            return new FirebaseUserManager(args);
        }

        /// <summary>
        /// Gets the user data corresponding to the given user ID.
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A record of user with the queried id if one exists.</returns>
        public async Task<UserRecord> GetUserById(
            string uid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("User ID cannot be null or empty.");
            }

            var payload = new Dictionary<string, object>()
            {
                { "localId", uid },
            };

            var response = await this.PostAndDeserializeAsync<GetAccountInfoResponse>(
                "accounts:lookup", payload, cancellationToken).ConfigureAwait(false);
            if (response == null || response.Users == null || response.Users.Count == 0)
            {
                throw new FirebaseException($"Failed to get user by ID: {uid}");
            }

            return new UserRecord(response.Users[0]);
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <exception cref="FirebaseException">If the server responds that cannot update the user.</exception>
        /// <param name="args">The user account data to be updated.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task UpdateUserAsync(
            UserArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            var payload = args.ToUpdateUserRequest();
            var response = await this.PostAndDeserializeAsync<JObject>(
                "accounts:update", payload, cancellationToken).ConfigureAwait(false);
            if (payload.Uid != (string)response["localId"])
            {
                throw new FirebaseException($"Failed to update user: {payload.Uid}");
            }
        }

        /// <summary>
        /// Delete user data corresponding to the given user ID.
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task DeleteUserAsync(
            string uid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("User id cannot be null or empty.");
            }

            var payload = new Dictionary<string, object>()
            {
                { "localId", uid },
            };
            var response = await this.PostAndDeserializeAsync<JObject>(
                "accounts:delete", payload, cancellationToken).ConfigureAwait(false);
            if (response == null || (string)response["kind"] == null)
            {
                throw new FirebaseException($"Failed to delete user: {uid}");
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private async Task<TResult> PostAndDeserializeAsync<TResult>(
            string path, object body, CancellationToken cancellationToken)
        {
            var json = await this.PostAsync(path, body, cancellationToken).ConfigureAwait(false);
            return this.SafeDeserialize<TResult>(json);
        }

        private TResult SafeDeserialize<TResult>(string json)
        {
            try
            {
                return NewtonsoftJsonSerializer.Instance.Deserialize<TResult>(json);
            }
            catch (Exception e)
            {
                throw new FirebaseException("Error while parsing Auth service response", e);
            }
        }

        private async Task<string> PostAsync(
            string path, object body, CancellationToken cancellationToken)
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"{this.baseUrl}/{path}"),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };
            return await this.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        private async Task<string> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.httpClient.SendAsync(request, cancellationToken)
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
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling Firebase Auth service", e);
            }
        }
    }
}
