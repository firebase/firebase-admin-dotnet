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

using Google.Apis.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains metadata associated with a Firebase user account. Instances
    /// of this class are immutable and thread safe.
    /// </summary>
    internal class UserRecord
    {
        private string _uid;
        private IReadOnlyDictionary<string, object> _customClaims;

        /// <summary>
        /// The user ID of this user.
        /// </summary>
        [JsonProperty("localId")]
        public string Uid
        {
            get => _uid;
            private set
            {
                CheckUid(value);
                _uid = value;
            }
        }

        /// <summary>
        /// Returns custom claims set on this user.
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<string, object> CustomClaims
        {
            get => _customClaims;
            set
            {
                CheckCustomClaims(value);
                _customClaims = value;
            }
        }

        [JsonProperty("customAttributes")]
        internal string CustomClaimsString => SerializeClaims(CustomClaims);

        public UserRecord(string uid)
        {
            Uid = uid;
        }

        /// <summary>
        /// Checks if the given user ID is valid.
        /// </summary>
        /// <param name="uid">The user ID. Must not be null or longer than 
        /// 128 characters.</param>
        public static void CheckUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("uid must not be null or empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("uid must not be longer than 128 characters");
            }
        }

        /// <summary>
        /// Checks if the given set of custom claims are valid.
        /// </summary>
        /// <param name="customClaims">The custom claims. Claim names must 
        /// not be null or empty and must not be reserved and the serialized
        /// claims have to be less than 1000 bytes.</param>
        public static void CheckCustomClaims(IReadOnlyDictionary<string, object> customClaims)
        {
            if (customClaims == null)
            {
                return;
            }

            foreach (var key in customClaims.Keys)
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

            var customClaimsString = SerializeClaims(customClaims);
            var byteCount = Encoding.Unicode.GetByteCount(customClaimsString);
            if (byteCount > 1000)
            {
                throw new ArgumentException($"Claims have to be not greater than 1000 bytes when serialized");
            }
        }

        private static string SerializeClaims(IReadOnlyDictionary<string, object> claims)
        {
            return NewtonsoftJsonSerializer.Instance.Serialize(claims);
        }
    }
}
