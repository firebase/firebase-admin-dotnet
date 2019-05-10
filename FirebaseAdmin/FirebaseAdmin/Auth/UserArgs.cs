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
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    internal sealed class UserArgs
    {
        private IReadOnlyDictionary<string, object> customClaims;
        private bool claimsTouched;

        public string Uid { get; set; }

        internal IReadOnlyDictionary<string, object> CustomClaims
        {
            get => this.customClaims;
            set
            {
                this.customClaims = value;
                this.claimsTouched = true;
            }
        }

        internal UpdateUserRequest ToUpdateUserRequest()
        {
            return new UpdateUserRequest(this);
        }

        private static string CheckUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("Uid must not be null or empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("Uid must not be longer than 128 characters");
            }

            return uid;
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

        internal sealed class UpdateUserRequest
        {
            internal UpdateUserRequest(UserArgs args)
            {
                this.Uid = CheckUid(args.Uid);
                if (args.claimsTouched)
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
