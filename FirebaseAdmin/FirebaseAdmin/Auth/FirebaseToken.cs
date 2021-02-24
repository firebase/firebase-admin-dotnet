// Copyright 2018, Google Inc. All rights reserved.
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

using System.Collections.Generic;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a valid, decoded Firebase ID token. It can be used to get the <c>Uid</c> and
    /// other claims available in the token.
    /// </summary>
    public sealed class FirebaseToken
    {
        internal FirebaseToken(Args args)
        {
            this.Issuer = args.Issuer;
            this.Subject = args.Subject;
            this.Audience = args.Audience;
            this.ExpirationTimeSeconds = args.ExpirationTimeSeconds;
            this.IssuedAtTimeSeconds = args.IssuedAtTimeSeconds;
            this.Uid = args.Subject;
            this.Claims = args.Claims;
            this.TenantId = args.Firebase?.Tenant;
        }

        /// <summary>
        /// Gets the issuer claim that identifies the principal that issued the JWT.
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Gets the subject claim identifying the principal that is the subject of the JWT.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the audience claim that identifies the audience that the JWT is intended for.
        /// </summary>
        public string Audience { get; }

        /// <summary>
        /// Gets the expiration time claim that identifies the expiration time (in seconds)
        /// on or after which the token MUST NOT be accepted for processing.
        /// </summary>
        public long ExpirationTimeSeconds { get; }

        /// <summary>
        /// Gets the issued at claim that identifies the time (in seconds) at which the JWT was
        /// issued.
        /// </summary>
        public long IssuedAtTimeSeconds { get; }

        /// <summary>
        /// Gets the User ID of the user to which this ID token belongs. This is same as
        /// <see cref="Subject"/>.
        /// </summary>
        public string Uid { get; }

        /// <summary>
        /// Gets the ID of the tenant the user belongs to, if available. Returns null if the ID
        /// token is not scoped to a tenant.
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// Gets all other claims present in the JWT as a readonly dictionary. This can be used to
        /// access custom claims of the token.
        /// </summary>
        public IReadOnlyDictionary<string, object> Claims { get; }

        internal sealed class Args
        {
            [JsonProperty("iss")]
            internal string Issuer { get; set; }

            [JsonProperty("sub")]
            internal string Subject { get; set; }

            [JsonProperty("aud")]
            internal string Audience { get; set; }

            [JsonProperty("exp")]
            internal long ExpirationTimeSeconds { get; set; }

            [JsonProperty("iat")]
            internal long IssuedAtTimeSeconds { get; set; }

            [JsonProperty("firebase")]
            internal FirebaseInfo Firebase { get; set; }

            [JsonIgnore]
            internal IReadOnlyDictionary<string, object> Claims { get; set; }
        }

        internal sealed class FirebaseInfo
        {
            [JsonProperty("tenant")]
            internal string Tenant { get; set; }
        }
    }
}
