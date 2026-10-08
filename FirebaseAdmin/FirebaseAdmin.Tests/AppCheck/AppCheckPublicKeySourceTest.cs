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
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Util;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckPublicKeySourceTest
    {
        private static readonly RSA Key1 = CreateRsaKey();
        private static readonly RSA Key2 = CreateRsaKey();

        [Fact]
        public async Task ImportsRsaKey()
        {
            var handler = new MockMessageHandler() { Response = Jwks(Jwk("k1", Key1)) };
            var keySource = new AppCheckPublicKeySource(new MockClock(), new MockHttpClientFactory(handler));

            var key = await keySource.GetPublicKeyAsync("k1");

            Assert.Equal("k1", key.Id);
            var data = Encoding.UTF8.GetBytes("data");
            var signature = Key1.SignData(data, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            Assert.True(key.RSA.VerifyData(
                data, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
            Assert.Equal(
                "https://firebaseappcheck.googleapis.com/v1/jwks",
                handler.Requests[0].Url.ToString());
        }

        [Fact]
        public async Task CachesKeysPerMaxAge()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler()
            {
                Response = Jwks(Jwk("k1", Key1)),
                ApplyHeaders = (headers, _) => headers.CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromHours(1),
                },
            };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));

            await keySource.GetPublicKeyAsync("k1");
            clock.UtcNow = clock.UtcNow.AddMinutes(59);
            await keySource.GetPublicKeyAsync("k1");
            Assert.Equal(1, handler.Calls);

            clock.UtcNow = clock.UtcNow.AddMinutes(1);
            await keySource.GetPublicKeyAsync("k1");
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task CachesKeysForDefaultDurationWithoutMaxAge()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler() { Response = Jwks(Jwk("k1", Key1)) };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));

            await keySource.GetPublicKeyAsync("k1");
            clock.UtcNow = clock.UtcNow.AddHours(6).AddSeconds(-1);
            await keySource.GetPublicKeyAsync("k1");
            Assert.Equal(1, handler.Calls);

            clock.UtcNow = clock.UtcNow.AddSeconds(1);
            await keySource.GetPublicKeyAsync("k1");
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task UnknownKeyIdTriggersRefresh()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler()
            {
                Response = new List<string>()
                {
                    Jwks(Jwk("k1", Key1)),
                    Jwks(Jwk("k1", Key1), Jwk("k2", Key2)),
                },
            };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));
            await keySource.GetPublicKeyAsync("k1");
            clock.UtcNow = clock.UtcNow.AddSeconds(30);

            var key = await keySource.GetPublicKeyAsync("k2");

            Assert.Equal("k2", key.Id);
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task UnknownKeyIdRefreshIsRateLimited()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler() { Response = Jwks(Jwk("k1", Key1)) };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));

            Assert.Null(await keySource.GetPublicKeyAsync("unknown"));
            Assert.Null(await keySource.GetPublicKeyAsync("unknown"));
            clock.UtcNow = clock.UtcNow.AddSeconds(29);
            Assert.Null(await keySource.GetPublicKeyAsync("unknown"));
            Assert.Equal(1, handler.Calls);

            clock.UtcNow = clock.UtcNow.AddSeconds(1);
            Assert.Null(await keySource.GetPublicKeyAsync("unknown"));
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task SkipsUnsupportedKeys()
        {
            var unsupported = Jwk("k2", Key2);
            unsupported["alg"] = "RS512";
            var handler = new MockMessageHandler()
            {
                Response = Jwks(
                    Jwk("k1", Key1),
                    unsupported,
                    new Dictionary<string, string>() { { "kid", "k3" }, { "kty", "EC" }, { "alg", "ES256" } },
                    new Dictionary<string, string>() { { "kty", "RSA" }, { "alg", "RS256" } }),
            };
            var keySource = new AppCheckPublicKeySource(new MockClock(), new MockHttpClientFactory(handler));

            Assert.NotNull(await keySource.GetPublicKeyAsync("k1"));
            Assert.Null(await keySource.GetPublicKeyAsync("k2"));
            Assert.Null(await keySource.GetPublicKeyAsync("k3"));
        }

        [Fact]
        public async Task SkipsMalformedKeys()
        {
            var malformed = Jwk("k2", Key2);
            malformed["n"] = "not*valid*base64";
            var handler = new MockMessageHandler() { Response = Jwks(Jwk("k1", Key1), malformed) };
            var keySource = new AppCheckPublicKeySource(new MockClock(), new MockHttpClientFactory(handler));

            Assert.NotNull(await keySource.GetPublicKeyAsync("k1"));
            Assert.Null(await keySource.GetPublicKeyAsync("k2"));
        }

        [Fact]
        public async Task OnlyMalformedKeys()
        {
            var clock = new MockClock();
            var malformed = Jwk("k1", Key1);
            malformed["e"] = "not*valid*base64";
            var handler = new MockMessageHandler() { Response = Jwks(malformed) };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => keySource.GetPublicKeyAsync("k1"));
            Assert.Equal("No valid public keys present in the JWKS response.", exception.Message);
            Assert.Equal(AppCheckErrorCode.ServiceError, exception.AppCheckErrorCode);

            // The failure is recorded, so retries within the refresh interval are rate limited.
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => keySource.GetPublicKeyAsync("k1"));
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task NoValidKeys()
        {
            var handler = new MockMessageHandler() { Response = @"{""keys"": []}" };
            var keySource = new AppCheckPublicKeySource(new MockClock(), new MockHttpClientFactory(handler));

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => keySource.GetPublicKeyAsync("k1"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal("No valid public keys present in the JWKS response.", exception.Message);
            Assert.Equal(AppCheckErrorCode.ServiceError, exception.AppCheckErrorCode);
            Assert.NotNull(exception.HttpResponse);
        }

        [Fact]
        public async Task HttpError()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "test error",
            };
            var keySource = new AppCheckPublicKeySource(new MockClock(), new MockHttpClientFactory(handler));

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => keySource.GetPublicKeyAsync("k1"));

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal(AppCheckErrorCode.ServiceError, exception.AppCheckErrorCode);
            Assert.NotNull(exception.HttpResponse);
        }

        [Fact]
        public async Task TransientErrorIsRetried()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { "unavailable", Jwks(Jwk("k1", Key1)) },
            };
            handler.GenerateStatusCode = (_) => handler.Requests.Count == 1
                ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK;
            var keySource = new AppCheckPublicKeySource(
                new MockClock(), new MockHttpClientFactory(handler), RetryOptions.NoBackOff);

            var key = await keySource.GetPublicKeyAsync("k1");

            Assert.Equal("k1", key.Id);
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task FetchFailureIsRateLimited()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "test error",
            };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => keySource.GetPublicKeyAsync("k1"));
            clock.UtcNow = clock.UtcNow.AddSeconds(29);

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => keySource.GetPublicKeyAsync("k1"));

            Assert.Equal(1, handler.Calls);
            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal(AppCheckErrorCode.ServiceError, exception.AppCheckErrorCode);
            Assert.StartsWith("Failed to fetch App Check public keys.", exception.Message);
            Assert.IsType<FirebaseAppCheckException>(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);
            Assert.Same(((FirebaseAppCheckException)exception.InnerException).HttpResponse, exception.HttpResponse);

            handler.StatusCode = HttpStatusCode.OK;
            handler.Response = Jwks(Jwk("k1", Key1));
            clock.UtcNow = clock.UtcNow.AddSeconds(1);
            Assert.NotNull(await keySource.GetPublicKeyAsync("k1"));
            Assert.Equal(2, handler.Calls);
        }

        [Fact]
        public async Task ExpiredKeysRefreshFailureIsRateLimited()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler() { Response = Jwks(Jwk("k1", Key1)) };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));
            await keySource.GetPublicKeyAsync("k1");

            handler.StatusCode = HttpStatusCode.InternalServerError;
            clock.UtcNow = clock.UtcNow.AddHours(6);
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => keySource.GetPublicKeyAsync("k1"));
            clock.UtcNow = clock.UtcNow.AddSeconds(29);
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => keySource.GetPublicKeyAsync("k1"));
            Assert.Equal(2, handler.Calls);

            handler.StatusCode = HttpStatusCode.OK;
            clock.UtcNow = clock.UtcNow.AddSeconds(1);
            Assert.NotNull(await keySource.GetPublicKeyAsync("k1"));
            Assert.Equal(3, handler.Calls);
        }

        [Fact]
        public async Task ShortMaxAgeIsNotRateLimited()
        {
            var clock = new MockClock();
            var handler = new MockMessageHandler()
            {
                Response = Jwks(Jwk("k1", Key1)),
                ApplyHeaders = (headers, _) => headers.CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(10),
                },
            };
            var keySource = new AppCheckPublicKeySource(clock, new MockHttpClientFactory(handler));

            await keySource.GetPublicKeyAsync("k1");
            clock.UtcNow = clock.UtcNow.AddSeconds(10);
            await keySource.GetPublicKeyAsync("k1");

            Assert.Equal(2, handler.Calls);
        }

        private static RSA CreateRsaKey()
        {
            var rsa = RSA.Create();
            rsa.KeySize = 2048;
            return rsa;
        }

        private static Dictionary<string, string> Jwk(string kid, RSA rsa)
        {
            var parameters = rsa.ExportParameters(false);
            return new Dictionary<string, string>()
            {
                { "kid", kid },
                { "kty", "RSA" },
                { "alg", "RS256" },
                { "use", "sig" },
                { "n", JwtUtils.UrlSafeBase64Encode(parameters.Modulus) },
                { "e", JwtUtils.UrlSafeBase64Encode(parameters.Exponent) },
            };
        }

        private static string Jwks(params Dictionary<string, string>[] keys)
        {
            return NewtonsoftJsonSerializer.Instance.Serialize(new { keys });
        }
    }
}
