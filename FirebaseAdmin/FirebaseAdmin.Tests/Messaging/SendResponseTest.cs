using System;
using FirebaseAdmin.Messaging;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class SendResponseTest
    {
        [Fact]
        public void SuccessfulResponse()
        {
            var response = SendResponse.FromMessageId("message-id");

            Assert.Equal("message-id", response.MessageId);
            Assert.True(response.IsSuccessful);
            Assert.Null(response.Exception);
        }

        [Fact]
        public void FailureResponse()
        {
            FirebaseMessagingException exception = new FirebaseMessagingException(
                "error-code",
                "error-message",
                null);
            SendResponse response = SendResponse.FromException(exception);

            Assert.Null(response.MessageId);
            Assert.False(response.IsSuccessful);
            Assert.Same(exception, response.Exception);
        }

        [Fact]
        public void MessageIdCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => SendResponse.FromMessageId(null));
        }

        [Fact]
        public void MessageIdCannotBeEmpty()
        {
            Assert.Throws<ArgumentException>(() => SendResponse.FromMessageId(string.Empty));
        }

        [Fact]
        public void ExceptionCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => SendResponse.FromException(null));
        }
    }
}
