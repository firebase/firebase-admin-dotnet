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
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class ImportUserRecordArgsTest
    {
        [Fact]
        public void TestImportUserRecordArgsSerializationBasic()
        {
            var userProviders = new List<UserProvider>
            {
                new UserProvider()
                {
                    Uid = "google.uid",
                    ProviderId = "google.com",
                },
            };

            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            var userMetadata = new UserMetadata(1, 2, null);

            var importUserRecordArgs = new ImportUserRecordArgs()
            {
                Uid = "123",
                Email = "example@gmail.com",
                EmailVerified = true,
                DisplayName = "Example",
                PhotoUrl = "http://example.com/photo",
                PhoneNumber = "+11234567890",
                Disabled = false,
                UserMetadata = userMetadata,
                PasswordHash = Encoding.ASCII.GetBytes("secret"),
                PasswordSalt = Encoding.ASCII.GetBytes("salt"),
                CustomClaims = customClaims,
                UserProviders = userProviders,
            };

            var expected = new Dictionary<string, object>()
            {
                { "localId", "123" },
                { "email", "example@gmail.com" },
                { "photoUrl", "http://example.com/photo" },
                { "phoneNumber", "+11234567890" },
                { "displayName", "Example" },
                { "createdAt", userMetadata.CreationTimestamp },
                { "lastLoginAt", userMetadata.LastSignInTimestamp },
                { "passwordHash", "c2VjcmV0" },
                { "salt", "c2FsdA" },
                { "providerUserInfo", userProviders },
                { "customAttributes", JsonConvert.SerializeObject(customClaims) },
                { "emailVerified", true },
                { "disabled", false },
            };

            Assert.Equal(
              JsonConvert.SerializeObject(expected),
              JsonConvert.SerializeObject(importUserRecordArgs.GetProperties()));
        }

        [Fact]
        public void TestImportUserRecordArgsMissingUid()
        {
            var userProviderWithMissingUid = new ImportUserRecordArgs() { };
            Assert.Throws<ArgumentNullException>(() => userProviderWithMissingUid.GetProperties());
        }

        [Fact]
        public void TestImportUserRecordArgsInvalidEmail()
        {
            var userProviderWithMissingUid = new ImportUserRecordArgs()
            {
                Uid = "123",
                Email = "invalidemail",
            };
            Assert.Throws<ArgumentException>(() => userProviderWithMissingUid.GetProperties());
        }

        [Fact]
        public void TestImportUserRecordArgsInvalidPhone()
        {
            var userProviderWithMissingUid = new ImportUserRecordArgs()
            {
                Uid = "123",
                PhoneNumber = "11234567890",
            };
            Assert.Throws<ArgumentException>(() => userProviderWithMissingUid.GetProperties());
        }

        [Fact]
        public void TestImportUserRecordArgsReservedCustomClaims()
        {
            foreach (string reservedKey in FirebaseTokenFactory.ReservedClaims)
            {
                var userProviderWithReservedClaimKey = new ImportUserRecordArgs()
                {
                    Uid = "123",
                    CustomClaims = new Dictionary<string, object>()
                    {
                        { reservedKey, "abc" },
                    },
                };
                Assert.Throws<ArgumentException>(() => userProviderWithReservedClaimKey.GetProperties());
            }
        }
    }
}
