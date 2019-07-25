using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class InstanceIdClientTest
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void InstanceIdClientThrowsOnNoProjectId()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentException>(
                () => new InstanceIdClient(clientFactory, MockCredential, null));
            Assert.Throws<ArgumentException>(
                () => new InstanceIdClient(clientFactory, MockCredential, string.Empty));
        }

        [Fact]
        public void InstanceIdClientThrowsOnNoCredential()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new InstanceIdClient(clientFactory, null, "test-project"));
        }

        [Fact]
        public void InstanceIdClientThrowsOnNoClientFactory()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new InstanceIdClient(null, MockCredential, "test-project"));
        }

        [Fact]
        public async Task InstanceIdClientSubscribesToTopic()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential, "test-project");

            var result = await client.SubscribeToTopicAsync("test-topic", new List<string> { "abc123" });

            Assert.Equal(1, result.SuccessCount);
        }

        [Fact]
        public async Task InstanceIdClientUnsubscribesFromTopic()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential, "test-project");

            var result = await client.UnsubscribeFromTopicAsync("test-topic", new List<string> { "abc123" });

            Assert.Equal(1, result.SuccessCount);
        }
    }
}
