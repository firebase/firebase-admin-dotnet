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
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// An <see cref="ISigner"/> implementation that uses the
    /// <a href="https://cloud.google.com/iam/reference/rest/v1/projects.serviceAccounts/signBlob">IAM
    /// service</a> to sign data. The IAM
    /// service must be called with a service account ID, and this class attempts to auto
    /// discover a service account ID by contacting the local metadata service present in
    /// Google-managed runtimes.
    /// </summary>
    internal class IAMSigner : ISigner
    {
        private const string SignBlobUrl =
            "https://iam.googleapis.com/v1/projects/-/serviceAccounts/{0}:signBlob";

        private const string MetadataServerUrl =
            "http://metadata/computeMetadata/v1/instance/service-accounts/default/email";

        private readonly ConfigurableHttpClient httpClient;
        private readonly Lazy<Task<string>> keyId;

        public IAMSigner(HttpClientFactory clientFactory, GoogleCredential credential)
        {
            this.httpClient = clientFactory.CreateAuthorizedHttpClient(credential);
            this.keyId = new Lazy<Task<string>>(
                async () => await DiscoverServiceAccountIdAsync(clientFactory)
                    .ConfigureAwait(false), true);
        }

        public async Task<byte[]> SignDataAsync(
            byte[] data, CancellationToken cancellationToken = default(CancellationToken))
        {
            var keyId = await GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var url = string.Format(SignBlobUrl, keyId);
            var request = new SignBlobRequest
            {
                BytesToSign = Convert.ToBase64String(data),
            };

            try
            {
                var response = await httpClient.PostJsonAsync(url, request, cancellationToken)
                    .ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                ThrowIfError(response, json);
                var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<SignBlobResponse>(json);
                return Convert.FromBase64String(parsed.Signature);
            }
            catch (HttpRequestException e)
            {
                throw new FirebaseException("Error while calling the IAM service.", e);
            }
        }

        public virtual async Task<string> GetKeyIdAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await keyId.Value.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new FirebaseException(
                    "Failed to determine service account ID. Make sure to initialize the SDK "
                        + "with service account credentials or specify a service account "
                        + "ID with iam.serviceAccounts.signBlob permission. Please refer to "
                        + "https://firebase.google.com/docs/auth/admin/create-custom-tokens for "
                        + "more details on creating custom tokens.", e);
            }
        }

        public void Dispose()
        {
            httpClient.Dispose();
        }

        private static async Task<string> DiscoverServiceAccountIdAsync(
            HttpClientFactory clientFactory)
        {
            using (var client = clientFactory.CreateDefaultHttpClient())
            {
                client.DefaultRequestHeaders.Add("Metadata-Flavor", "Google");
                return await client.GetStringAsync(MetadataServerUrl).ConfigureAwait(false);
            }
        }

        private void ThrowIfError(HttpResponseMessage response, string content)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            string error = null;
            try
            {
                var result = NewtonsoftJsonSerializer.Instance.Deserialize<SignBlobError>(content);
                error = result?.Error.Message;
            }
            catch (Exception)
            {
                // Ignore any errors encountered while parsing the originl error.
            }

            if (string.IsNullOrEmpty(error))
            {
                error = "Response status code does not indicate success: "
                    + $"{(int)response.StatusCode} ({response.StatusCode})"
                    + $"{Environment.NewLine}{content}";
            }

            throw new FirebaseException(error);
        }

        /// <summary>
        /// Represents the sign request sent to the remote IAM service.
        /// </summary>
        internal class SignBlobRequest
        {
            [Newtonsoft.Json.JsonProperty("bytesToSign")]
            public string BytesToSign { get; set; }
        }

        /// <summary>
        /// Represents the sign response sent by the remote IAM service.
        /// </summary>
        internal class SignBlobResponse
        {
            [Newtonsoft.Json.JsonProperty("signature")]
            public string Signature { get; set; }
        }

        /// <summary>
        /// Represents an error response sent by the remote IAM service.
        /// </summary>
        private class SignBlobError
        {
            [Newtonsoft.Json.JsonProperty("error")]
            public SignBlobErrorDetail Error { get; set; }
        }

        /// <summary>
        /// Represents the error details embedded in an IAM error response.
        /// </summary>
        private class SignBlobErrorDetail
        {
            [Newtonsoft.Json.JsonProperty("message")]
            public string Message { get; set; }
        }
    }
}