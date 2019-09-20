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
    /// An HTTP client initializer that configures clients to retry failing HTTP requests on
    /// low-level exceptions and unsuccessful HTTP responses. Retry conditions and other parameters
    /// are configured via <see cref="RetryOptions"/>. Supports exponential back-off and the
    /// "Retry-After" header in error responses.
    /// </summary>
    internal sealed class RetryHttpClientInitializer : IConfigurableHttpClientInitializer
    {
        private readonly RetryOptions retryOptions;

        internal RetryHttpClientInitializer(RetryOptions retryOptions)
        {
            this.retryOptions = retryOptions.ThrowIfNull(nameof(retryOptions));
        }

        public void Initialize(ConfigurableHttpClient client)
        {
            var backOffHandler = new FirebaseBackOffHandler(this.retryOptions);
            client.MessageHandler.AddExceptionHandler(backOffHandler);
            client.MessageHandler.AddUnsuccessfulResponseHandler(backOffHandler);

            // NumTries is the global setting that controls how many times the
            // ConfigurableMessageHandler is willing to retry a request. This defaults to 3 in the
            // Google API client and does not honor the ExponentialBackOff.MaxRetries setting. See
            // https://github.com/googleapis/google-api-dotnet-client/issues/1461.
            client.MessageHandler.NumTries = this.retryOptions.MaxRetries + 1;
        }
    }
}
