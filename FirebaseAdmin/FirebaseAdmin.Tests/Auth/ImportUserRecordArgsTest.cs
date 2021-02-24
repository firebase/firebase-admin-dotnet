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
using System.Linq;
using System.Text;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Json;
using Newtonsoft.Json;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class ImportUserRecordArgsTest
    {
        [Fact]
        public void Serialize()
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
            var passwordHash = Encoding.ASCII.GetBytes("secret");
            var passwordSalt = Encoding.ASCII.GetBytes("salt");

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
                PasswordHash = passwordHash,
                PasswordSalt = passwordSalt,
                CustomClaims = customClaims,
                UserProviders = userProviders,
            };

            var expected = new Dictionary<string, object>()
            {
                { "createdAt", userMetadata.CreationTimestamp },
                { "customAttributes", JsonConvert.SerializeObject(customClaims) },
                { "disabled", false },
                { "displayName", "Example" },
                { "email", "example@gmail.com" },
                { "emailVerified", true },
                { "lastLoginAt", userMetadata.LastSignInTimestamp },
                { "passwordHash", JwtUtils.UrlSafeBase64Encode(passwordHash) },
                { "salt", JwtUtils.UrlSafeBase64Encode(passwordSalt) },
                { "phoneNumber", "+11234567890" },
                { "photoUrl", "http://example.com/photo" },
                { "providerUserInfo", userProviders.Select(userProvider => userProvider.ToRequest()) },
                { "localId", "123" },
            };

            Assert.Equal(
              JsonConvert.SerializeObject(expected),
              JsonConvert.SerializeObject(importUserRecordArgs.ToRequest()));
        }

        [Fact]
        public void RequiredOnly()
        {
            var userRecordMinimal = new ImportUserRecordArgs()
            {
                Uid = "123",
            };

            var expected = new Dictionary<string, object>()
            {
                { "localId", "123" },
            };
            Assert.Equal(
                NewtonsoftJsonSerializer.Instance.Serialize(expected),
                NewtonsoftJsonSerializer.Instance.Serialize(userRecordMinimal.ToRequest()));
        }

        [Fact]
        public void MissingUid()
        {
            var userRecordArgsWithMissingUid = new ImportUserRecordArgs() { };
            Assert.Throws<ArgumentNullException>(
                () => userRecordArgsWithMissingUid.ToRequest());
        }

        [Fact]
        public void InvalidEmail()
        {
            var userRecordArgsInvalidEmail = new ImportUserRecordArgs()
            {
                Uid = "123",
                Email = "invalidemail",
            };
            Assert.Throws<ArgumentException>(
                () => userRecordArgsInvalidEmail.ToRequest());
        }

        [Fact]
        public void InvalidPhone()
        {
            var userProviderWithMissingUid = new ImportUserRecordArgs()
            {
                Uid = "123",
                PhoneNumber = "11234567890",
            };
            Assert.Throws<ArgumentException>(
                () => userProviderWithMissingUid.ToRequest());
        }

        [Fact]
        public void ReservedCustomClaims()
        {
            foreach (var reservedKey in FirebaseTokenFactory.ReservedClaims)
            {
                var userProviderWithReservedClaimKey = new ImportUserRecordArgs()
                {
                    Uid = "123",
                    CustomClaims = new Dictionary<string, object>()
                    {
                        { reservedKey, "abc" },
                    },
                };
                Assert.Throws<ArgumentException>(
                    () => userProviderWithReservedClaimKey.ToRequest());
            }
        }
    }
}
