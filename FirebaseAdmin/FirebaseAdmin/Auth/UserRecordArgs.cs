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
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A specification for creating or updating user accounts.
    /// </summary>
    public sealed class UserRecordArgs
    {
        private Optional<string> displayName;
        private Optional<string> photoUrl;
        private Optional<string> phoneNumber;
        private Optional<IReadOnlyDictionary<string, object>> customClaims;
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
        public string PhoneNumber
        {
            get => this.phoneNumber?.Value;
            set => this.phoneNumber = this.Wrap(value);
        }

        /// <summary>
        /// Gets or sets the display name of the user account.
        /// </summary>
        public string DisplayName
        {
            get => this.displayName?.Value;
            set => this.displayName = this.Wrap(value);
        }

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
        public string PhotoUrl
        {
            get => this.photoUrl?.Value;
            set => this.photoUrl = this.Wrap(value);
        }

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

        internal long? ValidSince { get; set; }

        internal IReadOnlyDictionary<string, object> CustomClaims
        {
            get => this.customClaims?.Value;
            set => this.customClaims = this.Wrap(value);
        }

        internal static string CheckUid(string uid, bool required = false)
        {
            if (uid == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(uid));
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

        internal static string CheckEmail(string email, bool required = false)
        {
            if (email == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(email));
                }
            }
            else if (email == string.Empty)
            {
                throw new ArgumentException("Email must not be empty");
            }
            else if (!Regex.IsMatch(email, @"^[^@]+@[^@]+$"))
            {
                throw new ArgumentException($"Invalid email address: {email}");
            }

            return email;
        }

        internal static string CheckPhoneNumber(string phoneNumber, bool required = false)
        {
            if (phoneNumber == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(phoneNumber));
                }
            }
            else if (phoneNumber == string.Empty)
            {
                throw new ArgumentException("Phone number must not be empty.");
            }
            else if (!phoneNumber.StartsWith("+"))
            {
                throw new ArgumentException(
                    "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.");
            }

            return phoneNumber;
        }

        // TODO(rsgowman): Once we upgrade our floor from .NET4.5 to .NET4.7, we can return a tuple
        // here, making this more like the other CheckX methods. i.e.:
        //     internal static (string, string) CheckProvider(...)
        internal static void CheckProvider(string providerId, string providerUid, bool required = false)
        {
            if (providerId == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(providerId));
                }
            }
            else if (providerId == string.Empty)
            {
                throw new ArgumentException(nameof(providerId) + " must not be empty");
            }

            if (providerUid == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(providerUid));
                }
            }
            else if (providerUid == string.Empty)
            {
                throw new ArgumentException(nameof(providerUid) + " must not be empty");
            }
        }

        internal static string CheckPhotoUrl(string photoUrl)
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

        internal static string CheckCustomClaims(IReadOnlyDictionary<string, object> claims)
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

        internal CreateUserRequest ToCreateUserRequest()
        {
            return new CreateUserRequest(this);
        }

        internal UpdateUserRequest ToUpdateUserRequest()
        {
            return new UpdateUserRequest(this);
        }

        private static string CheckPassword(string password)
        {
            if (password != null && password.Length < 6)
            {
                throw new ArgumentException("Password must be at least 6 characters long.");
            }

            return password;
        }

        private Optional<T> Wrap<T>(T value)
        {
            return new Optional<T>(value);
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
                if (args.customClaims != null)
                {
                    this.CustomClaims = CheckCustomClaims(args.customClaims.Value);
                }

                this.Disabled = args.disabled;
                this.Email = CheckEmail(args.Email);
                this.EmailVerified = args.emailVerified;
                this.Password = CheckPassword(args.Password);
                this.ValidSince = args.ValidSince;

                if (args.displayName != null)
                {
                    var displayName = args.displayName.Value;
                    if (displayName == null)
                    {
                        this.AddDeleteAttribute("DISPLAY_NAME");
                    }
                    else
                    {
                        this.DisplayName = displayName;
                    }
                }

                if (args.photoUrl != null)
                {
                    var photoUrl = args.photoUrl.Value;
                    if (photoUrl == null)
                    {
                        this.AddDeleteAttribute("PHOTO_URL");
                    }
                    else
                    {
                        this.PhotoUrl = CheckPhotoUrl(photoUrl);
                    }
                }

                if (args.phoneNumber != null)
                {
                    var phoneNumber = args.phoneNumber.Value;
                    if (phoneNumber == null)
                    {
                        this.AddDeleteProvider("phone");
                    }
                    else
                    {
                        this.PhoneNumber = CheckPhoneNumber(phoneNumber);
                    }
                }
            }

            [JsonProperty("customAttributes")]
            public string CustomClaims { get; set; }

            [JsonProperty("deleteAttribute")]
            public IList<string> DeleteAttribute { get; set; }

            [JsonProperty("deleteProvider")]
            public IList<string> DeleteProvider { get; set; }

            [JsonProperty("disableUser")]
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

            [JsonProperty("validSince")]
            public long? ValidSince { get; set; }

            private void AddDeleteAttribute(string attribute)
            {
                if (this.DeleteAttribute == null)
                {
                    this.DeleteAttribute = new List<string>();
                }

                this.DeleteAttribute.Add(attribute);
            }

            private void AddDeleteProvider(string provider)
            {
                if (this.DeleteProvider == null)
                {
                    this.DeleteProvider = new List<string>();
                }

                this.DeleteProvider.Add(provider);
            }
        }

        /// <summary>
        /// Wraps a nullable value. Used to differentiate between parameters that have not been set, and
        /// the parameters that have been explicitly set to null.
        /// </summary>
        private class Optional<T>
        {
            internal Optional(T value)
            {
                this.Value = value;
            }

            internal T Value { get; private set; }
        }
    }
}
