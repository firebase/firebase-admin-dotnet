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
            Assert.True(response.IsSuccess);
            Assert.Null(response.Exception);
        }

        [Fact]
        public void FailureResponse()
        {
            var exception = new FirebaseMessagingException(ErrorCode.Unknown, "error-message");
            var response = SendResponse.FromException(exception);

            Assert.Null(response.MessageId);
            Assert.False(response.IsSuccess);
            Assert.Same(exception, response.Exception);
        }

        [Fact]
        public void MessageIdCannotBeNull()
        {
            Assert.Throws<ArgumentException>(() => SendResponse.FromMessageId(null));
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
