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
    public class BatchItemResponseTest
    {
        [Fact]
        public void SuccessfulResponse()
        {
            var response = BatchItemResponse.FromMessageId("message-id");

            Assert.Equal("message-id", response.MessageId);
            Assert.True(response.IsSuccessful);
            Assert.Null(response.Exception);
        }

        [Fact]
        public void FailureResponse()
        {
            var exception = new FirebaseException(
                "error-message",
                null);
            var response = BatchItemResponse.FromException(exception);

            Assert.Null(response.MessageId);
            Assert.False(response.IsSuccessful);
            Assert.Same(exception, response.Exception);
        }

        [Fact]
        public void MessageIdCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => BatchItemResponse.FromMessageId(null));
        }

        [Fact]
        public void MessageIdCannotBeEmpty()
        {
            Assert.Throws<ArgumentException>(() => BatchItemResponse.FromMessageId(string.Empty));
        }

        [Fact]
        public void ExceptionCannotBeNull()
        {
            Assert.Throws<ArgumentNullException>(() => BatchItemResponse.FromException(null));
        }
    }
}
