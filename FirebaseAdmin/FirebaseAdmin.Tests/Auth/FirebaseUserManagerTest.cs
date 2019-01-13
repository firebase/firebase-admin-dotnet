// Copyright 2019, Google Inc. All rights reserved.
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
using Xunit;
using Google.Apis.Auth.OAuth2;

namespace FirebaseAdmin.Auth.Tests
{
    public class FirebaseUserManagerTest
    {
        private static readonly GoogleCredential mockCredential =
            GoogleCredential.FromAccessToken("test-token");
        private const string mockProjectId = "project1";

        [Fact]
        public void InvalidUid()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = mockCredential, ProjectId = mockProjectId });
            var userManager = FirebaseUserManager.Create(app);
            var customClaims = new Dictionary<string, object>()
            {
                {"admin", true}
            };

            Assert.Throws<ArgumentException>(() => new UserRecord(null));
            Assert.Throws<ArgumentException>(() => new UserRecord(""));
            Assert.Throws<ArgumentException>(() => new UserRecord(new string('a', 129)));
        }

        [Fact]
        public void ReservedClaims()
        {
            foreach (var key in FirebaseTokenFactory.ReservedClaims)
            {
                var customClaims = new Dictionary<string, object>(){
                    {key, "value"},
                };
                Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = customClaims});
            }
        }

        [Fact]
        public void EmptyClaims()
        {
            var emptyClaims = new Dictionary<string, object>(){
                    {"", "value"},
            };
            Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = emptyClaims });
        }

        [Fact]
        public void TooLargeClaimsPayload()
        {
            var customClaims = new Dictionary<string, object>();

            for(var i = 0; i < 100; ++i)
            {
                customClaims.Add($"claim{i}", $"value{i}");
            }

            Assert.Throws<ArgumentException>(() => new UserRecord("user1") { CustomClaims = customClaims });
        }
    }
}
