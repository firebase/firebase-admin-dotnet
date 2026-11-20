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
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class FirebaseAppCheckTest : IDisposable
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        private static readonly RSA Key = CreateRsaKey();
        private static readonly RSA OtherKey = CreateRsaKey();

        [Fact]
        public void GetAppCheckWithoutApp()
        {
            Assert.Null(FirebaseAppCheck.DefaultInstance);
        }

        [Fact]
        public void GetDefaultAppCheck()
        {
            var app = FirebaseApp.Create(this.CreateOptions());
            var appCheck = FirebaseAppCheck.DefaultInstance;
            Assert.NotNull(appCheck);
            Assert.Same(appCheck, FirebaseAppCheck.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseAppCheck.DefaultInstance);
        }

        [Fact]
        public void GetAppCheck()
        {
            var app = FirebaseApp.Create(this.CreateOptions(), "MyApp");
            var appCheck = FirebaseAppCheck.GetAppCheck(app);
            Assert.NotNull(appCheck);
            Assert.Same(appCheck, FirebaseAppCheck.GetAppCheck(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseAppCheck.GetAppCheck(app));
        }

        [Fact]
        public void GetAppCheckWithNullApp()
        {
            Assert.Throws<ArgumentNullException>(() => FirebaseAppCheck.GetAppCheck(null));
        }

        [Fact]
        public void NoProjectId()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });

            var exception = Assert.Throws<ArgumentException>(() => FirebaseAppCheck.GetAppCheck(app));

            Assert.StartsWith("Project ID is required to access App Check service.", exception.Message);
        }

        [Fact]
        public async Task UseAfterDelete()
        {
            var app = FirebaseApp.Create(this.CreateOptions());
            var appCheck = FirebaseAppCheck.DefaultInstance;
            app.Delete();

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => appCheck.VerifyTokenAsync("token"));

            Assert.Equal("Cannot invoke after deleting the app.", exception.Message);
        }

        [Fact]
        public async Task VerifyTokenUsesAppOptions()
        {
            var handler = new MockMessageHandler() { Response = Jwks() };
            FirebaseApp.Create(this.CreateOptions(handler));

            // Valid header and claims, wrong signing key: fails only after the keys are fetched.
            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => FirebaseAppCheck.DefaultInstance.VerifyTokenAsync(CreateToken(OtherKey)));

            Assert.Equal(AppCheckErrorCode.InvalidAppCheckToken, exception.AppCheckErrorCode);
            Assert.Equal("Failed to verify App Check token signature.", exception.Message);
            var request = Assert.Single(handler.Requests);
            Assert.Equal("https://firebaseappcheck.googleapis.com/v1/jwks", request.Url.ToString());
        }

        [Fact]
        public async Task VerifyTokenWithConsume()
        {
            var handler = new MockMessageHandler()
            {
                Response = new List<string>() { Jwks(), @"{""alreadyConsumed"": true}" },
            };
            FirebaseApp.Create(this.CreateOptions(handler));
            var options = new VerifyAppCheckTokenOptions() { Consume = true };

            var response = await FirebaseAppCheck.DefaultInstance.VerifyTokenAsync(
                CreateToken(Key), options);

            Assert.Equal("test-app-id", response.AppId);
            Assert.True(response.AlreadyConsumed);
            Assert.Equal(2, handler.Requests.Count);
            Assert.False(handler.Requests[0].Headers.Contains("Authorization"));
            var replayRequest = handler.Requests[1];
            Assert.Equal(
                "https://firebaseappcheck.googleapis.com/v1beta/projects/test-project:verifyAppCheckToken",
                replayRequest.Url.ToString());
            Assert.Equal(
                "Bearer test-token", replayRequest.Headers.GetValues("Authorization").Single());
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private static RSA CreateRsaKey()
        {
            var rsa = RSA.Create();
            rsa.KeySize = 2048;
            return rsa;
        }

        private static string Jwks()
        {
            var parameters = Key.ExportParameters(false);
            var jwk = new Dictionary<string, string>()
            {
                { "kid", "k1" },
                { "kty", "RSA" },
                { "alg", "RS256" },
                { "n", JwtUtils.UrlSafeBase64Encode(parameters.Modulus) },
                { "e", JwtUtils.UrlSafeBase64Encode(parameters.Exponent) },
            };
            return NewtonsoftJsonSerializer.Instance.Serialize(new { keys = new[] { jwk } });
        }

        private static string CreateToken(RSA signingKey)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header = new { alg = "RS256", typ = "JWT", kid = "k1" };
            var payload = new
            {
                iss = "https://firebaseappcheck.googleapis.com/1234",
                sub = "test-app-id",
                aud = new[] { "projects/1234", "projects/test-project" },
                iat = now,
                exp = now + 3600,
            };
            var unsigned = $"{Encode(header)}.{Encode(payload)}";
            var signature = signingKey.SignData(
                Encoding.ASCII.GetBytes(unsigned), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return $"{unsigned}.{JwtUtils.UrlSafeBase64Encode(signature)}";
        }

        private static string Encode(object value)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(value);
            return JwtUtils.UrlSafeBase64Encode(Encoding.UTF8.GetBytes(json));
        }

        private AppOptions CreateOptions(MockMessageHandler handler = null)
        {
            return new AppOptions()
            {
                Credential = MockCredential,
                HttpClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                ProjectId = "test-project",
            };
        }
    }
}
