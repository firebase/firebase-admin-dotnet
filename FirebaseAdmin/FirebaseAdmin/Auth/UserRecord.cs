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
    public sealed class UserRecord
    {
        private string uid;
        private string email;
        private string phoneNumber;
        private bool emailVerified;
        private string displayName;
        private string photoUrl;
        private bool disabled;
        private List<ProviderUserInfo> providers;
        private long tokensValidAfterTimestamp;
        private UserMetadata userMetaData;
        private IReadOnlyDictionary<string, object> customClaims;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class with the specified user ID.
        /// </summary>
        /// <param name="uid">The user's ID.</param>
        public UserRecord(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentNullException(nameof(uid));
            }

            this.uid = uid;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserRecord"/> class from an existing instance of the <see cref="Internal.GetAccountInfoResponse.User"/> class.
        /// </summary>
        /// <param name="user">The <see cref="Internal.GetAccountInfoResponse.User"/> instance to copy the user's data from.</param>
        public UserRecord(Internal.GetAccountInfoResponse.User user)
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }
            else if (string.IsNullOrEmpty(user.UserID))
            {
                throw new ArgumentException("UserID must not be null or empty.");
            }

            this.uid = user.UserID;
            this.email = user.Email;
            this.phoneNumber = user.PhoneNumber;
            this.emailVerified = user.EmailVerified;
            this.displayName = user.DisplayName;
            this.photoUrl = user.PhotoUrl;
            this.disabled = user.Disabled;

            if (user.Providers == null || user.Providers.Count == 0)
            {
                this.providers = new List<ProviderUserInfo>();
            }
            else
            {
                var count = user.Providers.Count;
                this.providers = new List<ProviderUserInfo>(count);

                for (int i = 0; i < count; i++)
                {
                    this.providers.Add(new ProviderUserInfo(user.Providers[i]));
                }
            }

            this.tokensValidAfterTimestamp = user.ValidSince * 1000;
            this.userMetaData = new UserMetadata(user.CreatedAt, user.LastLoginAt);
            this.customClaims = this.ParseCustomClaims(user.CustomClaims);
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
        /// Gets the user's email address.
        /// </summary>
        public string Email => this.email;

        /// <summary>
        /// Gets the user's phone number.
        /// </summary>
        public string PhoneNumber => this.phoneNumber;

        /// <summary>
        /// Gets a value indicating whether the user's email address is verified or not.
        /// </summary>
        public bool EmailVerified => this.emailVerified;

        /// <summary>
        /// Gets the user's display name.
        /// </summary>
        public string DisplayName => this.displayName;

        /// <summary>
        /// Gets the user's photo URL.
        /// </summary>
        public string PhotoUrl => this.photoUrl;

        /// <summary>
        /// Gets a value indicating whether the user account is disabled or not.
        /// </summary>
        public bool Disabled => this.disabled;

        /// <summary>
        /// Gets a list of provider data for this user.
        /// </summary>
        public List<ProviderUserInfo> Providers => this.providers;

        /// <summary>
        /// Gets a timestamp representing the date and time that this token will become active.
        /// </summary>
        public long TokensValidAfterTimestamp => this.tokensValidAfterTimestamp;

        /// <summary>
        /// Gets additional user metadata.
        /// </summary>
        public UserMetadata UserMetaData => this.userMetaData;

        /// <summary>
        /// Gets or sets the custom claims set on this user.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, object> CustomClaims
        {
            get => this.customClaims;
            set
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
        public static void CheckUid(string uid)
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
        internal static void CheckCustomClaims(IReadOnlyDictionary<string, object> customClaims)
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

        private ReadOnlyDictionary<string, object> ParseCustomClaims(string customClaims)
        {
            if (string.IsNullOrEmpty(customClaims))
            {
                return new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            }
            else
            {
                var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<Dictionary<string, object>>(customClaims);

                return new ReadOnlyDictionary<string, object>(parsed);
            }
        }
    }
}
