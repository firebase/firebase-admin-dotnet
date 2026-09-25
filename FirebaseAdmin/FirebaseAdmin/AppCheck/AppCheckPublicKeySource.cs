// Copyright 2026, Google Inc. All rights reserved.
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
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Util;
using Google.Apis.Http;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Retrieves App Check public keys from the JWKS endpoint. Keys are cached according to the
    /// HTTP cache-control directive, and refreshed (with rate limiting) when an unknown key ID
    /// is requested.
    /// </summary>
    internal sealed class AppCheckPublicKeySource
    {
        private const string JwksUrl = "https://firebaseappcheck.googleapis.com/v1/jwks";

        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromHours(6);
        private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(30);

        private readonly SemaphoreSlim cacheLock = new SemaphoreSlim(1, 1);
        private readonly IClock clock;
        private readonly HttpClientFactory clientFactory;
        private IReadOnlyDictionary<string, PublicKey> cachedKeys;
        private DateTime expirationTime;
        private DateTime lastFetchTime;

        internal AppCheckPublicKeySource(IClock clock, HttpClientFactory clientFactory)
        {
            this.clock = clock.ThrowIfNull(nameof(clock));
            this.clientFactory = clientFactory.ThrowIfNull(nameof(clientFactory));
        }

        /// <summary>
        /// Returns the public key with the given key ID, or null if no such key exists.
        /// </summary>
        internal async Task<PublicKey> GetPublicKeyAsync(
            string keyId, CancellationToken cancellationToken = default(CancellationToken))
        {
            keyId.ThrowIfNullOrEmpty(nameof(keyId));
            var keys = await this.GetKeysAsync(false, cancellationToken).ConfigureAwait(false);
            if (!keys.TryGetValue(keyId, out var key))
            {
                // The keys may have been rotated since they were cached.
                keys = await this.GetKeysAsync(true, cancellationToken).ConfigureAwait(false);
                keys.TryGetValue(keyId, out key);
            }

            return key;
        }

        private async Task<IReadOnlyDictionary<string, PublicKey>> GetKeysAsync(
            bool forceRefresh, CancellationToken cancellationToken)
        {
            await this.cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var now = this.clock.UtcNow;
                if (this.cachedKeys == null
                    || now >= this.expirationTime
                    || (forceRefresh && now - this.lastFetchTime >= MinRefreshInterval))
                {
                    this.lastFetchTime = now;
                    await this.RefreshAsync(now, cancellationToken).ConfigureAwait(false);
                }

                return this.cachedKeys;
            }
            finally
            {
                this.cacheLock.Release();
            }
        }

        private async Task RefreshAsync(DateTime now, CancellationToken cancellationToken)
        {
            using (var httpClient = this.CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, JwksUrl);
                var response = await httpClient
                    .SendAndDeserializeAsync<JwkSet>(request, cancellationToken)
                    .ConfigureAwait(false);

                this.cachedKeys = this.ParseKeys(response);
                var maxAge = response.HttpResponse.Headers.CacheControl?.MaxAge;
                this.expirationTime = now.Add(maxAge ?? DefaultCacheDuration);
            }
        }

        private IReadOnlyDictionary<string, PublicKey> ParseKeys(
            DeserializedResponseInfo<JwkSet> response)
        {
            var builder = ImmutableDictionary.CreateBuilder<string, PublicKey>();
            foreach (var jwk in response.Result?.Keys ?? new List<JwkSet.Jwk>())
            {
                if (jwk.Kty != "RSA" || jwk.Alg != "RS256" || string.IsNullOrEmpty(jwk.Kid)
                    || string.IsNullOrEmpty(jwk.N) || string.IsNullOrEmpty(jwk.E))
                {
                    continue;
                }

                var rsa = RSA.Create();
                rsa.ImportParameters(new RSAParameters()
                {
                    Modulus = JwtUtils.Base64DecodeToBytes(jwk.N),
                    Exponent = JwtUtils.Base64DecodeToBytes(jwk.E),
                });
                builder[jwk.Kid] = new PublicKey(jwk.Kid, rsa);
            }

            if (builder.Count == 0)
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.Unknown,
                    "No valid public keys present in the JWKS response.",
                    AppCheckErrorCode.ServiceError,
                    response: response.HttpResponse);
            }

            return builder.ToImmutable();
        }

        private ErrorHandlingHttpClient<FirebaseAppCheckException> CreateHttpClient()
        {
            return new ErrorHandlingHttpClient<FirebaseAppCheckException>(
                new ErrorHandlingHttpClientArgs<FirebaseAppCheckException>()
                {
                    HttpClientFactory = this.clientFactory,
                    ErrorResponseHandler = AppCheckErrorHandler.Instance,
                    RequestExceptionHandler = AppCheckErrorHandler.Instance,
                    DeserializeExceptionHandler = AppCheckErrorHandler.Instance,
                });
        }

        private sealed class JwkSet
        {
            [JsonProperty("keys")]
            internal List<Jwk> Keys { get; set; }

            internal sealed class Jwk
            {
                [JsonProperty("kid")]
                internal string Kid { get; set; }

                [JsonProperty("kty")]
                internal string Kty { get; set; }

                [JsonProperty("alg")]
                internal string Alg { get; set; }

                [JsonProperty("n")]
                internal string N { get; set; }

                [JsonProperty("e")]
                internal string E { get; set; }
            }
        }
    }
}
