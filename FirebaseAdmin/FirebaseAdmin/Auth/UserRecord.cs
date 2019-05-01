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
        private const string PROVIDERID = "firebase";

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
        internal UserRecord(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentNullException(nameof(uid));
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
        /// Gets a value indicating whether the user's email address is verified or not.
        /// </summary>
        public bool EmailVerified => this.emailVerified;

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
        /// Returns the user's unique ID assigned by the identity provider.
        /// </summary>
        /// <returns>a user ID string.</returns>
        string IUserInfo.GetUid() => this.uid;

        /// <summary>
        /// Returns the user's display name, if available.
        /// </summary>
        /// <returns>a display name string or null.</returns>
        string IUserInfo.GetDisplayName() => this.displayName;

        /// <summary>
        /// Returns the user's email address, if available.
        /// </summary>
        /// <returns>an email address string or null.</returns>
        string IUserInfo.GetEmail() => this.email;

        /// <summary>
        /// Gets the user's phone number.
        /// </summary>
        /// <returns>a phone number string or null.</returns>
        string IUserInfo.GetPhoneNumber() => this.phoneNumber;

        /// <summary>
        /// Returns the user's photo URL, if available.
        /// </summary>
        /// <returns>a URL string or null.</returns>
        string IUserInfo.GetPhotoUrl() => this.photoUrl;

        /// <summary>
        /// Returns the ID of the identity provider. This can be a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider.
        /// </summary>
        /// <returns>an ID string that uniquely identifies the identity provider.</returns>
        string IUserInfo.GetProviderId() => UserRecord.PROVIDERID;

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

        private static ReadOnlyDictionary<string, object> ParseCustomClaims(string customClaims)
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
