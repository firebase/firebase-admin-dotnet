using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Tests
{
    public class FirebaseAppCheckTests : IDisposable
    {
        [Fact]
        public async Task CreateTokenFromAppId()
        {
            string filePath = @"C:\path\to\your\file.txt";
            string fileContent = File.ReadAllText(filePath);
            string[] appIds = fileContent.Split(',');
            foreach (string appId in appIds)
            {
                var token = await FirebaseAppCheck.CreateToken(appId);
                Assert.IsType<string>(token.Token);
                Assert.NotNull(token.Token);
                Assert.IsType<int>(token.TtlMillis);
                Assert.Equal<int>(3600000, token.TtlMillis);
            }
        }

        [Fact]
        public async Task CreateTokenFromAppIdAndTtlMillis()
        {
            string filePath = @"C:\path\to\your\file.txt";
            string fileContent = File.ReadAllText(filePath);
            string[] appIds = fileContent.Split(',');
            foreach (string appId in appIds)
            {
                AppCheckTokenOptions options = new (1800000);
                var token = await FirebaseAppCheck.CreateToken(appId, options);
                Assert.IsType<string>(token.Token);
                Assert.NotNull(token.Token);
                Assert.IsType<int>(token.TtlMillis);
                Assert.Equal<int>(1800000, token.TtlMillis);
            }
        }

        [Fact]
        public async Task InvalidAppIdCreate()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => FirebaseAppCheck.CreateToken(appId: null));
            await Assert.ThrowsAsync<ArgumentException>(() => FirebaseAppCheck.CreateToken(appId: string.Empty));
        }

        [Fact]
        public async Task DecodeVerifyToken()
        {
            string appId = "1234"; // '../resources/appid.txt'
            AppCheckToken validToken = await FirebaseAppCheck.CreateToken(appId);
            var verifiedToken = FirebaseAppCheck.Decode_and_verify(validToken.Token);
            /* Assert.Equal("explicit-project", verifiedToken);*/
        }

        [Fact]
        public async Task DecodeVerifyTokenInvaild()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => FirebaseAppCheck.Decode_and_verify(token: null));
            await Assert.ThrowsAsync<ArgumentException>(() => FirebaseAppCheck.Decode_and_verify(token: string.Empty));
        }

        public void Dispose()
        {
            FirebaseAppCheck.Delete();
        }
    }
}
