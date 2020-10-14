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

using RSAKey = System.Security.Cryptography.RSA;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// Represents an RSA public key, which can be used to verify signatures.
    /// </summary>
    internal sealed class PublicKey
    {
        public PublicKey(string keyId, RSAKey rsa)
        {
            this.Id = keyId;
            this.RSA = rsa;
        }

        /// <summary>
        /// Gets the unique identifier of this key.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the <see cref="RSAKey"/> instance containing the contents of
        /// the public key.
        /// </summary>
        public RSAKey RSA { get; }
    }
}
