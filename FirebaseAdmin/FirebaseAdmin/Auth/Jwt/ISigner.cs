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
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// Represents an object can be used to cryptographically sign data. Mainly used for signing
    /// custom JWT tokens issued to Firebase users.
    /// </summary>
    internal interface ISigner : IDisposable
    {
        /// <summary>
        /// Returns the ID (client email) of the service account used to sign payloads.
        /// </summary>
        /// <returns>A task that completes with the key ID string.</returns>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        Task<string> GetKeyIdAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Signs the given data payload.
        /// </summary>
        /// <returns>A task that completes with the crypto signature.</returns>
        /// <param name="data">A byte array of data to be signed.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        Task<byte[]> SignDataAsync(byte[] data, CancellationToken cancellationToken);
    }
}
