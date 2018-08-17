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

#if NETSTANDARD1_5 || NETSTANDARD2_0
using RsaKey = System.Security.Cryptography.RSA;
#elif NET45
using RsaKey = System.Security.Cryptography.RSACryptoServiceProvider;
#else
#error Unsupported target
#endif  

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents an RSA public key, which can be used to verify signatures.
    /// </summary>
    internal sealed class PublicKey
    {
        /// <summary>
        /// The unique identifier of this key.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// A <see cref="System.Security.Cryptography.RSA"/> instance containing the contents of
        /// the public key.
        /// </summary>
        public RsaKey RSA { get; }

        public PublicKey(string keyId, RsaKey rsa)
        {
            Id = keyId;
            RSA = rsa;
        }
    }
}
