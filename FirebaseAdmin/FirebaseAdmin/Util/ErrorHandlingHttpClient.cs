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
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An HTTP client wrapper that handles various exceptions by turning them into the suitable
    /// instances of <see cref="FirebaseException"/>. Specifically, this implementation handles
    /// low-level network exceptions, HTTP error responses (non 2xx responses), and response
    /// deserialization exceptions.
    /// </summary>
    /// <typeparam name="T">Subtype of <see cref="FirebaseException"/> raised by this
    /// HTTP client.</typeparam>
    internal sealed class ErrorHandlingHttpClient<T> : IDisposable
    where T : FirebaseException
    {
        private readonly HttpClient httpClient;
        private readonly IHttpResponseDeserializer deserializer;
        private readonly IHttpErrorResponseHandler<T> errorResponseHandler;
        private readonly IHttpRequestExceptionHandler<T> requestExceptionHandler;
        private readonly IDeserializeExceptionHandler<T> deserializeExceptionHandler;

        internal ErrorHandlingHttpClient(ErrorHandlingHttpClientArgs<T> args)
        {
            this.httpClient = CreateHttpClient(args);
            this.deserializer = args.Deserializer ?? JsonResponseDeserializer.Instance;
            this.errorResponseHandler = args.ErrorResponseHandler.ThrowIfNull(
                nameof(args.ErrorResponseHandler));
            this.requestExceptionHandler = args.RequestExceptionHandler.ThrowIfNull(
                nameof(args.RequestExceptionHandler));
            this.deserializeExceptionHandler = args.DeserializeExceptionHandler.ThrowIfNull(
                nameof(args.DeserializeExceptionHandler));
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        internal async Task<DeserializedResponseInfo<TResult>> SendAndDeserializeAsync<TResult>(
            HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            var info = await this.SendAndReadAsync(request, cancellationToken)
                .ConfigureAwait(false);
            try
            {
                var result = this.deserializer.Deserialize<TResult>(info.Body);
                return new DeserializedResponseInfo<TResult>(info, result);
            }
            catch (Exception e)
            {
                throw this.deserializeExceptionHandler.HandleDeserializeException(e, info);
            }
        }

        internal async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken = default)
        {
            return await this.httpClient.SendAsync(request, cancellationToken)
                .ConfigureAwait(false);
        }

        private static HttpClient CreateHttpClient(ErrorHandlingHttpClientArgs<T> args)
        {
            var clientArgs = new CreateHttpClientArgs();
            var credential = args.Credential;
            if (credential != null)
            {
                clientArgs.Initializers.Add(credential);
            }

            var retry = args.RetryOptions;
            if (retry != null)
            {
                clientArgs.Initializers.Add(new RetryHttpClientInitializer(retry));
            }

            var clientFactory = args.HttpClientFactory.ThrowIfNull(nameof(args.HttpClientFactory));
            return clientFactory.CreateHttpClient(clientArgs);
        }

        private async Task<ResponseInfo> SendAndReadAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await this.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw this.errorResponseHandler.HandleHttpErrorResponse(response, body);
                }

                return new ResponseInfo(response, body);
            }
            catch (HttpRequestException e)
            {
                throw this.requestExceptionHandler.HandleHttpRequestException(e);
            }
        }

        private sealed class JsonResponseDeserializer : IHttpResponseDeserializer
        {
            internal static readonly JsonResponseDeserializer Instance = new JsonResponseDeserializer();

            private JsonResponseDeserializer() { }

            public TResult Deserialize<TResult>(string body)
            {
                return NewtonsoftJsonSerializer.Instance.Deserialize<TResult>(body);
            }
        }
    }
}
