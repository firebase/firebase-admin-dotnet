// Copyright 2020, Google Inc. All rights reserved.
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
using System.Linq;
using FirebaseAdmin.Auth.Jwt;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a user account to be imported to Firebase Auth via the
    /// <see cref="AbstractFirebaseAuth.ImportUsersAsync(IEnumerable{ImportUserRecordArgs})"/> API.
    /// Must contain at least a user ID string.
    /// </summary>
    public sealed class ImportUserRecordArgs
    {
        /// <summary>
        /// Key name for custom attributes.
        /// </summary>
        private const string CustomAttributes = "customAttributes";

        /// <summary>
        /// Gets or sets the user ID of the user.
        /// </summary>
        public string Uid { get; set; }

        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets if the email was verified, null signifies that it was not specified.
        /// </summary>
        public bool? EmailVerified { get; set; }

        /// <summary>
        /// Gets or sets the display name of the user.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets phone number of the user.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the photo URL.
        /// </summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the disabled value, null signifies that it was not specified.
        /// </summary>
        public bool? Disabled { get; set; }

        /// <summary>
        /// Gets or sets the user metadata.
        /// </summary>
        public UserMetadata UserMetadata { get; set; }

        /// <summary>
        /// Gets or sets the password hash.
        /// </summary>
        public byte[] PasswordHash { get; set; }

        /// <summary>
        /// Gets or sets the password salt.
        /// </summary>
        public byte[] PasswordSalt { get; set; }

        /// <summary>
        /// Gets or sets the user providers.
        /// </summary>
        public IEnumerable<UserProvider> UserProviders { get; set; }

        /// <summary>
        /// Gets or sets the custom claims.
        /// </summary>
        public IReadOnlyDictionary<string, object> CustomClaims { get; set; }

        internal bool HasPassword()
        {
            return this.PasswordHash != null;
        }

        internal Request ToRequest()
        {
            return new Request(this);
        }

        internal sealed class Request
        {
            internal Request(ImportUserRecordArgs args)
            {
                this.Uid = UserRecordArgs.CheckUid(args.Uid, true);
                this.Email = UserRecordArgs.CheckEmail(args.Email);
                this.PhotoUrl = UserRecordArgs.CheckPhotoUrl(args.PhotoUrl);
                this.PhoneNumber = UserRecordArgs.CheckPhoneNumber(args.PhoneNumber);

                if (!string.IsNullOrEmpty(args.DisplayName))
                {
                    this.DisplayName = args.DisplayName;
                }

                if (args.UserMetadata != null)
                {
                    this.CreatedAt = args.UserMetadata.CreationTimestamp;
                    this.LastLoginAt = args.UserMetadata.LastSignInTimestamp;
                }

                if (args.PasswordHash != null)
                {
                    this.PasswordHash = JwtUtils.UrlSafeBase64Encode(args.PasswordHash);
                }

                if (args.PasswordSalt != null)
                {
                    this.PasswordSalt = JwtUtils.UrlSafeBase64Encode(args.PasswordSalt);
                }

                if (args.UserProviders != null && args.UserProviders.Count() > 0)
                {
                    this.ProviderUserInfo = new List<UserProvider.Request>(
                        args.UserProviders.Select(userProvider => userProvider.ToRequest()));
                }

                if (args.CustomClaims != null && args.CustomClaims.Count > 0)
                {
                    var serialized = UserRecordArgs.CheckCustomClaims(args.CustomClaims);
                    this.CustomAttributes = serialized;
                }

                this.EmailVerified = args.EmailVerified;
                this.Disabled = args.Disabled;
            }

            [JsonProperty("createdAt")]
            public DateTime? CreatedAt { get; set; }

            [JsonProperty("customAttributes")]
            public string CustomAttributes { get; set; }

            [JsonProperty("disabled")]
            public bool? Disabled { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("email")]
            public string Email { get; set; }

            [JsonProperty("emailVerified")]
            public bool? EmailVerified { get; set; }

            [JsonProperty("lastLoginAt")]
            public DateTime? LastLoginAt { get; set; }

            [JsonProperty("passwordHash")]
            public string PasswordHash { get; set; }

            [JsonProperty("salt")]
            public string PasswordSalt { get; set; }

            [JsonProperty("phoneNumber")]
            public string PhoneNumber { get; set; }

            [JsonProperty("photoUrl")]
            public string PhotoUrl { get; set; }

            [JsonProperty("providerUserInfo")]
            public List<UserProvider.Request> ProviderUserInfo { get; set; }

            [JsonProperty("localId")]
            public string Uid { get; set; }
        }
    }
}
