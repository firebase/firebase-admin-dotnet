using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Moq;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckApiClientTest
    {
        private readonly string appId = "1:1234:android:1234";
        private readonly string testTokenToExchange = "signed-custom-token";
        private readonly string noProjectId = "Failed to determine project ID.Initialize the SDK with service "
                        + "account credentials or set project ID as an app option. Alternatively, set the "
                        + "GOOGLE_CLOUD_PROJECT environment variable.";

        [Fact]
        public void CreateInvalidApp()
        {
            Assert.Throws<ArgumentException>(() => new AppCheckApiClient(null));
        }

        [Fact]
        public async Task ExchangeTokenNoProjectId()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException(this.noProjectId));
            var result = await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, this.appId));
            Assert.Equal(this.noProjectId, result.Message);
        }

        [Fact]
        public async Task ExchangeTokenInvalidAppId()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException(this.noProjectId));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, null));
        }

        [Fact]
        public async Task ExchangeTokenInvalidCustomTokenAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException(this.noProjectId));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(string.Empty, this.appId));
            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(null, this.appId));
        }

        [Fact]
        public async Task ExchangeTokenFullErrorResponseAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException("not-found", "Requested entity not found"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, this.appId));
        }

        [Fact]
        public async Task ExchangeTokenErrorCodeAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException("unknown-error", "Unknown server error: {}"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, this.appId));
        }

        [Fact]
        public async Task ExchangeTokenFullNonJsonAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException("unknown-error", "Unexpected response with status: 404 and body: not json"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, this.appId));
        }

        [Fact]
        public async Task ExchangeTokenAppErrorAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new ArgumentException("network-error", "socket hang up"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.ExchangeTokenAsync(string.Empty, this.appId));
        }

        [Fact]
        public async Task ExchangeTokenOnSuccessAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.ExchangeTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new AppCheckToken("token", 3000));

            var result = await appCheckApiClient.Object.ExchangeTokenAsync(this.testTokenToExchange, this.appId).ConfigureAwait(false);
            Assert.NotNull(result);
            Assert.Equal("token", result.Token);
            Assert.Equal(3000, result.TtlMillis);
        }

        [Fact]
        public async Task VerifyReplayNoProjectIdAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException(this.noProjectId));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange));
        }

        [Fact]
        public async Task VerifyReplayInvaildTokenAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException(this.noProjectId));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(string.Empty));
        }

        [Fact]
        public async Task VerifyReplayFullErrorAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException("not-found", "Requested entity not found"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange));
        }

        [Fact]
        public async Task VerifyReplayErrorCodeAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException("unknown-error", "Unknown server error: {}"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange));
        }

        [Fact]
        public async Task VerifyReplayNonJsonAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException("unknown-error", "Unexpected response with status: 404 and body: not json"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange));
        }

        [Fact]
        public async Task VerifyReplayFirebaseAppErrorAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .Throws(new ArgumentException("network-error", "socket hang up"));

            await Assert.ThrowsAsync<ArgumentException>(() => appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange));
        }

        [Fact]
        public async Task VerifyReplayAlreadyTrueAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .ReturnsAsync(true);

            bool res = await appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange).ConfigureAwait(false);
            Assert.True(res);
        }

        [Fact]
        public async Task VerifyReplayAlreadyFlaseAsync()
        {
            var appCheckApiClient = new Mock<IAppCheckApiClient>();

            appCheckApiClient.Setup(service => service.VerifyReplayProtection(It.IsAny<string>()))
                .ReturnsAsync(true);

            bool res = await appCheckApiClient.Object.VerifyReplayProtection(this.testTokenToExchange).ConfigureAwait(false);
            Assert.True(res);
        }
    }
}
