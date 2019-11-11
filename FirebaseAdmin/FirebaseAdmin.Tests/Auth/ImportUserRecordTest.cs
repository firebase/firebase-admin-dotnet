using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class ImportUserRecordTest
    {
        [Fact]
        public void NullResponse()
        {
            Assert.Throws<ArgumentException>(() => new ImportUserRecord(null));
        }

        [Fact]
        public void NullUid()
        {
            Assert.Throws<ArgumentException>(() => new ImportUserRecord(
                new GetAccountInfoResponse.User()
                {
                    UserId = null,
                }));
        }

        [Fact]
        public void EmptyUid()
        {
            Assert.Throws<ArgumentException>(() => new ImportUserRecord(
                new GetAccountInfoResponse.User()
                {
                    UserId = string.Empty,
                }));
        }

        [Fact]
        public void UidOnly()
        {
            var user = new ImportUserRecord(new GetAccountInfoResponse.User()
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
            Assert.Null(user.PasswordHash);
            Assert.Null(user.PasswordSalt);
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
                PasswordHash = "secret",
                PasswordSalt = "mgcl2",
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
            };
            var user = new ImportUserRecord(response);

            Assert.Equal("user1", user.Uid);
            Assert.Equal("Test User", user.DisplayName);
            Assert.Equal("user@domain.com", user.Email);
            Assert.Equal("+11234567890", user.PhoneNumber);
            Assert.Equal("https://domain.com/user.png", user.PhotoUrl);
            Assert.Equal("firebase", user.ProviderId);
            Assert.True(user.Disabled);
            Assert.True(user.EmailVerified);
            Assert.Equal(UserRecord.UnixEpoch.AddSeconds(3600), user.TokensValidAfterTimestamp);
            Assert.Equal("secret", user.PasswordHash);
            Assert.Equal("mgcl2", user.PasswordSalt);

            var claims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "level", 10L },
            };
            Assert.Equal(claims, user.CustomClaims);

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

            var metadata = user.UserMetaData;
            Assert.NotNull(metadata);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(100), metadata.CreationTimestamp);
            Assert.Equal(UserRecord.UnixEpoch.AddMilliseconds(150), metadata.LastSignInTimestamp);
        }
    }
}
