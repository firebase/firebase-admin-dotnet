using System;
using System.Collections.Generic;
using FirebaseAdmin.Messaging;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class TopicManagementResponseTest
    {
        [Fact]
        public void SuccessfulReponse()
        {
            var topicManagementResults = new List<string> { null };
            var response = new TopicManagementResponse(topicManagementResults);

            Assert.Empty(response.Errors);
            Assert.Equal(1, response.SuccessCount);
        }

        [Fact]
        public void UnsuccessfulResponse()
        {
            var topicManagementResults = new List<string> { null, "NOT_FOUND" };
            var response = new TopicManagementResponse(topicManagementResults);

            Assert.Single(response.Errors);
            Assert.Equal(1, response.SuccessCount);
            Assert.NotEmpty(response.Errors);
            Assert.Equal("registration-token-not-registered", response.Errors[0].Reason);
            Assert.Equal(1, response.Errors[0].Index);
        }

        [Fact]
        public void NullResponse()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                new TopicManagementResponse(null);
            });
        }

        [Fact]
        public void EmptyResponse()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new TopicManagementResponse(new List<string>());
            });
        }

        [Fact]
        public void UnknownError()
        {
            var topicManagementResults = new List<string> { "NOT_A_REAL_ERROR_CODE" };
            var response = new TopicManagementResponse(topicManagementResults);

            Assert.Single(response.Errors);
            Assert.Equal("unknown-error", response.Errors[0].Reason);
            Assert.Equal(0, response.Errors[0].Index);
        }

        [Fact]
        public void UnexpectedResponse()
        {
            var topicManagementResults = new List<string> { "NOT_A_REAL_CODE" };
            var response = new TopicManagementResponse(topicManagementResults);

            Assert.Single(response.Errors);
            Assert.Equal("unknown-error", response.Errors[0].Reason);
            Assert.Equal(0, response.SuccessCount);
        }

        [Fact]
        public void CountsSuccessAndErrors()
        {
            var topicManagementResults = new List<string> { "NOT_FOUND", null, "INVALID_ARGUMENT", null, null };
            var response = new TopicManagementResponse(topicManagementResults);

            Assert.Equal(2, response.FailureCount);
            Assert.Equal(3, response.SuccessCount);
            Assert.Equal("registration-token-not-registered", response.Errors[0].Reason);
            Assert.NotEmpty(response.Errors);
            Assert.Equal(0, response.Errors[0].Index);
            Assert.Equal("invalid-argument", response.Errors[1].Reason);
            Assert.Equal(2, response.Errors[1].Index);
        }
    }
}
