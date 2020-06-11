// Copyright 2020, Google Inc. All rights reserved.
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
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class UserProviderTest
    {
        [Fact]
        public void NoUid()
        {
            var userProvider = new UserProvider()
            {
                ProviderId = "abc",
            };
            Assert.Throws<ArgumentException>(() => userProvider.ToRequest());
        }

        [Fact]
        public void EmptyUid()
        {
            var userProvider = new UserProvider()
            {
                Uid = string.Empty,
            };
            Assert.Throws<ArgumentException>(() => userProvider.ToRequest());
        }

        [Fact]
        public void NoProviderId()
        {
            var userProvider = new UserProvider()
            {
                Uid = "abc",
            };
            Assert.Throws<ArgumentException>(() => userProvider.ToRequest());
        }

        [Fact]
        public void EmptyProviderId()
        {
            var userProvider = new UserProvider()
            {
                ProviderId = string.Empty,
            };
            Assert.Throws<ArgumentException>(() => userProvider.ToRequest());
        }

        [Fact]
        public void RequiredOnly()
        {
            var userProvider = new UserProvider()
            {
                Uid = "user1",
                ProviderId = "firebase",
            };

            Assert.Equal("user1", userProvider.Uid);
            Assert.Null(userProvider.DisplayName);
            Assert.Null(userProvider.Email);
            Assert.Null(userProvider.PhotoUrl);
            Assert.Equal("firebase", userProvider.ProviderId);
        }

        [Fact]
        public void AllProperties()
        {
            var userProvider = new UserProvider()
            {
                DisplayName = "displayName",
                Email = "example@gmail.com",
                PhotoUrl = "http://photo.com",
                Uid = "user1",
                ProviderId = "firebase",
            };

            Assert.Equal("user1", userProvider.Uid);
            Assert.Equal("displayName", userProvider.DisplayName);
            Assert.Equal("example@gmail.com", userProvider.Email);
            Assert.Equal("http://photo.com", userProvider.PhotoUrl);
            Assert.Equal("firebase", userProvider.ProviderId);
        }
    }
}
