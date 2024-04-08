using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Tests.AppCheck;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Moq;
using Xunit;

namespace FirebaseAdmin.AppCheck.Tests
{
    public class FirebaseAppCheckTests : IDisposable
    {
        private static readonly GoogleCredential MockCredential = GoogleCredential.FromFile("./resources/service_account.json");
        private static readonly string ProjectId = "test-project";
        private static readonly string AppId = "1:1234:android:1234";

        private string noProjectId = "Project ID is required to access app check service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.";

        [Fact]
        public void GetAppCheckWithoutApp()
        {
            Assert.Null(FirebaseAppCheck.DefaultInstance);
        }

        [Fact]
        public void GetAppCheckWithoutProjectId()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            var res = Assert.Throws<FirebaseAppCheckException>(() => new FirebaseAppCheck(app));
            Assert.Equal(this.noProjectId, res.Message);
            app.Delete();
        }

        [Fact]
        public void GetDefaultAppCheck()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential, ProjectId = ProjectId });
            FirebaseAppCheck appCheck = FirebaseAppCheck.DefaultInstance;
            Assert.NotNull(appCheck);
            Assert.Same(appCheck, FirebaseAppCheck.DefaultInstance);
            app.Delete();
            Assert.Null(FirebaseAppCheck.DefaultInstance);
        }

        [Fact]
        public void GetAppCheck()
        {
            var app = FirebaseApp.Create(new AppOptions { Credential = MockCredential, ProjectId = ProjectId }, "MyApp");
            FirebaseAppCheck appCheck = FirebaseAppCheck.GetAppCheck(app);
            Assert.NotNull(appCheck);
            Assert.Same(appCheck, FirebaseAppCheck.GetAppCheck(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => FirebaseAppCheck.GetAppCheck(app));
        }

        [Fact]
        public async Task GetAppCheckWithApiClientFactory()
        {
            var bytes = Encoding.UTF8.GetBytes("signature");
            var handler = new MockMessageHandler()
            {
                Response = new CreatTokenResponse()
                {
                    Signature = Convert.ToBase64String(bytes),
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };
            var factory = new MockHttpClientFactory(handler);

            var app = FirebaseApp.Create(
                new AppOptions()
                {
                    Credential = GoogleCredential.FromAccessToken("test-token"),
                    HttpClientFactory = factory,
                    ProjectId = ProjectId,
                },
                AppId);

            FirebaseAppCheck appCheck = FirebaseAppCheck.GetAppCheck(app);
            Assert.NotNull(appCheck);
            Assert.Same(appCheck, FirebaseAppCheck.GetAppCheck(app));

            var response = await appCheck.CreateTokenAsync(app.Name);
            Assert.Equal("test-token", response.Token);
            Assert.Equal(36000000, response.TtlMillis);
        }

        [Fact]
        public async Task UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions()
                {
                    Credential = MockCredential,
                    ProjectId = ProjectId,
                });

            FirebaseAppCheck appCheck = FirebaseAppCheck.DefaultInstance;
            app.Delete();
            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await appCheck.CreateTokenAsync(AppId));
        }

        [Fact]
        public async Task CreateTokenSuccess()
        {
            var bytes = Encoding.UTF8.GetBytes("signature");
            var handler = new MockMessageHandler()
            {
                Response = new CreatTokenResponse()
                {
                    Signature = Convert.ToBase64String(bytes),
                    Token = "test-token",
                    Ttl = "36000s",
                },
            };
            var factory = new MockHttpClientFactory(handler);

            var app = FirebaseApp.Create(
                new AppOptions()
                {
                    Credential = GoogleCredential.FromAccessToken("test-token"),
                    HttpClientFactory = factory,
                    ProjectId = ProjectId,
                },
                AppId);

            FirebaseAppCheck appCheck = FirebaseAppCheck.GetAppCheck(app);

            var response = await appCheck.CreateTokenAsync(app.Name);
            Assert.Equal("test-token", response.Token);
            Assert.Equal(36000000, response.TtlMillis);
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private sealed class CreatTokenResponse
        {
            [Newtonsoft.Json.JsonProperty("signedBlob")]
            public string Signature { get; set; }

            [Newtonsoft.Json.JsonProperty("token")]
            public string Token { get; set; }

            [Newtonsoft.Json.JsonProperty("ttl")]
            public string Ttl { get; set; }
        }
    }
}
