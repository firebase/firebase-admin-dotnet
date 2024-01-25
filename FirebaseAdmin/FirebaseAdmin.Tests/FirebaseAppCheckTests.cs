using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, " +
    "PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93" +
    "bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113b" +
    "e11e6a7d3113e92484cf7045cc7")]

namespace FirebaseAdmin.Tests
{
    public class FirebaseAppCheckTests : IDisposable
    {
        private readonly string appId = "1:1234:android:1234";
        private FirebaseApp mockCredentialApp;

        public FirebaseAppCheckTests()
        {
            var credential = GoogleCredential.FromFile("./resources/service_account.json");
            var options = new AppOptions()
            {
                Credential = credential,
            };
            this.mockCredentialApp = FirebaseApp.Create(options);
        }

        [Fact]
        public void CreateInvalidApp()
        {
            Assert.Throws<ArgumentNullException>(() => FirebaseAppCheck.Create(null));
        }

        [Fact]
        public void CreateAppCheck()
        {
            FirebaseAppCheck withoutAppIdCreate = FirebaseAppCheck.Create(this.mockCredentialApp);
            Assert.NotNull(withoutAppIdCreate);
        }

        [Fact]
        public void WithoutProjectIDCreate()
        {
            // Project ID not set in the environment.
            Environment.SetEnvironmentVariable("GOOGLE_CLOUD_PROJECT", null);
            Environment.SetEnvironmentVariable("GCLOUD_PROJECT", null);

            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
            };
            var app = FirebaseApp.Create(options, "1234");

            Assert.Throws<ArgumentException>(() => FirebaseAppCheck.Create(app));
        }

        [Fact]
        public void FailedSignCreateToken()
        {
            string expected = "sign error";
            var createTokenMock = new Mock<ISigner>();

            // Setup the mock to throw an exception when SignDataAsync is called
            createTokenMock.Setup(service => service.SignDataAsync(It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
                           .Throws(new ArgumentException(expected));

            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
            };
            var app = FirebaseApp.Create(options, "4321");

            Assert.Throws<ArgumentException>(() => FirebaseAppCheck.Create(app));
        }

        [Fact]
        public async Task CreateTokenApiError()
        {
            var createTokenMock = new Mock<IAppCheckApiClient>();

            createTokenMock.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new ArgumentException("INTERAL_ERROR"));

            FirebaseAppCheck createTokenFromAppId = new FirebaseAppCheck(this.mockCredentialApp);

            await Assert.ThrowsAsync<HttpRequestException>(() => createTokenFromAppId.CreateToken(this.appId));
        }

        [Fact]
        public async Task CreateTokenApiErrorOptions()
        {
            var createTokenMock = new Mock<IAppCheckApiClient>();

            createTokenMock.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>())).Throws(new ArgumentException("INTERAL_ERROR"));

            AppCheckTokenOptions options = new (1800000);
            FirebaseAppCheck createTokenFromAppIdAndTtlMillis = new FirebaseAppCheck(this.mockCredentialApp);

            await Assert.ThrowsAsync<HttpRequestException>(() => createTokenFromAppIdAndTtlMillis.CreateToken(this.appId));
        }

        [Fact]
        public async Task CreateTokenAppCheckTokenSuccess()
        {
            string createdCustomToken = "custom-token";

            AppCheckTokenGenerator tokenFactory = AppCheckTokenGenerator.Create(this.mockCredentialApp);

            var createCustomTokenMock = new Mock<IAppCheckTokenGenerator>();

            createCustomTokenMock.Setup(service => service.CreateCustomTokenAsync(It.IsAny<string>(), It.IsAny<AppCheckTokenOptions>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdCustomToken);

            var customRes = await createCustomTokenMock.Object.CreateCustomTokenAsync(this.appId).ConfigureAwait(false);
            Assert.Equal(createdCustomToken, customRes);

            AppCheckToken expected = new ("token", 3000);
            var createExchangeTokenMock = new Mock<IAppCheckApiClient>();
            createExchangeTokenMock.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expected);

            AppCheckTokenOptions options = new (3000);

            AppCheckToken res = await createExchangeTokenMock.Object.ExchangeTokenAsync("custom-token", this.appId).ConfigureAwait(false);
            Assert.Equal("token", res.Token);
            Assert.Equal(3000, res.TtlMillis);
        }

        [Fact]
        public async Task VerifyTokenApiError()
        {
            var createTokenMock = new Mock<IAppCheckApiClient>();
            createTokenMock.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException("INTERAL_ERROR"));

            FirebaseAppCheck verifyToken = new FirebaseAppCheck(this.mockCredentialApp);

            await Assert.ThrowsAsync<ArgumentException>(() => createTokenMock.Object.VerifyReplayProtection("token"));
        }

        [Fact]
        public async Task VerifyTokenSuccess()
        {
            // Create an instance of FirebaseToken.Args and set its properties.
            var args = new FirebaseToken.Args
            {
                AppId = "1234",
                Issuer = "issuer",
                Subject = "subject",
                Audience = "audience",
                ExpirationTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600, // 1 hour from now
                IssuedAtTimeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
            FirebaseToken mockFirebaseToken = new FirebaseToken(args);

            var verifyTokenMock = new Mock<IAppCheckTokenVerify>();
            verifyTokenMock.Setup(service => service.VerifyTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockFirebaseToken);

            var verifyRes = await verifyTokenMock.Object.VerifyTokenAsync(this.appId).ConfigureAwait(false);

            Assert.Equal(verifyRes.AppId, mockFirebaseToken.AppId);
            Assert.Equal(verifyRes.Issuer, mockFirebaseToken.Issuer);
            Assert.Equal(verifyRes.Subject, mockFirebaseToken.Subject);
            Assert.Equal(verifyRes.Audience, mockFirebaseToken.Audience);
            Assert.Equal(verifyRes.ExpirationTimeSeconds, mockFirebaseToken.ExpirationTimeSeconds);
            Assert.Equal(verifyRes.IssuedAtTimeSeconds, mockFirebaseToken.IssuedAtTimeSeconds);
        }

        [Fact]
        public async Task VerifyTokenInvaild()
        {
            FirebaseAppCheck verifyTokenInvaild = new FirebaseAppCheck(this.mockCredentialApp);

            await Assert.ThrowsAsync<ArgumentNullException>(() => verifyTokenInvaild.VerifyToken(null));
            await Assert.ThrowsAsync<ArgumentNullException>(() => verifyTokenInvaild.VerifyToken(string.Empty));
        }

        public void Dispose()
        {
            FirebaseAppCheck.DeleteAll();
        }
    }
}
