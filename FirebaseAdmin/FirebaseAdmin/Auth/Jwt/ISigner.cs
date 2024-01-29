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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2, " +
    "PublicKey=0024000004800000940000000602000000240000525341310004000001000100c547cac37abd99c8db225ef2f6c8a3602f3b3606cc9891605d02baa56104f4cfc0734aa39b93" +
    "bf7852f7d9266654753cc297e7d2edfe0bac1cdcf9f717241550e0a7b191195b7667bb4f64bcb8e2121380fd1d9d46ad2d92d2d15605093924cceaf74c4861eff62abf69b9291ed0a340e113b" +
    "e11e6a7d3113e92484cf7045cc7")]

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// Represents an object can be used to cryptographically sign data. Mainly used for signing
    /// custom JWT tokens issued to Firebase users.
    /// </summary>
    internal interface ISigner : IDisposable
    {
        /// <summary>
        /// Gets the name of the algorithm used to sign data.
        /// </summary>
        string Algorithm { get; }

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
