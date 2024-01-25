using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Moq;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckTokenGeneratorTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private const int ThirtyMinInMs = 1800000;
        private const int SevenDaysInMs = 604800000;
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        private readonly string appId = "test-app-id";

        [Fact]
        public void ProjectIdFromOptions()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                ProjectId = "explicit-project-id1",
            });
            var verifier = AppCheckTokenVerify.Create(app);
            Assert.Equal("explicit-project-id1", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromServiceAccount()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            });
            var verifier = AppCheckTokenVerify.Create(app);
            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Fact]
        public async Task InvalidAppId()
        {
            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
            };
            var app = FirebaseApp.Create(options, "123");

            AppCheckTokenGenerator tokenGenerator = AppCheckTokenGenerator.Create(app);
            await Assert.ThrowsAsync<ArgumentException>(() => tokenGenerator.CreateCustomTokenAsync(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => tokenGenerator.CreateCustomTokenAsync(null));
        }

        [Fact]
        public async Task InvalidOptions()
        {
            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
            };
            var app = FirebaseApp.Create(options, "1234");
            var tokenGernerator = AppCheckTokenGenerator.Create(app);
            int[] ttls = new int[] { -100, -1, 0, 10, 1799999, 604800001, 1209600000 };
            foreach (var ttl in ttls)
            {
                var option = new AppCheckTokenOptions(ttl);

                var result = await Assert.ThrowsAsync<ArgumentException>(() =>
                    tokenGernerator.CreateCustomTokenAsync(this.appId, option));
            }
        }

        [Fact]
        public void ValidOptions()
        {
            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
            };
            var app = FirebaseApp.Create(options, "12356");
            var tokenGernerator = AppCheckTokenGenerator.Create(app);
            int[] ttls = new int[] { ThirtyMinInMs, ThirtyMinInMs + 1, SevenDaysInMs / 2, SevenDaysInMs - 1, SevenDaysInMs };
            foreach (var ttl in ttls)
            {
                var option = new AppCheckTokenOptions(ttl);

                var result = tokenGernerator.CreateCustomTokenAsync(this.appId, option);
                Assert.NotNull(result);
            }
        }

        [Fact]
        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}
