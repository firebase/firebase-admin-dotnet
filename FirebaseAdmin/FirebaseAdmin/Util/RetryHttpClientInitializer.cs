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

using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An HTTP client initializer that configures clients to retry HTTP failing requests on
    /// low-level exceptions and unsuccessful HTTP responses. Retry conditions and other parameters
    /// are configured via <see cref="RetryOptions"/>. Repeated retry attempts on the same
    /// request are delayed with exponential backoff. Retries due to unsuccessful responses
    /// that contain the "Retry-After" header are delayed according to the header value.
    /// </summary>
    internal sealed class RetryHttpClientInitializer : IConfigurableHttpClientInitializer
    {
        private readonly RetryOptions retryOptions;
        private readonly ExponentialBackOffInitializer backOffInitializer;

        internal RetryHttpClientInitializer(RetryOptions retryOptions)
        {
            this.retryOptions = retryOptions.ThrowIfNull(nameof(retryOptions));
            var backOffHandler = new FirebaseBackOffHandler(this.retryOptions);

            // ExponentialBackOffPolicy.Exception:
            // Instructs the initializer to register an exception handler with the underlying
            // ConfigurableMessageHandler. Whether to retry on a certain exception or not is
            // determined by the BackOffHandler.HandleExceptionFunc.

            // ExponentialBackOffPolicy.UnsuccessfulResponse503:
            // This constant is poorly named. It simply instructs the initializer to
            // register an unsuccessful HTTP response handler with the underlying
            // ConfigurableMessageHandler. Whether a certain response is retried or not is
            // determined by the BackOffHandler.HandleUnsuccessfulResponseFunc.
            this.backOffInitializer = new ExponentialBackOffInitializer(
                ExponentialBackOffPolicy.Exception |
                ExponentialBackOffPolicy.UnsuccessfulResponse503,
                () => backOffHandler);
        }

        public void Initialize(ConfigurableHttpClient client)
        {
            this.backOffInitializer.Initialize(client);

            // NumTries is the global setting that controls how many times the
            // ConfigurableMessageHandler is willing to retry a request. This defaults to 3 in the
            // Google API client and does not honor the ExponentialBackOff.MaxRetries setting. See
            // https://github.com/googleapis/google-api-dotnet-client/issues/1461.
            client.MessageHandler.NumTries = this.retryOptions.MaxRetries + 1;
        }
    }
}
