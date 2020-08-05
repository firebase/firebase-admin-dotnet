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
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// An object that can be used to retrieve a set of RSA public keys for verifying signatures.
    /// </summary>
    internal interface IPublicKeySource
    {
        /// <summary>
        /// Returns a set of public keys.
        /// </summary>
        /// <returns>A task that completes with a list of public keys.</returns>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        Task<IReadOnlyList<PublicKey>> GetPublicKeysAsync(CancellationToken cancellationToken);
    }
}
