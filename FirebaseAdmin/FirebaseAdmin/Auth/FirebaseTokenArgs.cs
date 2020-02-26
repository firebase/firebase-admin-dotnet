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

        [JsonProperty("firebase")]
        internal FirebaseClaims Firebase { get; set; }

        internal sealed class FirebaseClaims
        {
            [JsonProperty("tenant")]
            public string TenantId { get; set; }
        }
    }
}
