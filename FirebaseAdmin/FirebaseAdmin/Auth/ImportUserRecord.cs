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
using System.Collections.Immutable;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a user account to be imported to Firebase Auth via the
    /// FirebaseAuth#importUsers(List, UserImportOptions) API. Must contain at least a
    /// uid string.
    /// </summary>
    public sealed class ImportUserRecord
    {
        private readonly IDictionary<string, object> properties;

        private ImportUserRecord(IDictionary<string, object> properties)
        {
            this.properties = ImmutableDictionary.CreateRange(properties);
        }

        /// <summary>
        /// Creates a new ImportUserRecord.Builder.
        /// </summary>
        /// <returns>A ImportUserRecord.Builder instance.</returns>
        public static Builder GetBuilder()
        {
            return new Builder();
        }

        internal bool HasPassword()
        {
            return this.properties.ContainsKey("passwordHash");
        }

        /// <summary>
        /// An inner class for building a user record.
        /// </summary>
        public class Builder
        {
            private readonly List<IUserInfo> userProviders = new List<IUserInfo>();
            private readonly IDictionary<string, object> customClaims = new Dictionary<string, object>();

            private string uid;
            private string email;
            private bool? emailVerified;
            private string displayName;
            private string phoneNumber;
            private string photoUrl;
            private bool? disabled;
            private UserMetadata userMetadata;
            private string passwordHash;
            private string passwordSalt;

            /// <summary>
            /// Sets a user ID for the user.
            /// </summary>
            /// <param name="uid">a non-null, non-empty user ID that uniquely identifies the user. The user ID
            /// must not be longer than 128 characters.</param>
            /// <returns>This builder.</returns>
            public Builder Uid(string uid)
            {
                this.uid = uid;
                return this;
            }

            /// <summary>
            /// Sets an email address for the user.
            /// </summary>
            /// <param name="email">a non-null, non-empty email address string.</param>
            /// <returns>This builder.</returns>
            public Builder Email(string email)
            {
                this.email = email;
                return this;
            }

            /// <summary>
            /// Sets whether the user email address has been verified or not.
            /// </summary>
            /// <param name="emailVerified">a boolean indicating the email verification status.</param>
            /// <returns>This builder.</returns>
            public Builder EmailVerified(bool emailVerified)
            {
                this.emailVerified = emailVerified;
                return this;
            }

            /// <summary>
            /// Sets the display name for the user.
            /// </summary>
            /// <param name="displayName">a non-null, non-empty display name string.</param>
            /// <returns>This builder.</returns>
            public Builder DisplayName(string displayName)
            {
                this.displayName = displayName;
                return this;
            }

            /// <summary>
            /// Sets the phone number associated with this user.
            /// </summary>
            /// <param name="phoneNumber">a valid phone number string.</param>
            /// <returns>This builder.</returns>
            public Builder PhoneNumber(string phoneNumber)
            {
                this.phoneNumber = phoneNumber;
                return this;
            }

            /// <summary>
            /// Sets the photo URL for the user.
            /// </summary>
            /// <param name="photoUrl">a non-null, non-empty URL string.</param>
            /// <returns>This builder.</returns>
            public Builder PhotoUrl(string photoUrl)
            {
                this.photoUrl = photoUrl;
                return this;
            }

            /// <summary>
            /// Sets whether the user account should be disabled by default or not.
            /// </summary>
            /// <param name="disabled">a boolean indicating whether the account should be disabled.</param>
            /// <returns>This builder.</returns>
            public Builder Disabled(bool disabled)
            {
                this.disabled = disabled;
                return this;
            }

            /// <summary>
            /// Sets additional metadata about the user.
            /// </summary>
            /// <param name="userMetadata">A UserMetadata instance.</param>
            /// <returns>This builder.</returns>
            public Builder UserMetadata(UserMetadata userMetadata)
            {
                this.userMetadata = userMetadata;
                return this;
            }

            /// <summary>
            /// Sets a string representing the user's hashed password. If at least one user account
            /// carries a password hash, a UserImportHash
            /// must be specified when calling the
            /// FirebaseAuth#importUsersAsync(List, UserImportOptions) method.
            /// </summary>
            /// <param name="passwordHash">A string containing the password hash.</param>
            /// <returns>This builder.</returns>
            public Builder PasswordHash(string passwordHash)
            {
                this.passwordHash = passwordHash;
                return this;
            }

            /// <summary>
            /// Sets a string representing the user's password salt.
            /// </summary>
            /// <param name="passwordSalt">A string containing the password salt.</param>
            /// <returns>This builder.</returns>
            public Builder PasswordSalt(string passwordSalt)
            {
                this.passwordSalt = passwordSalt;
                return this;
            }

            /// <summary>
            /// Adds a user provider to be associated with this user.
            /// </summary>
            /// <param name="provider">A data provider associated with this user.</param>
            /// <returns>This builder.</returns>
            public Builder UserProvider(IUserInfo provider)
            {
                this.userProviders.Add(provider);
                return this;
            }

            /// <summary>
            ///  Associates all user provider's in the given list with this user.
            /// </summary>
            /// <param name="providers">A list of this user's data providers.</param>
            /// <returns>This builder.</returns>
            public Builder AllUserProvider(List<IUserInfo> providers)
            {
                this.userProviders.AddRange(providers);
                return this;
            }

            /// <summary>
            /// Sets the specified custom claim on this user account.
            /// </summary>
            /// <param name="key">Name of the claim.</param>
            /// <param name="value">Value of the claim.</param>
            /// <returns>This builder.</returns>
            public Builder CustomClaim(string key, object value)
            {
                this.customClaims.Add(key, value);
                return this;
            }

            /// <summary>
            /// Sets the custom claims associated with this user.
            /// </summary>
            /// <param name="customClaims">A dictionary of custom claims.</param>
            /// <returns>This builder.</returns>
            public Builder AllCustomClaims(IReadOnlyDictionary<string, object> customClaims)
            {
                foreach (var claim in customClaims)
                {
                    this.customClaims.Add(claim.Key, claim.Value);
                }

                return this;
            }

            /// <summary>
            /// Builds a new ImportUserRecord object.
            /// </summary>
            /// <returns>A non-null ImportUserRecord object.</returns>
            public ImportUserRecord Build()
            {
                IDictionary<string, object> properties = new Dictionary<string, object>();

                // perhaps use UserRecordArgs property checks here?
                UserRecordArgs.CheckUid(this.uid);
                properties.Add("localId", this.uid);

                UserRecordArgs.CheckEmail(this.email);
                properties.Add("email", this.email);

                UserRecordArgs.CheckPhotoUrl(this.photoUrl);
                properties.Add("photoUrl", this.photoUrl);

                UserRecordArgs.CheckPhoneNumber(this.phoneNumber);
                properties.Add("phoneNumber", this.phoneNumber);

                if (!string.IsNullOrEmpty(this.displayName))
                {
                    properties.Add("displayName", this.displayName);
                }

                if (this.userMetadata != null)
                {
                    if (this.userMetadata.CreationTimestamp != null)
                    {
                        properties.Add("createdAt", this.userMetadata.CreationTimestamp);
                    }

                    if (this.userMetadata.LastSignInTimestamp != null)
                    {
                        properties.Add("lastLoginAt", this.userMetadata.LastSignInTimestamp);
                    }
                }

                if (!string.IsNullOrEmpty(this.passwordHash))
                {
                    properties.Add("passwordHash", Convert.ToBase64String(Encoding.UTF8.GetBytes(this.passwordHash)));
                }

                if (!string.IsNullOrEmpty(this.passwordSalt))
                {
                    properties.Add("passwordSalt", Convert.ToBase64String(Encoding.UTF8.GetBytes(this.passwordSalt)));
                }

                if (this.userProviders.Count > 0)
                {
                    properties.Add("providerUserInfo", ImmutableList.CreateRange(this.userProviders));
                }

                if (this.customClaims.Count > 0)
                {
                    var mergedClaims = ImmutableDictionary.CreateRange(this.customClaims);

                    // perhaps use UserRecordArgs' checkCustomClaim here?
                    properties.Add("customAttributes", mergedClaims);
                }

                if (this.emailVerified != null)
                {
                    properties.Add("emailVerified", this.emailVerified);
                }

                if (this.disabled != null)
                {
                    properties.Add("disabled", this.disabled);
                }

                return new ImportUserRecord(properties);
            }
        }
    }
}