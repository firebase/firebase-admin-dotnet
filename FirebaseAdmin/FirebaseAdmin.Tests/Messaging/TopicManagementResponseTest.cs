using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class TopicManagementResponseTest
    {
        [Fact]
        public void SuccessfulTopicManagementResponse()
        {
            var json = @"[{}, {}]";
            var jObjects = JArray.Parse(json).ToObject<List<JObject>>();
            var response = new TopicManagementResponse(jObjects);

            Assert.Equal(0, response.GetFailureCount());
            Assert.Equal(2, response.GetSuccessCount());
        }

        [Fact]
        public void UnsuccessfulTopicManagementResponse()
        {
            var json = @"[{}, {""error"":""NOT_FOUND""}]";
            var jObjects = JArray.Parse(json).ToObject<List<JObject>>();
            var response = new TopicManagementResponse(jObjects);

            Assert.Equal(1, response.GetFailureCount());
            Assert.Equal(1, response.GetSuccessCount());
        }

        [Fact]
        public void TopicManagementResponseCannotBeNull()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                List<JObject> jObjects = null;
                var response = new TopicManagementResponse(jObjects);
            });
        }

        [Fact]
        public void TopicManagementResponseCannotBeEmptyArray()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                List<JObject> jObjects = new List<JObject>();
                var response = new TopicManagementResponse(jObjects);
            });
        }

        [Fact]
        public void TopicManagementResponseHandlesUnknownErrors()
        {
            var json = @"[{""error"":""NOT_A_REAL_ERROR_CODE""}]";
            var jObjects = JArray.Parse(json).ToObject<List<JObject>>();
            var response = new TopicManagementResponse(jObjects);

            Assert.Equal(1, response.GetFailureCount());
            Assert.Equal(0, response.GetSuccessCount());
        }

        [Fact]
        public void TopicManagementResponseHandlesUnexpectedResponse()
        {
            var json = @"[{""unexpected"":""NOT_A_REAL_CODE""}]";
            var jObjects = JArray.Parse(json).ToObject<List<JObject>>();
            var response = new TopicManagementResponse(jObjects);

            Assert.Equal(0, response.GetFailureCount());
            Assert.Equal(1, response.GetSuccessCount());
        }

        [Fact]
        public void TopicManagementResponseCountsSuccessAndErrors()
        {
            var json = @"[{""error"": ""NOT_FOUND""}, {}, {""error"": ""NOT_FOUND""}, {}, {}]";
            var jObjects = JArray.Parse(json).ToObject<List<JObject>>();
            var response = new TopicManagementResponse(jObjects);

            Assert.Equal(2, response.GetFailureCount());
            Assert.Equal(3, response.GetSuccessCount());
        }
    }
}
