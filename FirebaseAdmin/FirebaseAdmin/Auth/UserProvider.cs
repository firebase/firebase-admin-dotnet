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
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a user identity provider that can be associated with a Firebase user.
    /// </summary>
    public sealed class UserProvider
    {
        private string uid;

        private string providerId;

        /// <summary>
        /// Gets or sets the user's unique ID assigned by the identity provider. This field is required.
        /// </summary>
        [JsonProperty("rawId")]
        public string Uid
        {
            get
            {
                if (string.IsNullOrEmpty(this.uid))
                {
                    throw new ArgumentException("Uid must not be null or empty");
                }

                return this.uid;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("Uid must not be null or empty");
                }

                this.uid = value;
            }
        }

        /// <summary>
        /// Gets or sets the user's display name.
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        [JsonProperty("email")]
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the photo URL of the user.
        /// </summary>
        [JsonProperty("photoUrl")]
        public string PhotoUrl { get; set; }

        /// <summary>
        /// Gets or sets the ID of the identity provider. This can be a short domain name (e.g. google.com) or
        /// the identifier of an OpenID identity provider. This field is required.
        /// </summary>
        [JsonProperty("providerId")]
        public string ProviderId
        {
            get
            {
                if (string.IsNullOrEmpty(this.providerId))
                {
                    throw new ArgumentException("ProviderId must not be null or empty");
                }

                return this.providerId;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentException("ProviderId must not be null or empty");
                }

                this.providerId = value;
            }
        }
    }
}
