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
using FirebaseAdmin.Auth.Users;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class UserRecordTest
    {
        [Fact]
        public void NullResponse()
        {
            Assert.Throws<ArgumentException>(() => new UserRecord(null));
        }

        [Fact]
        public void NullUid()
        {
            Assert.Throws<ArgumentException>(() => new UserRecord(new GetAccountInfoResponse.User()
            {
                UserId = null,
            }));
        }

        [Fact]
        public void EmptyUid()
        {
            Assert.Throws<ArgumentException>(() => new UserRecord(new GetAccountInfoResponse.User()
            {
                UserId = string.Empty,
            }));
        }

        [Fact]
        public void UidOnly()
        {
            var user = new UserRecord(new GetAccountInfoResponse.User()
            {
                UserId = "user1",
            });

            Assert.Equal("user1", user.Uid);
            Assert.Null(user.DisplayName);
            Assert.Null(user.Email);
            Assert.Null(user.PhoneNumber);
            Assert.Null(user.PhotoUrl);
            Assert.Equal("firebase", user.ProviderId);
            Assert.False(user.Disabled);
            Assert.False(user.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch, user.TokensValidAfterTimestamp);
            Assert.Empty(user.CustomClaims);
            Assert.Empty(user.ProviderData);
            Assert.NotNull(user.UserMetaData);
            Assert.Null(user.UserMetaData.CreationTimestamp);
            Assert.Null(user.UserMetaData.LastSignInTimestamp);
        }

        [Fact]
        public void AllProperties()
        {
            var response = new GetAccountInfoResponse.User()
            {
                UserId = "user1",
                DisplayName = "Test User",
                Email = "user@domain.com",
                PhoneNumber = "+11234567890",
                PhotoUrl = "https://domain.com/user.png",
                Disabled = true,
                EmailVerified = true,
                ValidSince = 3600,
                CreatedAt = 100,
                LastLoginAt = 150,
                CustomClaims = @"{""admin"": true, ""level"": 10}",
                TenantId = "tenant1",
                Providers = new List<GetAccountInfoResponse.Provider>()
                {
                    new GetAccountInfoResponse.Provider()
                    {
                        ProviderID = "google.com",
                        UserId = "googleuid",
                    },
                    new GetAccountInfoResponse.Provider()
                    {
                        ProviderID = "other.com",
                        UserId = "otheruid",
                        DisplayName = "Other Name",
                        Email = "user@other.com",
                        PhotoUrl = "https://other.com/user.png",
                        PhoneNumber = "+10987654321",
                    },
                },
                Mfa = new List<GetAccountInfoResponse.MfaEnrollment>()
                {
                    new GetAccountInfoResponse.MfaEnrollment()
                    {
                        MfaEnrollmentId = "mfa1",
                        DisplayName = "SecondFactor",
                        PhoneInfo = "*********321",
                        EnrolledAt = "2014 - 10 - 03T15:01:23Z",
                    },
                    new GetAccountInfoResponse.MfaEnrollment()
                    {
                        MfaEnrollmentId = "mfa2",
                        DisplayName = "SecondSecondFactor",
                        PhoneInfo = "*********322",
                        EnrolledAt = "2014 - 10 - 03T15:01:23Z",
                    },
                    new GetAccountInfoResponse.MfaEnrollment()
                    {
                        MfaEnrollmentId = "totp",
                        DisplayName = "totp",
                        TotpInfo = new(),
                        EnrolledAt = "2014 - 10 - 03T15:01:23Z",
                    },
                },
            };
            var user = new UserRecord(response);

            Assert.Equal("user1", user.Uid);
            Assert.Equal("Test User", user.DisplayName);
            Assert.Equal("user@domain.com", user.Email);
            Assert.Equal("+11234567890", user.PhoneNumber);
            Assert.Equal("https://domain.com/user.png", user.PhotoUrl);
            Assert.Equal("firebase", user.ProviderId);
            Assert.True(user.Disabled);
            Assert.True(user.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch.AddSeconds(3600), user.TokensValidAfterTimestamp);

            var claims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "level", 10L },
            };
            Assert.Equal(claims, user.CustomClaims);
            Assert.Equal("tenant1", user.TenantId);

            Assert.Equal(2, user.ProviderData.Length);
            var provider = user.ProviderData[0];
            Assert.Equal("google.com", provider.ProviderId);
            Assert.Equal("googleuid", provider.Uid);
            Assert.Null(provider.DisplayName);
            Assert.Null(provider.Email);
            Assert.Null(provider.PhoneNumber);
            Assert.Null(provider.PhotoUrl);

            provider = user.ProviderData[1];
            Assert.Equal("other.com", provider.ProviderId);
            Assert.Equal("otheruid", provider.Uid);
            Assert.Equal("Other Name", provider.DisplayName);
            Assert.Equal("user@other.com", provider.Email);
            Assert.Equal("+10987654321", provider.PhoneNumber);
            Assert.Equal("https://other.com/user.png", provider.PhotoUrl);

            var mfaEnrollment = user.Mfa[0];
            Assert.Equal("mfa1", mfaEnrollment.MfaEnrollmentId);
            Assert.Equal("SecondFactor", mfaEnrollment.DisplayName);
            Assert.Equal("*********321", mfaEnrollment.PhoneInfo);
            Assert.Equal("2014 - 10 - 03T15:01:23Z", mfaEnrollment.EnrolledAt);
            Assert.Equal(MfaFactorIdType.Phone, mfaEnrollment.MfaFactorId);
            Assert.Null(mfaEnrollment.UnobfuscatedPhoneInfo);

            mfaEnrollment = user.Mfa[1];
            Assert.Equal("mfa2", mfaEnrollment.MfaEnrollmentId);
            Assert.Equal("SecondSecondFactor", mfaEnrollment.DisplayName);
            Assert.Equal("*********322", mfaEnrollment.PhoneInfo);
            Assert.Equal("2014 - 10 - 03T15:01:23Z", mfaEnrollment.EnrolledAt);
            Assert.Equal(MfaFactorIdType.Phone, mfaEnrollment.MfaFactorId);
            Assert.Null(mfaEnrollment.UnobfuscatedPhoneInfo);

            mfaEnrollment = user.Mfa[2];
            Assert.Equal("totp", mfaEnrollment.MfaEnrollmentId);
            Assert.Equal("totp", mfaEnrollment.DisplayName);
            Assert.Equal("2014 - 10 - 03T15:01:23Z", mfaEnrollment.EnrolledAt);
            Assert.Equal(MfaFactorIdType.Totp, mfaEnrollment.MfaFactorId);
            Assert.Null(mfaEnrollment.UnobfuscatedPhoneInfo);
            Assert.Null(mfaEnrollment.PhoneInfo);

            var metadata = user.UserMetaData;
            Assert.NotNull(metadata);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(100), metadata.CreationTimestamp);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(150), metadata.LastSignInTimestamp);
        }
    }
}
