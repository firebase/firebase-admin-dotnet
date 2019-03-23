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
using System.Text;
using System.Text.RegularExpressions;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account. Instances
    /// of this class are immutable and thread safe.
    /// </summary>
    internal class UserRecord
    {
        private string uid;
        private string email;
        private string phoneNumber;
        private string displayName;
        private string photoUrl;
        private long tokensValidAfterTimestamp;
        private IReadOnlyDictionary<string, object> customClaims;

        public UserRecord(string uid)
        {
            this.Uid = uid;
        }

        /// <summary>
        /// Gets the user ID of this user.
        /// </summary>
        [JsonProperty("localId")]
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
        /// Gets the email of this user.
        /// </summary>
        [JsonProperty("email")]
        public string Email
        {
            get => this.email;
            private set
            {
                CheckEmail(value);
                this.email = value;
            }
        }

        /// <summary>
        /// Gets the phone number of this user.
        /// </summary>
        [JsonProperty("phoneNumber")]
        public string PhoneNumber
        {
            get => this.phoneNumber;
            private set
            {
                CheckPhoneNumber(value);
                this.phoneNumber = value;
            }
        }

        /// <summary>
        /// Gets the display name of this user.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName
        {
            get => this.displayName;
            private set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("display name cannot be null or empty");
                }

                this.displayName = value;
            }
        }

        /// <summary>
        /// Gets the photo url of this user.
        /// </summary>
        [JsonProperty("photoUrl")]
        public string PhotoUrl
        {
            get => this.photoUrl;
            private set
            {
                CheckPhotoUrl(value);
                this.photoUrl = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the user's email has been verified.
        /// </summary>
        [JsonProperty("emailVerified")]
        public bool IsEmailVerified { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the user's account is disabled.
        /// </summary>
        [JsonProperty("disabled")]
        public bool IsDisabled { get; private set; }

        /// <summary>
        /// Gets a new password hash for the user.
        /// </summary>
        public string PasswordHash { get; private set; }

        /// <summary>
        /// Gets a new password salt for the user.
        /// </summary>
        public string PasswordSalt { get; private set; }

        /// <summary>
        /// Gets the valid tokens since the epoch in milliseconds.
        /// </summary>
        [JsonProperty("validSince")]
        public long TokensValidAfterTimestamp
        {
            get => this.tokensValidAfterTimestamp * 1000;
        }

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
        /// Checks if the given user email is valid.
        /// </summary>
        /// <param name="email">The user email. Must not be null and
        /// must have valid email address formatting.</param>
        public static void CheckEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("email must not be null or empty");
            }
            else if (!Regex.IsMatch(email, "^[^@]+@[^@]+$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException("email address is invalid");
            }
        }

        /// <summary>
        /// Checks if the given user phone number is valid.
        /// </summary>
        /// <param name="phoneNumber">The user phone number. Must not be null and
        /// must have valid E.164 formatting.</param>
        public static void CheckPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                throw new ArgumentException("phone number must not be null or empty");
            }
            else if (!phoneNumber.StartsWith("+"))
            {
                throw new ArgumentException("phone number is invalid. Must start with a '+' sign");
            }
        }

        /// <summary>
        /// Checks if the given user photo url is valid.
        /// </summary>
        /// <param name="photoUrl">The user phone number. Must not be null and
        /// must have valid E.164 formatting.</param>
        public static void CheckPhotoUrl(string photoUrl)
        {
            if (string.IsNullOrEmpty(photoUrl))
            {
                throw new ArgumentException("photo url must not be null or empty");
            }
            else if (!Uri.IsWellFormedUriString(photoUrl, UriKind.RelativeOrAbsolute))
            {
                throw new ArgumentException("photo url is malformed");
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
    }
}
