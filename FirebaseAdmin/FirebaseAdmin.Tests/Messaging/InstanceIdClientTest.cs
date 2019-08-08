using System;
using System.Collections.Generic;
using System.Net;
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

            var result = await client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic");

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

            var result = await client.UnsubscribeFromTopicAsync(new List<string> { "abc123" }, "test-topic");

            Assert.Equal(1, result.SuccessCount);
        }

        [Fact]
        public async Task BadRequest()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.BadRequest,
                Response = "BadRequest",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal("Unexpected HTTP response with status: 400 (BadRequest)\nBadRequest", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task Unauthorized()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.Unauthorized,
                Response = "Unauthorized",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.Unauthenticated, exception.ErrorCode);
            Assert.Equal("Unexpected HTTP response with status: 401 (Unauthorized)\nUnauthorized", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task Forbidden()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.Forbidden,
                Response = "Forbidden",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.PermissionDenied, exception.ErrorCode);
            Assert.Equal("Unexpected HTTP response with status: 403 (Forbidden)\nForbidden", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task NotFound()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.NotFound,
                Response = "NotFound",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal("Unexpected HTTP response with status: 404 (NotFound)\nNotFound", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task ServiceUnavailable()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "ServiceUnavailable",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.Unavailable, exception.ErrorCode);
            Assert.Equal("Unexpected HTTP response with status: 503 (ServiceUnavailable)\nServiceUnavailable", exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }
    }
}
