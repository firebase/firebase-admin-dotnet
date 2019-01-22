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
            this.Issuer = args.Issuer;
            this.Subject = args.Subject;
            this.Audience = args.Audience;
            this.ExpirationTimeSeconds = args.ExpirationTimeSeconds;
            this.IssuedAtTimeSeconds = args.IssuedAtTimeSeconds;
            this.Uid = args.Subject;
            this.Claims = args.Claims;
        }

        /// <summary>
        /// Gets the issuer claim that identifies the principal that issued the JWT.
        /// </summary>
        public string Issuer { get; private set; }

        /// <summary>
        /// Gets the subject claim identifying the principal that is the subject of the JWT.
        /// </summary>
        public string Subject { get; private set; }

        /// <summary>
        /// Gets the audience claim that identifies the audience that the JWT is intended for.
        /// </summary>
        public string Audience { get; private set; }

        /// <summary>
        /// Gets the expiration time claim that identifies the expiration time (in seconds)
        /// on or after which the token MUST NOT be accepted for processing.
        /// </summary>
        public long ExpirationTimeSeconds { get; private set; }

        /// <summary>
        /// Gets the issued at claim that identifies the time (in seconds) at which the JWT was
        /// issued.
        /// </summary>
        public long IssuedAtTimeSeconds { get; private set; }

        /// <summary>
        /// Gets the User ID of the user to which this ID token belongs. This is same as
        /// <see cref="Subject"/>.
        /// </summary>
        public string Uid { get; private set; }

        /// <summary>
        /// Gets Aall other claims present in the JWT as a readonly dictionary. This can be used to
        /// access custom claims of the token.
        /// </summary>
        public IReadOnlyDictionary<string, object> Claims { get; private set; }
    }
}
