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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckTokenVerifierTest
    {
        private const string ProjectId = "test-project";
        private const string AppId = "1:1234:android:abcd";

        private static readonly RSA Key1 = CreateRsaKey();
        private static readonly RSA Key2 = CreateRsaKey();

        private readonly MockClock clock = new MockClock();
        private readonly MockMessageHandler keyHandler = new MockMessageHandler()
        {
            Response = Jwks(Jwk("k1", Key1)),
        };

        private readonly MockMessageHandler replayHandler = new MockMessageHandler()
        {
            Response = @"{""alreadyConsumed"": false}",
        };

        public static IEnumerable<object[]> MalformedTokens => new List<object[]>()
        {
            new object[] { "token" },
            new object[] { "a.b" },
            new object[] { "a.b.c.d" },
            new object[] { "a.b.c" },
            new object[] { $"{Encode("null")}.{Encode("null")}.sig" },
            new object[] { $"{Encode("not json")}.{Encode("{}")}.sig" },
        };

        private long Now => new DateTimeOffset(this.clock.UtcNow).ToUnixTimeSeconds();

        [Fact]
        public async Task ValidToken()
        {
            var token = this.CreateToken();

            var response = await this.CreateVerifier().VerifyTokenAsync(token);

            Assert.Equal(AppId, response.AppId);
            Assert.Null(response.AlreadyConsumed);
            var decoded = response.Token;
            Assert.Equal("https://firebaseappcheck.googleapis.com/1234", decoded.Issuer);
            Assert.Equal(AppId, decoded.Subject);
            Assert.Equal(new[] { "projects/1234", "projects/test-project" }, decoded.Audience);
            Assert.Equal(this.Now, decoded.IssuedAtTime.ToUnixTimeSeconds());
            Assert.Equal(this.Now + 3600, decoded.ExpirationTime.ToUnixTimeSeconds());
            Assert.Equal("test-jti", decoded.Jti);
            Assert.Equal("debug", decoded.Provider);
            Assert.Equal(
                new[] { "aud", "exp", "iat", "iss", "jti", "provider", "sub" },
                new SortedSet<string>(decoded.Claims.Keys));
            Assert.Equal(AppId, decoded.Claims["sub"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task NoConsume(bool withOptions)
        {
            var options = withOptions ? new VerifyAppCheckTokenOptions() { Consume = false } : null;

            var response = await this.CreateVerifier().VerifyTokenAsync(this.CreateToken(), options);

            Assert.Null(response.AlreadyConsumed);
            Assert.Equal(0, this.replayHandler.Calls);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Consume(bool alreadyConsumed)
        {
            this.replayHandler.Response = $@"{{""alreadyConsumed"": {alreadyConsumed.ToString().ToLower()}}}";
            var token = this.CreateToken();
            var options = new VerifyAppCheckTokenOptions() { Consume = true };

            var response = await this.CreateVerifier().VerifyTokenAsync(token, options);

            Assert.Equal(alreadyConsumed, response.AlreadyConsumed);
            var request = Assert.Single(this.replayHandler.Requests);
            Assert.Equal(
                "https://firebaseappcheck.googleapis.com/v1beta/projects/test-project:verifyAppCheckToken",
                request.Url.ToString());
            var body = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, string>>(
                request.Body);
            Assert.Equal(token, body["app_check_token"]);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public async Task NullOrEmptyToken(string token)
        {
            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => this.CreateVerifier().VerifyTokenAsync(token));

            Assert.Equal("App Check token must not be null or empty.", exception.Message);
        }

        [Theory]
        [MemberData(nameof(MalformedTokens))]
        public async Task MalformedToken(string token)
        {
            await this.AssertInvalidToken(token);
        }

        [Theory]
        [InlineData("alg", "HS256")]
        [InlineData("alg", null)]
        [InlineData("kid", "")]
        [InlineData("kid", null)]
        [InlineData("typ", "JOSE")]
        [InlineData("typ", null)]
        public async Task InvalidHeader(string name, string value)
        {
            var token = this.CreateToken(header: new Dictionary<string, object>() { { name, value } });

            await this.AssertInvalidToken(token);
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Theory]
        [InlineData("iss", "https://example.com/1234")]
        [InlineData("iss", null)]
        [InlineData("aud", "projects/other-project")]
        [InlineData("aud", null)]
        [InlineData("sub", "")]
        [InlineData("sub", null)]
        public async Task InvalidClaim(string name, string value)
        {
            var token = this.CreateToken(payload: new Dictionary<string, object>() { { name, value } });

            await this.AssertInvalidToken(token);
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Theory]
        [InlineData("iat", -1L)]
        [InlineData("iat", 253402300800L)]
        [InlineData("iat", long.MinValue)]
        [InlineData("exp", -1L)]
        [InlineData("exp", 253402300800L)]
        [InlineData("exp", long.MaxValue)]
        public async Task InvalidTimestamp(string name, long value)
        {
            var token = this.CreateToken(payload: new Dictionary<string, object>() { { name, value } });

            var exception = await this.AssertInvalidToken(token);
            Assert.Contains($"({name}) claim: {value}.", exception.Message);
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Theory]
        [InlineData("iat", "issued-at")]
        [InlineData("exp", "expiration")]
        public async Task MissingTimestamp(string name, string description)
        {
            var token = this.CreateToken(payload: new Dictionary<string, object>() { { name, null } });

            var exception = await this.AssertInvalidToken(token);
            Assert.Equal($"App Check token has no {description} ({name}) claim.", exception.Message);
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Fact]
        public async Task IssuedInFuture()
        {
            var verifier = this.CreateVerifier();

            await verifier.VerifyTokenAsync(this.CreateToken(
                payload: new Dictionary<string, object>() { { "iat", this.Now + 60 } }));
            await this.AssertInvalidToken(this.CreateToken(
                payload: new Dictionary<string, object>() { { "iat", this.Now + 61 } }));
        }

        [Fact]
        public async Task ExpiredToken()
        {
            var verifier = this.CreateVerifier();
            var token = this.CreateToken(
                payload: new Dictionary<string, object>() { { "exp", this.Now - 61 } });

            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => verifier.VerifyTokenAsync(token));

            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AppCheckErrorCode.AppCheckTokenExpired, exception.AppCheckErrorCode);
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Fact]
        public async Task ExpiredWithinClockSkew()
        {
            var token = this.CreateToken(
                payload: new Dictionary<string, object>() { { "exp", this.Now - 60 } });

            var response = await this.CreateVerifier().VerifyTokenAsync(token);

            Assert.Equal(AppId, response.AppId);
        }

        [Fact]
        public async Task TypeHeaderIsCaseInsensitive()
        {
            var token = this.CreateToken(header: new Dictionary<string, object>() { { "typ", "jwt" } });

            var response = await this.CreateVerifier().VerifyTokenAsync(token);

            Assert.Equal(AppId, response.AppId);
        }

        [Fact]
        public async Task Cancelled()
        {
            var canceller = new CancellationTokenSource();
            canceller.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => this.CreateVerifier().VerifyTokenAsync(this.CreateToken(), null, canceller.Token));
            Assert.Equal(0, this.keyHandler.Calls);
        }

        [Fact]
        public async Task UnknownKeyId()
        {
            var token = this.CreateToken(header: new Dictionary<string, object>() { { "kid", "k2" } });

            var exception = await this.AssertInvalidToken(token);

            Assert.Equal("Failed to verify App Check token signature.", exception.Message);
        }

        [Fact]
        public async Task InvalidSignature()
        {
            var token = this.CreateToken(signingKey: Key2);

            var exception = await this.AssertInvalidToken(token);

            Assert.Equal("Failed to verify App Check token signature.", exception.Message);
            Assert.Equal(0, this.replayHandler.Calls);
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

        private static string Encode(string value)
        {
            return JwtUtils.UrlSafeBase64Encode(Encoding.UTF8.GetBytes(value));
        }

        private static string Encode(Dictionary<string, object> segment, Dictionary<string, object> overrides)
        {
            foreach (var entry in overrides ?? new Dictionary<string, object>())
            {
                if (entry.Value == null)
                {
                    segment.Remove(entry.Key);
                }
                else
                {
                    segment[entry.Key] = entry.Value;
                }
            }

            return Encode(NewtonsoftJsonSerializer.Instance.Serialize(segment));
        }

        private AppCheckTokenVerifier CreateVerifier()
        {
            var keySource = new AppCheckPublicKeySource(
                this.clock, new MockHttpClientFactory(this.keyHandler));
            var client = new AppCheckClient(
                new MockHttpClientFactory(this.replayHandler),
                GoogleCredential.FromAccessToken("test-access-token"),
                ProjectId);
            return new AppCheckTokenVerifier(ProjectId, keySource, client, this.clock);
        }

        private string CreateToken(
            Dictionary<string, object> header = null,
            Dictionary<string, object> payload = null,
            RSA signingKey = null)
        {
            var encodedHeader = Encode(
                new Dictionary<string, object>()
                {
                    { "alg", "RS256" },
                    { "typ", "JWT" },
                    { "kid", "k1" },
                },
                header);
            var encodedPayload = Encode(
                new Dictionary<string, object>()
                {
                    { "iss", "https://firebaseappcheck.googleapis.com/1234" },
                    { "sub", AppId },
                    { "aud", new[] { "projects/1234", "projects/test-project" } },
                    { "iat", this.Now },
                    { "exp", this.Now + 3600 },
                    { "jti", "test-jti" },
                    { "provider", "debug" },
                },
                payload);
            var signature = (signingKey ?? Key1).SignData(
                Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}"),
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);
            return $"{encodedHeader}.{encodedPayload}.{JwtUtils.UrlSafeBase64Encode(signature)}";
        }

        private async Task<FirebaseAppCheckException> AssertInvalidToken(string token)
        {
            var exception = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                () => this.CreateVerifier().VerifyTokenAsync(token));

            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AppCheckErrorCode.InvalidAppCheckToken, exception.AppCheckErrorCode);
            return exception;
        }
    }
}
