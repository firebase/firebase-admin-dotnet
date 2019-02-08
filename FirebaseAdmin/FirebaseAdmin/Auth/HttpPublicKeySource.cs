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
using System.IO;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

#if NETSTANDARD1_5 || NETSTANDARD2_0
using RSAKey = System.Security.Cryptography.RSA;
#elif NET45
using RSAKey = System.Security.Cryptography.RSACryptoServiceProvider;
#else
#error Unsupported target
#endif

namespace FirebaseAdmin.Auth
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
                        using (var httpClient = this.clientFactory.CreateDefaultHttpClient())
                        {
                            var response = await httpClient.GetAsync(this.certUrl, cancellationToken)
                                .ConfigureAwait(false);
                            response.EnsureSuccessStatusCode();
                            this.cachedKeys = this.ParseKeys(await response.Content.ReadAsStringAsync()
                                .ConfigureAwait(false));
                            var cacheControl = response.Headers.CacheControl;
                            if (cacheControl?.MaxAge != null)
                            {
                                this.expirationTime = now.Add(cacheControl.MaxAge.Value)
                                    .Subtract(ClockSkew);
                            }
                        }
                    }
                }
                catch (HttpRequestException e)
                {
                    throw new FirebaseException("Failed to retrieve latest public keys.", e);
                }
                finally
                {
                    this.cacheLock.Release();
                }
            }

            return this.cachedKeys;
        }

        private IReadOnlyList<PublicKey> ParseKeys(string json)
        {
            var rawKeys = NewtonsoftJsonSerializer.Instance
                .Deserialize<Dictionary<string, string>>(json);
            if (rawKeys.Count == 0)
            {
                throw new InvalidDataException("No public keys present in the response.");
            }

            var builder = ImmutableList.CreateBuilder<PublicKey>();
            foreach (var entry in rawKeys)
            {
                var x509cert = new X509Certificate2(Encoding.UTF8.GetBytes(entry.Value));
                RSAKey rsa;
#if NETSTANDARD1_5 || NETSTANDARD2_0
                rsa = x509cert.GetRSAPublicKey();
#elif NET45
                rsa = (RSAKey) x509cert.PublicKey.Key;
#else
#error Unsupported target
#endif
                builder.Add(new PublicKey(entry.Key, rsa));
            }

            return builder.ToImmutableList();
        }
    }
}
