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
using System.Collections.ObjectModel;
using System.Text;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account. Instances
    /// of this class are immutable and thread safe.
    /// </summary>
    public sealed class UserRecord : IUserInfo
    {
        internal static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private const string DefaultProviderId = "firebase";

        private string uid;
        private string email;
        private string phoneNumber;
        private bool emailVerified;
        private string displayName;
        private string photoUrl;
        private bool disabled;
        private IUserInfo[] providers;
        private long validSinceTimestampInSeconds;
        private UserMetadata userMetaData;
        private IReadOnlyDictionary<string, object> customClaims;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class with the specified user ID.
        /// </summary>
        /// <param name="uid">The user's ID.</param>
        internal UserRecord(string uid)
        {
            if (string.IsNullOrEmpty(uid) || uid.Length > 128)
            {
                throw new ArgumentException("User ID must not be null or empty, and be 128 characters or shorter.");
            }

            this.uid = uid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class from an existing instance of the <see cref="GetAccountInfoResponse.User"/> class.
        /// </summary>
        /// <param name="user">The <see cref="GetAccountInfoResponse.User"/> instance to copy the user's data from.</param>
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

            this.uid = user.UserId;
            this.email = user.Email;
            this.phoneNumber = user.PhoneNumber;
            this.emailVerified = user.EmailVerified;
            this.displayName = user.DisplayName;
            this.photoUrl = user.PhotoUrl;
            this.disabled = user.Disabled;

            if (user.Providers == null || user.Providers.Count == 0)
            {
                this.providers = new IUserInfo[0];
            }
            else
            {
                var count = user.Providers.Count;
                this.providers = new IUserInfo[count];
                for (int i = 0; i < count; i++)
                {
                    this.providers[i] = new ProviderUserInfo(user.Providers[i]);
                }
            }

            this.validSinceTimestampInSeconds = user.ValidSince;
            this.userMetaData = new UserMetadata(user.CreatedAt, user.LastLoginAt);
            this.customClaims = UserRecord.ParseCustomClaims(user.CustomClaims);
        }

        /// <summary>
        /// Gets the user ID of this user.
        /// </summary>
        public string Uid
        {
            get => this.uid;
            private set
            {
                CheckUid(value);
                this.uid = value;
            }
        }

        /// <summary>
        /// Gets the user's display name, if available. Otherwise null.
        /// </summary>
        public string DisplayName
        {
            get => this.displayName;
        }

        /// <summary>
        /// Gets the user's email address, if available. Otherwise null.
        /// </summary>
        public string Email
        {
            get => this.email;
        }

        /// <summary>
        /// Gets the user's phone number, if available. Otherwise null.
        /// </summary>
        public string PhoneNumber
        {
            get => this.phoneNumber;
        }

        /// <summary>
        /// Gets the user's photo URL, if available. Otherwise null.
        /// </summary>
        public string PhotoUrl
        {
            get => this.photoUrl;
        }

        /// <summary>
        /// Gets the ID of the identity provider. This has the constant value <c>firebase</c>.
        /// </summary>
        public string ProviderId
        {
            get => UserRecord.DefaultProviderId;
        }

        /// <summary>
        /// Gets a value indicating whether the user's email address is verified or not.
        /// </summary>
        public bool EmailVerified => this.emailVerified;

        /// <summary>
        /// Gets a value indicating whether the user account is disabled or not.
        /// </summary>
        public bool Disabled => this.disabled;

        /// <summary>
        /// Gets a non-null array of provider data for this user. Possibly empty.
        /// </summary>
        public IUserInfo[] ProviderData => this.providers;

        /// <summary>
        /// Gets a timestamp that indicates the earliest point in time at which a valid ID token
        /// could have been issued to this user. Tokens issued prior to this  timestamp are
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
        /// Gets additional user metadata.
        /// </summary>
        public UserMetadata UserMetaData => this.userMetaData;

        /// <summary>
        /// Gets the custom claims set on this user.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, object> CustomClaims
        {
            get => this.customClaims;
            internal set
            {
                CheckCustomClaims(value);
                this.customClaims = value;
            }
        }

        [JsonProperty("customAttributes")]
        internal string CustomClaimsString => SerializeClaims(CustomClaims);

        /// <summary>
        /// Checks if the given user ID is valid.
        /// </summary>
        /// <param name="uid">The user ID. Must not be null or longer than
        /// 128 characters.</param>
        private static void CheckUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("uid must not be null or empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("uid must not be longer than 128 characters");
            }
        }

        /// <summary>
        /// Checks if the given set of custom claims are valid.
        /// </summary>
        /// <param name="customClaims">The custom claims. Claim names must
        /// not be null or empty and must not be reserved and the serialized
        /// claims have to be less than 1000 bytes.</param>
        private static void CheckCustomClaims(IReadOnlyDictionary<string, object> customClaims)
        {
            if (customClaims == null)
            {
                return;
            }

            foreach (var key in customClaims.Keys)
            {
                if (string.IsNullOrEmpty(key))
                {
                    throw new ArgumentException("Claim names must not be null or empty");
                }

                if (FirebaseTokenFactory.ReservedClaims.Contains(key))
                {
                    throw new ArgumentException($"Claim {key} is reserved and cannot be set");
                }
            }

            var customClaimsString = SerializeClaims(customClaims);
            var byteCount = Encoding.Unicode.GetByteCount(customClaimsString);
            if (byteCount > 1000)
            {
                throw new ArgumentException($"Claims have to be not greater than 1000 bytes when serialized");
            }
        }

        private static string SerializeClaims(IReadOnlyDictionary<string, object> claims)
        {
            return NewtonsoftJsonSerializer.Instance.Serialize(claims);
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
