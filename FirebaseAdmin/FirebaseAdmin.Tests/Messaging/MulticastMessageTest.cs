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
using System.Linq;
using FirebaseAdmin.Messaging;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class MulticastMessageTest
    {
#pragma warning disable CS0618
        [Fact]
        public void GetMessageList()
        {
            var message = new MulticastMessage
            {
                Tokens = new[] { "test-token1", "test-token2" },
            };

            var messages = message.GetMessageList();

            Assert.Equal(2, messages.Count);
            Assert.Equal("test-token1", messages[0].Token);
            Assert.Equal("test-token2", messages[1].Token);
        }
#pragma warning restore CS0618

        [Fact]
        public void GetMessageListFids()
        {
            var message = new MulticastMessage
            {
                Fids = new[] { "test-fid1", "test-fid2" },
            };

            var messages = message.GetMessageList();

            Assert.Equal(2, messages.Count);
            Assert.Equal("test-fid1", messages[0].Fid);
            Assert.Equal("test-fid2", messages[1].Fid);
        }

        [Fact]
        public void GetMessageListNoTargets()
        {
            var message = new MulticastMessage();

            Assert.Throws<ArgumentException>(() => message.GetMessageList());
        }

#pragma warning disable CS0618
        [Fact]
        public void GetMessageListBothTargets()
        {
            var message = new MulticastMessage
            {
                Tokens = new[] { "test-token1" },
                Fids = new[] { "test-fid1" },
            };

            var messages = message.GetMessageList();

            Assert.Equal(2, messages.Count);
            Assert.Equal("test-token1", messages[0].Token);
            Assert.Equal("test-fid1", messages[1].Fid);
        }
#pragma warning restore CS0618

#pragma warning disable CS0618
        [Fact]
        public void GetMessageListTooManyTokens()
        {
            var message = new MulticastMessage
            {
                Tokens = Enumerable.Range(0, 501).Select(x => x.ToString()).ToList(),
            };

            Assert.Throws<ArgumentException>(() => message.GetMessageList());
        }
#pragma warning restore CS0618

        [Fact]
        public void GetMessageListTooManyFids()
        {
            var message = new MulticastMessage
            {
                Fids = Enumerable.Range(0, 501).Select(x => x.ToString()).ToList(),
            };

            Assert.Throws<ArgumentException>(() => message.GetMessageList());
        }

#pragma warning disable CS0618
        [Fact]
        public void GetMessageListTooManyCombinedTargets()
        {
            var message = new MulticastMessage
            {
                Tokens = Enumerable.Range(0, 250).Select(x => x.ToString()).ToList(),
                Fids = Enumerable.Range(0, 251).Select(x => x.ToString()).ToList(),
            };

            Assert.Throws<ArgumentException>(() => message.GetMessageList());
        }
#pragma warning restore CS0618
    }
}
