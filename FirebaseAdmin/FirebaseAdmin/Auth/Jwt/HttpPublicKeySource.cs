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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Apis.Http;
using Google.Apis.Util;
using RSAKey = System.Security.Cryptography.RSA;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// An <see cref="IPublicKeySource"/> implementation that retrieves public keys from a remote
    /// HTTP server. Retrieved keys are cached in memory according to the HTTP cache-control
    /// directive.
    /// </summary>
    internal sealed class HttpPublicKeySource : IPublicKeySource
    {
        // Default clock skew used by most GCP libraries. This interval is subtracted from the
        // cache expiry time, before any expiration checks. This helps correct for minor
        // discrepancies between clocks on different machines. It also ensures that the cache is
        // pre-emptively refreshed instead of waiting until the last second.
        private static readonly TimeSpan ClockSkew = new TimeSpan(hours: 0, minutes: 5, seconds: 0);

        private readonly SemaphoreSlim cacheLock = new SemaphoreSlim(1, 1);
        private readonly string certUrl;
        private readonly IClock clock;
        private readonly HttpClientFactory clientFactory;
        private DateTime expirationTime;
        private IReadOnlyList<PublicKey> cachedKeys;

        public HttpPublicKeySource(string certUrl, IClock clock, HttpClientFactory clientFactory)
        {
            this.certUrl = certUrl.ThrowIfNullOrEmpty(nameof(certUrl));
            this.clock = clock.ThrowIfNull(nameof(clock));
            this.clientFactory = clientFactory.ThrowIfNull(nameof(clientFactory));
            this.expirationTime = clock.UtcNow;
        }

        public async Task<IReadOnlyList<PublicKey>> GetPublicKeysAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (this.cachedKeys == null || this.clock.UtcNow >= this.expirationTime)
            {
                await this.cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    var now = this.clock.UtcNow;
                    if (this.cachedKeys == null || now >= this.expirationTime)
                    {
                        using (var httpClient = this.CreateHttpClient())
                        {
                            var request = new HttpRequestMessage()
                            {
                                Method = HttpMethod.Get,
                                RequestUri = new Uri(this.certUrl),
                            };
                            var response = await httpClient
                                .SendAndDeserializeAsync<Dictionary<string, string>>(request, cancellationToken)
                                .ConfigureAwait(false);

                            this.cachedKeys = this.ParseKeys(response);
                            var cacheControl = response.HttpResponse.Headers.CacheControl;
                            if (cacheControl?.MaxAge != null)
                            {
                                this.expirationTime = now.Add(cacheControl.MaxAge.Value)
                                    .Subtract(ClockSkew);
                            }
                        }
                    }
                }
                finally
                {
                    this.cacheLock.Release();
                }
            }

            return this.cachedKeys;
        }

        private ErrorHandlingHttpClient<FirebaseAuthException> CreateHttpClient()
        {
            return new ErrorHandlingHttpClient<FirebaseAuthException>(
                new ErrorHandlingHttpClientArgs<FirebaseAuthException>()
                {
                    HttpClientFactory = this.clientFactory,
                    ErrorResponseHandler = HttpKeySourceErrorHandler.Instance,
                    RequestExceptionHandler = HttpKeySourceErrorHandler.Instance,
                    DeserializeExceptionHandler = HttpKeySourceErrorHandler.Instance,
                });
        }

        private IReadOnlyList<PublicKey> ParseKeys(DeserializedResponseInfo<Dictionary<string, string>> response)
        {
            if (response.Result.Count == 0)
            {
                throw new FirebaseAuthException(
                    ErrorCode.Unknown,
                    "No public keys present in the response.",
                    AuthErrorCode.CertificateFetchFailed,
                    response: response.HttpResponse);
            }

            var builder = ImmutableList.CreateBuilder<PublicKey>();
            foreach (var entry in response.Result)
            {
                var x509cert = new X509Certificate2(Encoding.UTF8.GetBytes(entry.Value));
                RSAKey rsa;
                rsa = x509cert.GetRSAPublicKey();
                builder.Add(new PublicKey(entry.Key, rsa));
            }

            return builder.ToImmutableList();
        }

        private class HttpKeySourceErrorHandler
        : HttpErrorHandler<FirebaseAuthException>,
            IHttpRequestExceptionHandler<FirebaseAuthException>,
            IDeserializeExceptionHandler<FirebaseAuthException>
        {
            internal static readonly HttpKeySourceErrorHandler Instance = new HttpKeySourceErrorHandler();

            private HttpKeySourceErrorHandler() { }

            public FirebaseAuthException HandleHttpRequestException(HttpRequestException exception)
            {
                var temp = exception.ToFirebaseException();
                return new FirebaseAuthException(
                    temp.ErrorCode,
                    $"Failed to retrieve latest public keys. {temp.Message}",
                    AuthErrorCode.CertificateFetchFailed,
                    inner: temp.InnerException,
                    response: temp.HttpResponse);
            }

            public FirebaseAuthException HandleDeserializeException(
                Exception exception, ResponseInfo responseInfo)
            {
                return new FirebaseAuthException(
                    ErrorCode.Unknown,
                    $"Failed to parse certificate response: {responseInfo.Body}.",
                    AuthErrorCode.CertificateFetchFailed,
                    inner: exception,
                    response: responseInfo.HttpResponse);
            }

            protected override FirebaseAuthException CreateException(FirebaseExceptionArgs args)
            {
                return new FirebaseAuthException(
                    args.Code,
                    args.Message,
                    AuthErrorCode.CertificateFetchFailed,
                    response: args.HttpResponse);
            }
        }
    }
}
