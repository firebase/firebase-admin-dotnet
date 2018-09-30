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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xunit;
using FirebaseAdmin.Messaging;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseMessagingTest
    {
        public FirebaseMessagingTest()
        {
            IntegrationTestUtils.EnsureDefaultApp();
        }

        [Fact]
        public async Task Send()
        {
            var message = new Message()
            {
                Topic = "foo-bar",
                Notification = new Notification()
                {
                    Title = "Title",
                    Body = "Body",
                },
                AndroidConfig = new AndroidConfig()
                {
                    Priority = Priority.NORMAL,
                    Ttl = TimeSpan.FromHours(1),
                    RestrictedPackageName = "com.google.firebase.testing",
                },
            };
            var id = await FirebaseMessaging.DefaultInstance.SendAsync(message, dryRun: true);
            Assert.True(!string.IsNullOrEmpty(id));
            Assert.Matches(new Regex("^projects/.*/messages/.*$"), id);
        }
    }
}