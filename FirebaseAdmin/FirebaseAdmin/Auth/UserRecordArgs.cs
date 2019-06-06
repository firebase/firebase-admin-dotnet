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
    /// A specification for creating or updating user accounts.
    /// </summary>
    public sealed class UserRecordArgs
    {
        private static readonly object Unspecified = new object();

        private object customClaims = Unspecified;
        private bool? disabled = null;
        private bool? emailVerified = null;

        /// <summary>
        /// Gets or sets the user ID of the account.
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the phone number of the user.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the display name of the user account.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user email address has been verified or not.
        /// </summary>
        public bool EmailVerified
        {
            get => this.emailVerified ?? false;
            set => this.emailVerified = value;
        }

        /// <summary>
        /// Gets or sets the photo URL of the user.
        /// </summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user account should be disabled by default or not.
        /// </summary>
        public bool Disabled
        {
            get => this.disabled ?? false;
            set => this.disabled = value;
        }

        /// <summary>
        /// Gets or sets the password of the user.
        /// </summary>
        public string Password { get; set; }

        internal IReadOnlyDictionary<string, object> CustomClaims
        {
            get => this.GetIfSpecified<Dictionary<string, object>>(this.customClaims);
            set => this.customClaims = value;
        }

        internal CreateUserRequest ToCreateUserRequest()
        {
            return new CreateUserRequest(this);
        }

        internal UpdateUserRequest ToUpdateUserRequest()
        {
            return new UpdateUserRequest(this);
        }

        private static string CheckUid(string uid, bool required = false)
        {
            if (uid == null)
            {
                if (required)
                {
                    throw new ArgumentException("Uid must not be null");
                }
            }
            else if (uid == string.Empty)
            {
                throw new ArgumentException("Uid must not be empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("Uid must not be longer than 128 characters");
            }

            return uid;
        }

        private static string CheckEmail(string email)
        {
            if (email != null)
            {
                if (email == string.Empty)
                {
                    throw new ArgumentException("Email must not be empty");
                }
                else if (!Regex.IsMatch(email, @"^[^@]+@[^@]+$"))
                {
                    throw new ArgumentException($"Invalid email address: {email}");
                }
            }

            return email;
        }

        private static string CheckPhoneNumber(string phoneNumber)
        {
            if (phoneNumber != null)
            {
                if (phoneNumber == string.Empty)
                {
                    throw new ArgumentException("Phone number must not be empty.");
                }
                else if (!phoneNumber.StartsWith("+"))
                {
                    throw new ArgumentException(
                        "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.");
                }
            }

            return phoneNumber;
        }

        private static string CheckPhotoUrl(string photoUrl)
        {
            if (photoUrl != null)
            {
                if (photoUrl == string.Empty)
                {
                    throw new ArgumentException("Photo URL must not be empty");
                }
                else if (!Uri.IsWellFormedUriString(photoUrl, UriKind.Absolute))
                {
                    throw new ArgumentException($"Malformed photo URL string: {photoUrl}");
                }
            }

            return photoUrl;
        }

        private static string CheckPassword(string password)
        {
            if (password != null)
            {
                if (password.Length < 6)
                {
                    throw new ArgumentException("Password must be at least 6 characters long.");
                }
            }

            return password;
        }

        private static string CheckCustomClaims(IReadOnlyDictionary<string, object> claims)
        {
            if (claims == null || claims.Count == 0)
            {
                return "{}";
            }

            foreach (var key in claims.Keys)
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

            var customClaimsString = NewtonsoftJsonSerializer.Instance.Serialize(claims);
            var byteCount = Encoding.UTF8.GetByteCount(customClaimsString);
            if (byteCount > 1000)
            {
                throw new ArgumentException($"Claims must not be longer than 1000 bytes when serialized");
            }

            return customClaimsString;
        }

        private T GetIfSpecified<T>(object value)
        {
            if (value == Unspecified)
            {
                return default(T);
            }

            return (T)value;
        }

        internal sealed class CreateUserRequest
        {
            internal CreateUserRequest(UserRecordArgs args)
            {
                this.Disabled = args.disabled;
                this.DisplayName = args.DisplayName;
                this.Email = CheckEmail(args.Email);
                this.EmailVerified = args.emailVerified;
                this.Password = CheckPassword(args.Password);
                this.PhoneNumber = CheckPhoneNumber(args.PhoneNumber);
                this.PhotoUrl = CheckPhotoUrl(args.PhotoUrl);
                this.Uid = CheckUid(args.Uid);
            }

            [JsonProperty("disabled")]
            public bool? Disabled { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("emailVerified")]
            public bool? EmailVerified { get; set; }

            [JsonProperty("password")]
            public string Password { get; set; }

            [JsonProperty("phoneNumber")]
            public string PhoneNumber { get; set; }

            [JsonProperty("photoUrl")]
            public string PhotoUrl { get; set; }

            [JsonProperty("localId")]
            public string Uid { get; set; }
        }

        internal sealed class UpdateUserRequest
        {
            internal UpdateUserRequest(UserRecordArgs args)
            {
                this.Uid = CheckUid(args.Uid, required: true);
                if (args.customClaims != Unspecified)
                {
                    this.CustomClaims = CheckCustomClaims(args.CustomClaims);
                }
            }

            [JsonProperty("localId")]
            public string Uid { get; set; }

            [JsonProperty("customAttributes")]
            public string CustomClaims { get; set; }
        }
    }
}
