using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
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
        public void NoCredential()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new InstanceIdClient(clientFactory, null));
        }

        [Fact]
        public void NoClientFactory()
        {
            var clientFactory = new HttpClientFactory();
            Assert.Throws<ArgumentNullException>(
                () => new InstanceIdClient(null, MockCredential));
        }

        [Fact]
        public async Task SubscribeToTopicAsync()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var result = await client.SubscribeToTopicAsync("test-topic", new List<string> { "abc123" });

            Assert.Equal(1, result.SuccessCount);
        }

        [Fact]
        public async Task UnsubscribeFromTopicAsync()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""results"":[{}]}",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var result = await client.UnsubscribeFromTopicAsync("test-topic", new List<string> { "abc123" });

            Assert.Equal(1, result.SuccessCount);
        }
    }
}
