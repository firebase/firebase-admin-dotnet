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
        internal FirebaseToken(FirebaseTokenArgs args)
        {
            Issuer = args.Issuer;
            Subject = args.Subject;
            Audience = args.Audience;
            ExpirationTimeSeconds = args.ExpirationTimeSeconds;
            IssuedAtTimeSeconds = args.IssuedAtTimeSeconds;
            Uid = args.Subject;
            Claims = args.Claims;
        }

        /// <summary>
        /// The issuer claim that identifies the principal that issued the JWT.
        /// </summary>
        public string Issuer { get; private set; }

        /// <summary>
        /// The subject claim identifying the principal that is the subject of the JWT.
        /// </summary>
        public string Subject { get; private set; }

        /// <summary>
        /// The audience claim that identifies the audience that the JWT is intended for.
        /// </summary>
        public string Audience { get; private set; }

        /// <summary>
        /// The expiration time claim that identifies the expiration time (in seconds)
        /// on or after which the token MUST NOT be accepted for processing.
        /// </summary>
        public long ExpirationTimeSeconds { get; private set; }

        /// <summary>
        /// The issued at claim that identifies the time (in seconds) at which the JWT was issued.
        /// </summary>
        public long IssuedAtTimeSeconds { get; private set; }

        /// <summary>
        /// User ID of the user to which this ID token belongs. This is same as <c>Subject</c>.
        /// </summary>
        public string Uid { get; private set; }

        /// <summary>
        /// A read-only dictionary of all other claims present in the JWT. This can be used to
        /// access custom claims of the token.
        /// </summary>
        public IReadOnlyDictionary<string, object> Claims { get; private set; }
    }

    internal sealed class FirebaseTokenArgs
    {
        [JsonProperty("iss")]
        public string Issuer { get; set; }

        [JsonProperty("sub")]
        public string Subject { get; set; }

        [JsonProperty("aud")]
        public string Audience { get; set; }

        [JsonProperty("exp")]
        public long ExpirationTimeSeconds { get; set; }

        [JsonProperty("iat")]
        public long IssuedAtTimeSeconds { get; set; }

        [JsonIgnore]
        public IReadOnlyDictionary<string, object> Claims { get; set; }
    }
}
