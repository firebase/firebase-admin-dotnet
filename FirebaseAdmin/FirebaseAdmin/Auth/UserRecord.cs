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
using Google.Apis.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account. Instances
    /// of this class are immutable and thread safe.
    /// </summary>
    public class UserRecord : IUserInfo
    {
        internal static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const string DefaultProviderId = "firebase";

        private readonly long validSinceTimestampInSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class from an existing
        /// instance of the <see cref="GetAccountInfoResponse.User"/> class.
        /// </summary>
        /// <param name="user">The <see cref="GetAccountInfoResponse.User"/> instance to copy
        /// the user's data from.</param>
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
            this.TenantId = user.TenantId;
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
        /// Gets the user's tenant ID, if available. Otherwise null.
        /// </summary>
        public string TenantId { get; }

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
