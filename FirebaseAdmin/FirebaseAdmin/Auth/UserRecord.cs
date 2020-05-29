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
using System.Text.RegularExpressions;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account. Instances
    /// of this class are immutable and thread safe.
    /// </summary>
    public class UserRecord : IUserInfo
    {
        /// <summary>
        /// Key name for custom attributes.
        /// </summary>
        public const string CustomAttributes = "customAttributes";

        internal static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const int MaxUidLength = 128;

        private const string DefaultProviderId = "firebase";

        private readonly long validSinceTimestampInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class from an existing instance of the
        /// <see cref="GetAccountInfoResponse.User"/> class.
        /// </summary>
        /// <param name="user">The <see cref="GetAccountInfoResponse.User"/> instance to copy the user's data
        /// from.</param>
        internal UserRecord(GetAccountInfoResponse.User user)
        {
            if (user == null)
            {
                throw new ArgumentException("User object must not be null or empty.");
            }
            else if (string.IsNullOrEmpty(user.UserId))
            {
                throw new ArgumentException("User ID must not be null or empty.");
            }

            this.Uid = user.UserId;
            this.Email = user.Email;
            this.PhoneNumber = user.PhoneNumber;
            this.EmailVerified = user.EmailVerified;
            this.DisplayName = user.DisplayName;
            this.PhotoUrl = user.PhotoUrl;
            this.Disabled = user.Disabled;

            if (user.Providers == null || user.Providers.Count == 0)
            {
                this.ProviderData = new IUserInfo[0];
            }
            else
            {
                var count = user.Providers.Count;
                this.ProviderData = new IUserInfo[count];
                for (int i = 0; i < count; i++)
                {
                    this.ProviderData[i] = new ProviderUserInfo(user.Providers[i]);
                }
            }

            this.validSinceTimestampInSeconds = user.ValidSince;

            // newtonsoft's json deserializer will convert an iso8601 format
            // string to a (non-null) DateTime, returning 0001-01-01 if it's not
            // present in the proto. We'll compare against the epoch and only
            // use the deserialized value if it's bigger.
            DateTime? lastRefreshAt = null;
            if (user.LastRefreshAt > UnixEpoch)
            {
                lastRefreshAt = user.LastRefreshAt;
            }

            this.UserMetaData = new UserMetadata(user.CreatedAt, user.LastLoginAt, lastRefreshAt);
            this.CustomClaims = UserRecord.ParseCustomClaims(user.CustomClaims);
        }

        /// <summary>
        /// Gets the user ID of this user.
        /// </summary>
        public string Uid { get; }

        /// <summary>
        /// Gets the user's display name, if available. Otherwise null.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the user's email address, if available. Otherwise null.
        /// </summary>
        public string Email { get; }

        /// <summary>
        /// Gets the user's phone number, if available. Otherwise null.
        /// </summary>
        public string PhoneNumber { get; }

        /// <summary>
        /// Gets the user's photo URL, if available. Otherwise null.
        /// </summary>
        public string PhotoUrl { get; }

        /// <summary>
        /// Gets the ID of the identity provider. This returns the constant value <c>firebase</c>.
        /// </summary>
        public string ProviderId => UserRecord.DefaultProviderId;

        /// <summary>
        /// Gets a value indicating whether the user's email address is verified or not.
        /// </summary>
        public bool EmailVerified { get; }

        /// <summary>
        /// Gets a value indicating whether the user account is disabled or not.
        /// </summary>
        public bool Disabled { get; }

        /// <summary>
        /// Gets a non-null array of provider data for this user. Possibly empty.
        /// </summary>
        public IUserInfo[] ProviderData { get; }

        /// <summary>
        /// Gets a timestamp that indicates the earliest point in time at which a valid ID token
        /// could have been issued to this user. Tokens issued prior to this timestamp are
        /// considered invalid.
        /// </summary>
        public DateTime TokensValidAfterTimestamp
        {
            get
            {
                return UnixEpoch.AddSeconds(this.validSinceTimestampInSeconds);
            }
        }

        /// <summary>
        /// Gets additional user metadata. This is guaranteed not to be null.
        /// </summary>
        public UserMetadata UserMetaData { get; }

        /// <summary>
        /// Gets the custom claims set on this user, as a non-null dictionary. Possibly empty.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomClaims { get; }

        /// <summary>
        /// Verifies if a provided uid is valid (which is defined as not empty/null and not longer
        /// than MaxUidLength).
        /// </summary>
        /// <param name="uid">uid to be verified.</param>
        public static void CheckUid(string uid)
        {
          if (string.IsNullOrEmpty(uid))
          {
            throw new ArgumentException("uid cannot be null or empty");
          }

          if (uid.Length > MaxUidLength)
          {
            throw new ArgumentException($"uid cannot be longer than {MaxUidLength} characters");
          }
        }

        /// <summary>
        /// Verifies if a provided email is valid (which is defined as not empty/null and matches
        /// a particular regex pattern).
        /// </summary>
        /// <param name="email">email to be verified.</param>
        public static void CheckEmail(string email)
        {
          if (string.IsNullOrEmpty(email))
          {
            throw new ArgumentException("email cannot be null or empty");
          }

          if (!Regex.IsMatch(email, "^[^@]+@[^@]+$"))
          {
            throw new ArgumentException("email is not a valid address");
          }
        }

        /// <summary>
        /// Verifies if a provided phone number is valid (which is defined as not empty/null and
        /// starts with '+' sign). Backend will enforce E.164 spec compliance, and normalize
        /// accordingly.
        /// </summary>
        /// <param name="phoneNumber">phone number to be verified.</param>
        public static void CheckPhoneNumber(string phoneNumber)
        {
          if (string.IsNullOrEmpty(phoneNumber))
          {
            throw new ArgumentException("phone number cannot be null or empty");
          }

          if (!phoneNumber.StartsWith("+"))
          {
            throw new ArgumentException("phone number must be a valid, E.164 compliant identifier starting with a '+' sign");
          }
        }

        /// <summary>
        /// Verifies if a provided photo url is valid (which is defined as not empty/null and
        /// is a well formed uri string (in accordance with RFC 2396 and RFC 2732)).
        /// </summary>
        /// <param name="photoUrl">photo url to be verified.</param>
        public static void CheckUrl(string photoUrl)
        {
          if (string.IsNullOrEmpty(photoUrl))
          {
            throw new ArgumentException("photoUrl cannot be null or empty");
          }

          if (!Uri.IsWellFormedUriString(photoUrl, UriKind.Absolute))
          {
            throw new ArgumentException("malformed uri string");
          }
        }

        /// <summary>
        /// Verifies if a provided custom claims dictionary is valid (which is defined
        /// as having no empty/null keys and no reserved claims).
        /// </summary>
        /// <param name="customClaims">customClaims dictionary to be verified.</param>
        public static void CheckCustomClaims(IReadOnlyDictionary<string, object> customClaims)
        {
          foreach (KeyValuePair<string, object> entry in customClaims)
          {
            if (string.IsNullOrEmpty(entry.Key))
            {
              throw new ArgumentException("Claim names must not be null or empty");
            }

            if (FirebaseTokenFactory.ReservedClaims.Contains(entry.Key))
            {
              throw new ArgumentException($"Claim '{entry.Key} is reserved and cannot be set");
            }
          }
        }

        private static IReadOnlyDictionary<string, object> ParseCustomClaims(string customClaims)
        {
          if (string.IsNullOrEmpty(customClaims))
          {
            return new Dictionary<string, object>();
          }
          else
          {
            return NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(customClaims);
          }
        }
    }
}
