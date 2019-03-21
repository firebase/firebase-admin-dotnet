using System;
using System.Collections.Generic;
using System.Linq;
using FirebaseAdmin.Messaging;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class BatchResponseTest
    {
        [Fact]
        public void EmptyResponses()
        {
            var responses = new List<SendResponse>();

            var batchResponse = new BatchResponse(responses);

            Assert.Equal(0, batchResponse.SuccessCount);
            Assert.Equal(0, batchResponse.FailureCount);
            Assert.Equal(0, batchResponse.Responses.Count);
        }

        [Fact]
        public void SomeResponse()
        {
            var responses = new SendResponse[]
            {
                SendResponse.FromMessageId("message1"),
                SendResponse.FromMessageId("message2"),
                SendResponse.FromException(
                    new FirebaseMessagingException(
                        "error-code",
                        "error-message",
                        null)),
            };

            var batchResponse = new BatchResponse(responses);

            Assert.Equal(2, batchResponse.SuccessCount);
            Assert.Equal(1, batchResponse.FailureCount);
            Assert.Equal(3, batchResponse.Responses.Count);
            Assert.True(responses.SequenceEqual(batchResponse.Responses));
        }

        [Fact]
        public void ResponsesCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => new BatchResponse(null));
        }
    }
}
