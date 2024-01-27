using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Jwt.Tests;
using Google.Apis.Auth;
using Google.Apis.Util;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.AppCheck.Tests
{
    public class AppCheckTokenVerifierTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        public static readonly IEnumerable<object[]> InvalidTokens = new List<object[]>
        {
            new object[] { "TestToken" },
            new object[] { "Test.Token" },
            new object[] { "Test.Token.Test.Token" },
        };

        public static readonly IEnumerable<object[]> InvalidAudiences = new List<object[]>
        {
            new object[] { new List<string> { "incorrectAudience" } },
            new object[] { new List<string> { "12345678", "project_id" } },
            new object[] { new List<string> { "projects/" + "12345678", "project_id" } },
        };

        private readonly string appId = "1:1234:android:1234";

        [Fact]
        public void NullKeySource()
        {
            var args = FullyPopulatedArgs();
            args.KeySource = null;

            Assert.Throws<ArgumentNullException>(() => new AppCheckTokenVerifier(args));
        }

        [Fact]
        public void ProjectId()
        {
            var args = FullyPopulatedArgs();

            var verifier = new AppCheckTokenVerifier(args);

            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task VerifyWithNullEmptyToken(string token)
        {
            var args = FullyPopulatedArgs();
            var tokenVerifier = new AppCheckTokenVerifier(args);

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await tokenVerifier.VerifyTokenAsync(token));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("App Check token must not be null or empty.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task VerifyWithInvalidProjectId(string projectId)
        {
            var args = FullyPopulatedArgs();
            args.ProjectId = projectId;
            var tokenVerifier = new AppCheckTokenVerifier(args);

            string token = "test-token";

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await tokenVerifier.VerifyTokenAsync(token));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("Must initialize app with a cert credential or set your Firebase project ID as the GOOGLE_CLOUD_PROJECT environment variable to verify an App Check token.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidCredential, ex.AppCheckErrorCode);
        }

        [Theory]
        [MemberData(nameof(InvalidTokens))]
        public async Task VerifyWithInvalidToken(string token)
        {
            var args = FullyPopulatedArgs();
            var tokenVerifier = new AppCheckTokenVerifier(args);

            var ex = await Assert.ThrowsAsync<FirebaseAppCheckException>(
                async () => await tokenVerifier.VerifyTokenAsync(token));

            Assert.Equal(ErrorCode.InvalidArgument, ex.ErrorCode);
            Assert.Equal("Incorrect number of segments in app check token.", ex.Message);
            Assert.Equal(AppCheckErrorCode.InvalidArgument, ex.AppCheckErrorCode);
        }

        [Theory]
        [MemberData(nameof(InvalidAudiences))]
        public async Task CheckInvalidAudience(List<string> aud)
        {
            string token = await this.GeneratorAppCheckTokenAsync(aud).ConfigureAwait(false);
            string expected = "The provided app check token has incorrect \"aud\" (audience) claim";
            var args = FullyPopulatedArgs();
            AppCheckTokenVerifier verifier = new AppCheckTokenVerifier(args);
            var result = await Assert.ThrowsAsync<FirebaseAppCheckException>(() => verifier.VerifyTokenAsync(token)).ConfigureAwait(false);
            Assert.Contains(expected, result.Message);
        }

        [Fact]
        public async Task CheckEmptyAudience()
        {
            string token = await this.GeneratorAppCheckTokenAsync([]).ConfigureAwait(false);
            var args = FullyPopulatedArgs();
            AppCheckTokenVerifier verifier = new AppCheckTokenVerifier(args);
            var result = await Assert.ThrowsAsync<FirebaseAppCheckException>(() => verifier.VerifyTokenAsync(token)).ConfigureAwait(false);
            Assert.Equal("Failed to verify app check signature.", result.Message);
        }

        [Fact]
        public async Task VerifyToken()
        {
            List<string> aud = new List<string> { "12345678", "projects/test-project" };
            string token = await this.GeneratorAppCheckTokenAsync(aud).ConfigureAwait(false);

            var args = FullyPopulatedArgs();
            AppCheckTokenVerifier verifier = new AppCheckTokenVerifier(args);
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => verifier.VerifyTokenAsync(token)).ConfigureAwait(false);
        }

        private static AppCheckTokenVerifier.Args FullyPopulatedArgs()
        {
            return new AppCheckTokenVerifier.Args
            {
                ProjectId = "test-project",
                Clock = null,
                KeySource = JwtTestUtils.DefaultKeySource,
            };
        }

        private async Task<string> GeneratorAppCheckTokenAsync(List<string> audience)
        {
            DateTime unixEpoch = new DateTime(
                    1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var header = new JsonWebSignature.Header()
            {
                Algorithm = "RS256",
                KeyId = "FGQdnRlzAmKyKr6-Hg_kMQrBkj_H6i6ADnBQz4OI6BU",
            };

            var signer = EmulatorSigner.Instance;
            CancellationToken cancellationToken = default;
            int issued = (int)(SystemClock.Default.UtcNow - unixEpoch).TotalSeconds;
            var keyId = await signer.GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var payload = new CustomTokenPayload()
            {
                Subject = this.appId,
                Issuer = "https://firebaseappcheck.googleapis.com/" + this.appId,
                AppId = this.appId,
                Audience = audience,
                ExpirationTimeSeconds = 60,
                IssuedAtTimeSeconds = issued,
                Ttl = "180000",
            };

            return await JwtUtils.CreateSignedJwtAsync(
                header, payload, signer).ConfigureAwait(false);
        }
    }

    internal class CustomTokenPayload : JsonWebToken.Payload
    {
        [JsonPropertyAttribute("app_id")]
        public string AppId { get; set; }

        [JsonPropertyAttribute("ttl")]
        public string Ttl { get; set; }
    }
}
