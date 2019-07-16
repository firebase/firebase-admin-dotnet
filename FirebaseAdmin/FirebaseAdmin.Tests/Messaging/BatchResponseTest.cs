// Copyright 2018, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
                    new FirebaseMessagingException(ErrorCode.Unknown, "error-message")),
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
