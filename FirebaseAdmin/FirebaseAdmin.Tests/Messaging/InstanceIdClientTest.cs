using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Messaging;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class InstanceIdClientTest
    {
        public static readonly IEnumerable<object[]> ErrorCodes =
            new List<object[]>()
            {
                new object[] { HttpStatusCode.BadRequest, ErrorCode.InvalidArgument },
                new object[] { HttpStatusCode.Unauthorized, ErrorCode.Unauthenticated },
                new object[] { HttpStatusCode.Forbidden, ErrorCode.PermissionDenied },
                new object[] { HttpStatusCode.NotFound, ErrorCode.NotFound },
                new object[] { HttpStatusCode.ServiceUnavailable, ErrorCode.Unavailable },
            };

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

        [Theory]
        [MemberData(nameof(ErrorCodes))]
        public async Task ErrorResponse(HttpStatusCode statusCode, ErrorCode expected)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = statusCode,
                Response = @"{""error"":""ErrorCode""}",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(expected, exception.ErrorCode);
            Assert.Equal(
                "Error while calling the IID service: ErrorCode",
                exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Theory]
        [MemberData(nameof(ErrorCodes))]
        public async Task UnexpectedErrorResponse(HttpStatusCode statusCode, ErrorCode expected)
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = statusCode,
                Response = "NonJsonErrorResponse",
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(expected, exception.ErrorCode);
            Assert.Equal(
                "Unexpected HTTP response with status: "
                + $"{(int)statusCode} ({statusCode}){Environment.NewLine}NonJsonErrorResponse",
                exception.Message);
            Assert.Null(exception.MessagingErrorCode);
            Assert.NotNull(exception.HttpResponse);
            Assert.Null(exception.InnerException);
        }

        [Fact]
        public async Task TransportError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Transport error"),
            };
            var factory = new MockHttpClientFactory(handler);

            var client = new InstanceIdClient(factory, MockCredential, RetryOptions.NoBackOff);

            var exception = await Assert.ThrowsAsync<FirebaseMessagingException>(
               () => client.SubscribeToTopicAsync(new List<string> { "abc123" }, "test-topic"));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Null(exception.MessagingErrorCode);
            Assert.Null(exception.HttpResponse);
            Assert.NotNull(exception.InnerException);
            Assert.Equal(5, handler.Calls);
        }
    }
}
