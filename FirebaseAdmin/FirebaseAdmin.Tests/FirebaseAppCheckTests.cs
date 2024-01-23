using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

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
        public void CreateAppCheck()
        {
            FirebaseAppCheck withoutAppIdCreate = FirebaseAppCheck.Create(this.mockCredentialApp);
            Assert.NotNull(withoutAppIdCreate);
        }

        [Fact]
        public async Task InvalidAppIdCreateToken()
        {
            FirebaseAppCheck invalidAppIdCreate = FirebaseAppCheck.Create(this.mockCredentialApp);

            await Assert.ThrowsAsync<ArgumentException>(() => invalidAppIdCreate.CreateToken(appId: null));
            await Assert.ThrowsAsync<ArgumentException>(() => invalidAppIdCreate.CreateToken(appId: string.Empty));
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
        public async Task CreateTokenFromAppId()
        {
            FirebaseAppCheck createTokenFromAppId = new FirebaseAppCheck(this.mockCredentialApp);
            var token = await createTokenFromAppId.CreateToken(this.appId);
            Assert.IsType<string>(token.Token);
            Assert.NotNull(token.Token);
            Assert.IsType<int>(token.TtlMillis);
            Assert.Equal<int>(3600000, token.TtlMillis);
        }

        [Fact]
        public async Task CreateTokenFromAppIdAndTtlMillis()
        {
            AppCheckTokenOptions options = new (1800000);
            FirebaseAppCheck createTokenFromAppIdAndTtlMillis = new FirebaseAppCheck(this.mockCredentialApp);

            var token = await createTokenFromAppIdAndTtlMillis.CreateToken(this.appId, options);
            Assert.IsType<string>(token.Token);
            Assert.NotNull(token.Token);
            Assert.IsType<int>(token.TtlMillis);
            Assert.Equal<int>(1800000, token.TtlMillis);
        }

        [Fact]
        public async Task VerifyToken()
        {
            FirebaseAppCheck verifyToken = new FirebaseAppCheck(this.mockCredentialApp);

            AppCheckToken validToken = await verifyToken.CreateToken(this.appId);
            AppCheckVerifyResponse verifiedToken = await verifyToken.VerifyToken(validToken.Token, null);
            Assert.Equal("explicit-project", verifiedToken.AppId);
        }

        [Fact]
        public async Task VerifyTokenInvaild()
        {
            FirebaseAppCheck verifyTokenInvaild = new FirebaseAppCheck(this.mockCredentialApp);

            await Assert.ThrowsAsync<ArgumentException>(() => verifyTokenInvaild.VerifyToken(null));
            await Assert.ThrowsAsync<ArgumentException>(() => verifyTokenInvaild.VerifyToken(string.Empty));
        }

        public void Dispose()
        {
            FirebaseAppCheck.DeleteAll();
        }
    }
}
