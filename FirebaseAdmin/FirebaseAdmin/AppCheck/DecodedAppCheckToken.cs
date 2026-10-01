// Copyright 2026, Google Inc. All rights reserved.
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
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Represents the decoded claims of a verified Firebase App Check token.
    /// </summary>
    public sealed class DecodedAppCheckToken
    {
        internal DecodedAppCheckToken(Args args)
        {
            this.Issuer = args.Issuer;
            this.Subject = args.Subject;
            this.Audience = args.Audience?.ToImmutableList() ?? ImmutableList<string>.Empty;
            this.ExpirationTime = DateTimeOffset.FromUnixTimeSeconds(args.ExpirationTimeSeconds);
            this.IssuedAtTime = DateTimeOffset.FromUnixTimeSeconds(args.IssuedAtTimeSeconds);
            this.Jti = args.Jti;
            this.Provider = args.Provider;
            this.Claims = args.Claims ?? ImmutableDictionary<string, object>.Empty;
        }

        /// <summary>
        /// Gets the issuer claim ('iss') identifying the principal that issued the JWT.
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// Gets the subject claim ('sub') of the token.
        /// </summary>
        public string Subject { get; }

        /// <summary>
        /// Gets the audience list ('aud') for which this token is intended.
        /// </summary>
        public IReadOnlyList<string> Audience { get; }

        /// <summary>
        /// Gets the expiration time ('exp') of the token.
        /// </summary>
        public DateTimeOffset ExpirationTime { get; }

        /// <summary>
        /// Gets the time ('iat') at which the token was issued.
        /// </summary>
        public DateTimeOffset IssuedAtTime { get; }

        /// <summary>
        /// Gets the JWT ID ('jti') of the token, or <c>null</c> if not present.
        /// </summary>
        public string Jti { get; }

        /// <summary>
        /// Gets the attestation provider ('provider') used to issue the token, or <c>null</c> if not present.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// Gets the complete dictionary of all claims in the token.
        /// </summary>
        public IReadOnlyDictionary<string, object> Claims { get; }

        internal sealed class Args
        {
            [JsonProperty("iss")]
            internal string Issuer { get; set; }

            [JsonProperty("sub")]
            internal string Subject { get; set; }

            [JsonProperty("aud")]
            [JsonConverter(typeof(AudienceConverter))]
            internal List<string> Audience { get; set; }

            [JsonProperty("exp")]
            internal long ExpirationTimeSeconds { get; set; }

            [JsonProperty("iat")]
            internal long IssuedAtTimeSeconds { get; set; }

            [JsonProperty("jti")]
            internal string Jti { get; set; }

            [JsonProperty("provider")]
            internal string Provider { get; set; }

            [JsonIgnore]
            internal IReadOnlyDictionary<string, object> Claims { get; set; }
        }

        private sealed class AudienceConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(List<string>);
            }

            public override object ReadJson(
                JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null)
                {
                    return null;
                }

                if (reader.TokenType == JsonToken.String)
                {
                    return new List<string> { (string)reader.Value };
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    return serializer.Deserialize<List<string>>(reader);
                }

                throw new JsonSerializationException(
                    $"Unexpected token {reader.TokenType} when parsing audience.");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                serializer.Serialize(writer, value);
            }
        }
    }
}
