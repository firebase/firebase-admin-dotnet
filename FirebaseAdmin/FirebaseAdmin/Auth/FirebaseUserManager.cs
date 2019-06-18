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
using Google.Api.Gax;
using Google.Api.Gax.Rest;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
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

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        internal static FirebaseUserManager Create(FirebaseApp app)
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
        internal async Task<UserRecord> GetUserByIdAsync(
            string uid, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("User ID cannot be null or empty.");
            }

            var query = new UserQuery()
            {
                Field = "localId",
                Value = uid,
                Label = "uid",
            };
            return await this.GetUserAsync(query, cancellationToken);
        }

        /// <summary>
        /// Gets the user data corresponding to the given email address.
        /// </summary>
        /// <param name="email">An email address.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A record of user with the queried email if one exists.</returns>
        internal async Task<UserRecord> GetUserByEmailAsync(
            string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty.");
            }

            var query = new UserQuery()
            {
                Field = "email",
                Value = email,
            };
            return await this.GetUserAsync(query, cancellationToken);
        }

        /// <summary>
        /// Gets the user data corresponding to the given phone number.
        /// </summary>
        /// <param name="phoneNumber">A phone number.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A record of user with the queried phone number if one exists.</returns>
        internal async Task<UserRecord> GetUserByPhoneNumberAsync(
            string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentException("Phone number cannot be null or empty.");
            }

            var query = new UserQuery()
            {
                Field = "phoneNumber",
                Value = phoneNumber,
                Label = "phone number",
            };
            return await this.GetUserAsync(query, cancellationToken);
        }

        internal PagedAsyncEnumerable<ExportedUserRecords, ExportedUserRecord> ListUsers(
            ListUsersOptions options)
        {
            var factory = new ListUsersRequest.Factory(this.baseUrl, this.httpClient, options);
            return new RestPagedAsyncEnumerable
                <ListUsersRequest, ExportedUserRecords, ExportedUserRecord>(
                () => factory.Create(),
                new ListUsersPageManager());
        }

        /// <summary>
        /// Create a new user account.
        /// </summary>
        /// <exception cref="FirebaseException">If an error occurs while creating the user account.</exception>
        /// <param name="args">The data to create the user account with.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>The unique uid assigned to the newly created user account.</returns>
        internal async Task<string> CreateUserAsync(
            UserRecordArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            var payload = args.ThrowIfNull(nameof(args)).ToCreateUserRequest();
            var response = await this.PostAndDeserializeAsync<JObject>(
                "accounts", payload, cancellationToken).ConfigureAwait(false);
            var uid = response["localId"];
            if (uid == null)
            {
                throw new FirebaseException("Failed to create new user.");
            }

            return uid.Value<string>();
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <exception cref="FirebaseException">If the server responds that cannot update the user.</exception>
        /// <param name="args">The user account data to be updated.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        internal async Task<string> UpdateUserAsync(
            UserRecordArgs args, CancellationToken cancellationToken = default(CancellationToken))
        {
            var payload = args.ToUpdateUserRequest();
            var response = await this.PostAndDeserializeAsync<JObject>(
                "accounts:update", payload, cancellationToken).ConfigureAwait(false);
            if (payload.Uid != (string)response["localId"])
            {
                throw new FirebaseException($"Failed to update user: {payload.Uid}");
            }

            return payload.Uid;
        }

        /// <summary>
        /// Delete user data corresponding to the given user ID.
        /// </summary>
        /// <param name="uid">A user ID string.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        internal async Task DeleteUserAsync(
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

        private async Task<UserRecord> GetUserAsync(UserQuery query, CancellationToken cancellationToken)
        {
            var response = await this.PostAndDeserializeAsync<GetAccountInfoResponse>(
                "accounts:lookup", query.Build(), cancellationToken).ConfigureAwait(false);
            if (response == null || response.Users == null || response.Users.Count == 0)
            {
                throw new FirebaseException($"Failed to get user with {query.Description}");
            }

            return new UserRecord(response.Users[0]);
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

        /// <summary>
        /// Represents a query that can be executed against the Firebase Auth service to retrieve user records.
        /// A query mainly consists of a <see cref="UserQuery.Field"/> and a <see cref="UserQuery.Value"/> (e.g.
        /// <c>Field = localId</c> and <c>Value = alice</c>). Additionally, a query may also specify a more
        /// human-readable <see cref="UserQuery.Label"/> for the field, which will appear on any error messages
        /// produced by the query.
        /// </summary>
        private class UserQuery
        {
            internal string Field { get; set; }

            internal string Value { get; set; }

            internal string Label { get; set; }

            internal string Description
            {
                get
                {
                    var label = this.Label;
                    if (string.IsNullOrEmpty(label))
                    {
                        label = this.Field;
                    }

                    return $"{label}: {this.Value}";
                }
            }

            internal Dictionary<string, object> Build()
            {
                return new Dictionary<string, object>()
                {
                    { this.Field, new string[] { this.Value } },
                };
            }
        }
    }
}
