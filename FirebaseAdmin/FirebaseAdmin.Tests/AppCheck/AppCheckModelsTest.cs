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
using FirebaseAdmin.AppCheck;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckModelsTest
    {
        [Fact]
        public void VerifyAppCheckTokenOptions_DefaultAndCustomValues()
        {
            var defaultOptions = new VerifyAppCheckTokenOptions();
            Assert.False(defaultOptions.Consume);

            var optionsWithConsume = new VerifyAppCheckTokenOptions { Consume = true };
            Assert.True(optionsWithConsume.Consume);
        }

        [Fact]
        public void DecodedAppCheckToken_AllPropertiesMapped()
        {
            var claims = new Dictionary<string, object>
            {
                { "iss", "https://firebaseappcheck.googleapis.com/123456" },
                { "sub", "test-app-id" },
                { "aud", new List<string> { "projects/123456" } },
                { "exp", 1700003600L },
                { "iat", 1700000000L },
                { "jti", "test-jti-uuid" },
                { "provider", "play_integrity" },
            };

            var args = new DecodedAppCheckToken.Args
            {
                Issuer = "https://firebaseappcheck.googleapis.com/123456",
                Subject = "test-app-id",
                Audience = new List<string> { "projects/123456" },
                ExpirationTimeSeconds = 1700003600L,
                IssuedAtTimeSeconds = 1700000000L,
                Jti = "test-jti-uuid",
                Provider = "play_integrity",
                Claims = claims,
            };

            var token = new DecodedAppCheckToken(args);

            Assert.Equal("https://firebaseappcheck.googleapis.com/123456", token.Issuer);
            Assert.Equal("test-app-id", token.Subject);
            Assert.Single(token.Audience);
            Assert.Equal("projects/123456", token.Audience[0]);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700003600L), token.ExpirationTime);
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1700000000L), token.IssuedAtTime);
            Assert.Equal("test-jti-uuid", token.Jti);
            Assert.Equal("play_integrity", token.Provider);
            Assert.Same(claims, token.Claims);
        }

        [Fact]
        public void DecodedAppCheckToken_NullAudienceDefaultsToEmpty()
        {
            var args = new DecodedAppCheckToken.Args
            {
                Issuer = "https://firebaseappcheck.googleapis.com/123456",
                Subject = "test-app-id",
                Audience = null,
                ExpirationTimeSeconds = 1700003600L,
                IssuedAtTimeSeconds = 1700000000L,
                Claims = new Dictionary<string, object>(),
            };

            var token = new DecodedAppCheckToken(args);

            Assert.NotNull(token.Audience);
            Assert.Empty(token.Audience);
        }

        [Fact]
        public void DecodedAppCheckToken_NullClaimsDefaultsToEmpty()
        {
            var args = new DecodedAppCheckToken.Args
            {
                Issuer = "https://firebaseappcheck.googleapis.com/123456",
                Subject = "test-app-id",
                ExpirationTimeSeconds = 1700003600L,
                IssuedAtTimeSeconds = 1700000000L,
                Claims = null,
            };

            var token = new DecodedAppCheckToken(args);

            Assert.NotNull(token.Claims);
            Assert.Empty(token.Claims);
        }

        [Fact]
        public void DecodedAppCheckToken_AudienceJsonDeserialization()
        {
            // String audience
            var jsonStringAud = "{\"aud\":\"single-aud\"}";
            var args1 = JsonConvert.DeserializeObject<DecodedAppCheckToken.Args>(jsonStringAud);
            Assert.Single(args1.Audience);
            Assert.Equal("single-aud", args1.Audience[0]);

            // Array audience
            var jsonArrayAud = "{\"aud\":[\"aud-1\",\"aud-2\"]}";
            var args2 = JsonConvert.DeserializeObject<DecodedAppCheckToken.Args>(jsonArrayAud);
            Assert.Equal(2, args2.Audience.Count);
            Assert.Equal("aud-1", args2.Audience[0]);
            Assert.Equal("aud-2", args2.Audience[1]);

            // Null audience
            var jsonNullAud = "{\"aud\":null}";
            var args3 = JsonConvert.DeserializeObject<DecodedAppCheckToken.Args>(jsonNullAud);
            Assert.Null(args3.Audience);

            // Invalid audience type throws JsonSerializationException
            var jsonInvalidAud = "{\"aud\":12345}";
            Assert.Throws<JsonSerializationException>(
                () => JsonConvert.DeserializeObject<DecodedAppCheckToken.Args>(jsonInvalidAud));
        }

        [Fact]
        public void VerifyAppCheckTokenResponse_ValidArguments()
        {
            var args = new DecodedAppCheckToken.Args
            {
                Issuer = "https://firebaseappcheck.googleapis.com/123456",
                Subject = "test-app-id",
                ExpirationTimeSeconds = 1700003600L,
                IssuedAtTimeSeconds = 1700000000L,
                Claims = new Dictionary<string, object>(),
            };
            var token = new DecodedAppCheckToken(args);

            var respWithoutConsume = new VerifyAppCheckTokenResponse("test-app-id", token);
            Assert.Equal("test-app-id", respWithoutConsume.AppId);
            Assert.Same(token, respWithoutConsume.Token);
            Assert.Null(respWithoutConsume.AlreadyConsumed);

            var respWithConsumedFalse = new VerifyAppCheckTokenResponse("test-app-id", token, false);
            Assert.Equal("test-app-id", respWithConsumedFalse.AppId);
            Assert.Same(token, respWithConsumedFalse.Token);
            Assert.False(respWithConsumedFalse.AlreadyConsumed);

            var respWithConsumedTrue = new VerifyAppCheckTokenResponse("test-app-id", token, true);
            Assert.Equal("test-app-id", respWithConsumedTrue.AppId);
            Assert.Same(token, respWithConsumedTrue.Token);
            Assert.True(respWithConsumedTrue.AlreadyConsumed);
        }

        [Fact]
        public void VerifyAppCheckTokenResponse_InvalidArgumentsThrow()
        {
            var args = new DecodedAppCheckToken.Args
            {
                Claims = new Dictionary<string, object>(),
            };
            var token = new DecodedAppCheckToken(args);

            Assert.Throws<ArgumentException>(() => new VerifyAppCheckTokenResponse(null, token));
            Assert.Throws<ArgumentException>(() => new VerifyAppCheckTokenResponse(string.Empty, token));
            Assert.Throws<ArgumentNullException>(() => new VerifyAppCheckTokenResponse("app-id", null));
        }
    }
}
