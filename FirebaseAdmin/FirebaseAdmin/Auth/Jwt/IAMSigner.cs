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
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// An <see cref="ISigner"/> implementation that uses the
    /// <a href="https://cloud.google.com/iam/docs/reference/credentials/rest/v1/projects.serviceAccounts/signBlob">
    /// IAMCredentials service</a> to sign data. The IAMCredentials
    /// service must be called with a service account ID, and this class attempts to auto
    /// discover a service account ID by contacting the local metadata service present in
    /// Google-managed runtimes.
    /// </summary>
    internal class IAMSigner : ISigner
    {
        private const string SignBlobUrl =
            "https://iamcredentials.googleapis.com/v1/projects/-/serviceAccounts/{0}:signBlob";

        private const string MetadataServerUrl =
            "http://metadata.google.internal/computeMetadata/v1/instance/service-accounts/default/email";

        private readonly ErrorHandlingHttpClient<FirebaseAuthException> httpClient;
        private readonly Lazy<Task<string>> keyId;

        public IAMSigner(
            HttpClientFactory clientFactory, GoogleCredential credential, RetryOptions retryOptions = null)
        {
            this.httpClient = new ErrorHandlingHttpClient<FirebaseAuthException>(
                new ErrorHandlingHttpClientArgs<FirebaseAuthException>()
                {
                    HttpClientFactory = clientFactory,
                    Credential = credential,
                    ErrorResponseHandler = IAMSignerErrorHandler.Instance,
                    RequestExceptionHandler = AuthErrorHandler.Instance,
                    DeserializeExceptionHandler = AuthErrorHandler.Instance,
                    RetryOptions = retryOptions,
                });
            this.keyId = new Lazy<Task<string>>(
                async () => await DiscoverServiceAccountIdAsync(clientFactory)
                    .ConfigureAwait(false), true);
        }

        public async Task<byte[]> SignDataAsync(
            byte[] data, CancellationToken cancellationToken = default(CancellationToken))
        {
            var keyId = await this.GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var body = new SignBlobRequest
            {
                BytesToSign = Convert.ToBase64String(data),
            };
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(string.Format(SignBlobUrl, keyId)),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };

            var response = await this.httpClient
                .SendAndDeserializeAsync<SignBlobResponse>(request, cancellationToken)
                .ConfigureAwait(false);
            return Convert.FromBase64String(response.Result.Signature);
        }

        public virtual async Task<string> GetKeyIdAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await this.keyId.Value.ConfigureAwait(false);
            }
            catch (Exception e)
            {
                // Invalid configuration or environment error.
                throw new InvalidOperationException(
                    "Failed to determine service account ID. Make sure to initialize the SDK "
                        + "with service account credentials or specify a service account "
                        + "ID with iam.serviceAccounts.signBlob permission. Please refer to "
                        + "https://firebase.google.com/docs/auth/admin/create-custom-tokens for "
                        + "more details on creating custom tokens.", e);
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        internal static IAMSigner Create(FirebaseApp app)
        {
            return new IAMSigner(
                app.Options.HttpClientFactory, app.Options.Credential, RetryOptions.Default);
        }

        private static async Task<string> DiscoverServiceAccountIdAsync(
            HttpClientFactory clientFactory)
        {
            using (var client = clientFactory.CreateDefaultHttpClient())
            {
                client.DefaultRequestHeaders.Add("Metadata-Flavor", "Google");
                var resp = await client.GetAsync(MetadataServerUrl).ConfigureAwait(false);
                resp.EnsureSuccessStatusCode();
                return await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Represents the sign request sent to the remote IAM service.
        /// </summary>
        internal class SignBlobRequest
        {
            [Newtonsoft.Json.JsonProperty("payload")]
            public string BytesToSign { get; set; }
        }

        /// <summary>
        /// Represents the sign response sent by the remote IAM service.
        /// </summary>
        internal class SignBlobResponse
        {
            [Newtonsoft.Json.JsonProperty("signedBlob")]
            public string Signature { get; set; }
        }

        private class IAMSignerErrorHandler : PlatformErrorHandler<FirebaseAuthException>
        {
            internal static readonly IAMSignerErrorHandler Instance = new IAMSignerErrorHandler();

            private IAMSignerErrorHandler() { }

            protected override FirebaseAuthException CreateException(FirebaseExceptionArgs args)
            {
                return new FirebaseAuthException(args.Code, args.Message, response: args.HttpResponse);
            }
        }
    }
}
