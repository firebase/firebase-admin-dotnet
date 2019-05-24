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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account.
    /// </summary>
    public sealed class ExportedUserRecord
    {
        /// <summary>
        /// Gets or sets the user's ID.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user's email address is verified or not.
        /// </summary>
        public bool EmailVerified { get; set; }

        /// <summary>
        /// Gets or sets the user's display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the URL for the user's photo.
        /// </summary>
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is disabled or not.
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Gets or sets the timestamp representing the time that the user account was created.
        /// </summary>
        public long CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp representing the last time that the user has logged in.
        /// </summary>
        public long LastLoginAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp representing the time that the user account was first valid.
        /// </summary>
        public long ValidSince { get; set; }

        /// <summary>
        /// Gets or sets the user's custom claims.
        /// </summary>
        public string CustomClaims { get; set; }

        internal static ExportedUserRecord CreateFrom(GetAccountInfoResponse.User userRecord)
        {
            return new ExportedUserRecord
            {
                UserId = userRecord.UserId,
                Email = userRecord.Email,
                PhoneNumber = userRecord.PhoneNumber,
                EmailVerified = userRecord.EmailVerified,
                DisplayName = userRecord.DisplayName,
                PhotoUrl = userRecord.PhotoUrl,
                Disabled = userRecord.Disabled,
                CreatedAt = userRecord.CreatedAt,
                LastLoginAt = userRecord.LastLoginAt,
                ValidSince = userRecord.ValidSince,
                CustomClaims = userRecord.CustomClaims,
            };
        }
    }
}