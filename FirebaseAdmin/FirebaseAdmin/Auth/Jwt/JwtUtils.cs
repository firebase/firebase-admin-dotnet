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

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// A collection of utilities for encoding and decoding JWTs.
    /// </summary>
    internal static class JwtUtils
    {
        /// <summary>
        /// Decodes a single JWT segment, and deserializes it into a value of type
        /// <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type of the decoded result.</typeparam>
        /// <returns>Decoded JWT segment as a value of type <typeparamref name="T"/>.</returns>
        /// <param name="value">JWT segment to be decoded.</param>
        public static T Decode<T>(string value)
        {
            var json = Base64Decode(value);
            return NewtonsoftJsonSerializer.Instance.Deserialize<T>(json);
        }

        internal static string Base64Decode(string input)
        {
            var raw = Base64DecodeToBytes(input);
            return Encoding.UTF8.GetString(raw);
        }

        internal static byte[] Base64DecodeToBytes(string input)
        {
            // undo the url safe replacements
            input = input.Replace('-', '+').Replace('_', '/');
            switch (input.Length % 4)
            {
                case 2: input += "=="; break;
                case 3: input += "="; break;
            }

            return Convert.FromBase64String(input);
        }

        internal static async Task<string> CreateSignedJwtAsync(
            object header,
            object payload,
            ISigner signer,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            string encodedHeader = Encode(header);
            string encodedPayload = Encode(payload);
            var assertion = new StringBuilder();
            assertion
                .Append(Encode(header))
                .Append('.')
                .Append(Encode(payload));
            var bytesToSign = Encoding.UTF8.GetBytes(assertion.ToString());
            var signature = await signer.SignDataAsync(bytesToSign, cancellationToken)
                .ConfigureAwait(false);
            assertion.Append('.').Append(UrlSafeBase64Encode(signature));
            return assertion.ToString();
        }

        internal static string UrlSafeBase64Encode(byte[] bytes)
        {
            var base64Value = Convert.ToBase64String(bytes);
            return base64Value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static string Encode(object obj)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(obj);
            return UrlSafeBase64Encode(Encoding.UTF8.GetBytes(json));
        }
    }
}
