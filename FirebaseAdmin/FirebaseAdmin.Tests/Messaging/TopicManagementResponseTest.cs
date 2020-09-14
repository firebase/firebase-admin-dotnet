using System;
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
            var json = @"{""results"": [{}, {}]}";
            var instanceIdServiceResponse = JsonConvert.DeserializeObject<InstanceIdServiceResponse>(json);
            var response = new TopicManagementResponse(instanceIdServiceResponse);

            Assert.Equal(0, response.FailureCount);
            Assert.Equal(2, response.SuccessCount);
        }

        [Fact]
        public void UnsuccessfulResponse()
        {
            var json = @"{""results"": [{}, {""error"":""NOT_FOUND""}]}";
            var instanceIdServiceResponse = JsonConvert.DeserializeObject<InstanceIdServiceResponse>(json);
            var response = new TopicManagementResponse(instanceIdServiceResponse);

            Assert.Equal(1, response.FailureCount);
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
            Assert.Throws<ArgumentNullException>(() =>
            {
                var instanceIdServiceResponse = new InstanceIdServiceResponse();
                new TopicManagementResponse(instanceIdServiceResponse);
            });
        }

        [Fact]
        public void UnregisteredToken()
        {
            var json = @"{""results"": [{}, {""error"":""NOT_FOUND""}]}";
            var instanceIdServiceResponse = JsonConvert.DeserializeObject<InstanceIdServiceResponse>(json);
            var response = new TopicManagementResponse(instanceIdServiceResponse);

            Assert.Single(response.Errors);
            Assert.Equal("registration-token-not-registered", response.Errors[0].Reason);
            Assert.Equal(1, response.Errors[0].Index);
        }

        [Fact]
        public void CountsSuccessAndErrors()
        {
            var json = @"{""results"": [{""error"": ""NOT_FOUND""}, {}, {""error"": ""INVALID_ARGUMENT""}, {}, {}]}";
            var instanceIdServiceResponse = JsonConvert.DeserializeObject<InstanceIdServiceResponse>(json);
            var response = new TopicManagementResponse(instanceIdServiceResponse);

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
