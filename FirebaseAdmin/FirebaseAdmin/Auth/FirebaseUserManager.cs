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

using FirebaseAdmin.Auth.Internal;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// FirebaseUserManager provides methods for interacting with the 
    /// <a href="https://developers.google.com/identity/toolkit/web/reference/relyingparty">
    /// Google Identity Toolkit</a> via its REST API. This class does not hold any mutable state, 
    /// and is thread safe.
    /// </summary>
    internal class FirebaseUserManager
    {
        private const string INTERNAL_ERROR = "internal-error";

        // Map of server-side error codes to SDK error codes.
        // SDK error codes defined at: https://firebase.google.com/docs/auth/admin/errors
        private static readonly IReadOnlyDictionary<string, string> _errorCodes =
            new ReadOnlyDictionary<string, string>(new Dictionary<string, string>()
            {
                { "CLAIMS_TOO_LARGE", "claims-too-large" },
                { "CONFIGURATION_NOT_FOUND", "project-not-found"  },
                { "INSUFFICIENT_PERMISSION", "insufficient-permission" },
                { "DUPLICATE_EMAIL", "email-already-exists" },
                { "DUPLICATE_LOCAL_ID", "uid-already-exists" },
                { "EMAIL_EXISTS", "email-already-exists" },
                { "INVALID_CLAIMS", "invalid-claims" },
                { "INVALID_EMAIL", "invalid-email" },
                { "INVALID_PAGE_SELECTION", "invalid-page-token" },
                { "INVALID_PHONE_NUMBER", "invalid-phone-number" },
                { "PHONE_NUMBER_EXISTS", "phone-number-already-exists" },
                { "PROJECT_NOT_FOUND", "project-not-found" },
                { "USER_NOT_FOUND", "user-not-found" },
                { "WEAK_PASSWORD", "invalid-password" },
                { "UNAUTHORIZED_DOMAIN", "unauthorized-continue-uri" },
                { "INVALID_DYNAMIC_LINK_DOMAIN", "invalid-dynamic-link-domain" }
            });

        private const string ID_TOOLKIT_URL = "https://identitytoolkit.googleapis.com/v1/projects/{0}";

        private readonly ConfigurableHttpClient _httpClient;
        private readonly string _baseUrl;

        private FirebaseUserManager(FirebaseUserManagerArgs args)
        {
            _httpClient = args.ClientFactory.CreateAuthorizedHttpClient(args.Credential);
            _baseUrl = string.Format(ID_TOOLKIT_URL, args.ProjectId);
        }

        /// <summary>
        /// Update an existing user.
        /// </summary>
        /// <exception cref="FirebaseException">If the server responds that cannot update the user.</exception>
        /// <param name="user">The user which we want to update.</param>
        public async Task UpdateUser(UserRecord user)
        {
            var updatePath = "/accounts:update";
            var resopnse = await PostAsync(updatePath, user);

            var userResponse = resopnse.ToObject<UserRecord>();
            if (userResponse == null || userResponse.Uid != user.Uid)
            {
                throw new FirebaseException(INTERNAL_ERROR);
            }
        }

        private async Task<JObject> PostAsync(string path, UserRecord user)
        {
            var requestUri = $"{_baseUrl}{path}";
            HttpResponseMessage response = null;
            try
            {
                response = await _httpClient.PostJsonAsync(requestUri, user, default);

                if (response.IsSuccessStatusCode)
                {
                    return JObject.Parse(await response.Content.ReadAsStringAsync());
                }
                else
                {
                    await HandleHttpError(response);
                }
            }
            catch (Exception)
            {
                throw new FirebaseException(INTERNAL_ERROR);
            }
            finally
            {
                if (response != null)
                {
                    response.Dispose();
                }
            }

            return null;
        }

        private async Task HandleHttpError(HttpResponseMessage response)
        {
            try
            {
                var errorResponse =
                    NewtonsoftJsonSerializer.Instance.Deserialize<HttpErrorResponse>(await response.Content.ReadAsStringAsync());
                if (_errorCodes.TryGetValue(errorResponse.ErrorCode, out var code))
                {
                    throw new FirebaseException(code);
                };
            }
            catch (Exception)
            {
                // Ignored
            }

            throw new FirebaseException(INTERNAL_ERROR);
        }

        internal static FirebaseUserManager Create(FirebaseApp app)
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
    }

    internal sealed class FirebaseUserManagerArgs
    {
        public HttpClientFactory ClientFactory { get; set; }
        public GoogleCredential Credential { get; set; }
        public string ProjectId { get; set; }
    }
}
