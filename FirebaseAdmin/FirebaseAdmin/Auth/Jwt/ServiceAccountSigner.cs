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
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// An <see cref="ISigner"/> implementation that uses service account credentials to sign
    /// data. Uses the private key present in the credential to produce signatures.
    /// </summary>
    internal sealed class ServiceAccountSigner : ISigner
    {
        private readonly ServiceAccountCredential credential;

        public ServiceAccountSigner(ServiceAccountCredential credential)
        {
            this.credential = credential.ThrowIfNull(nameof(credential));
        }

        public Task<string> GetKeyIdAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(this.credential.Id);
        }

        public Task<byte[]> SignDataAsync(byte[] data, CancellationToken cancellationToken = default(CancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var signature = this.credential.CreateSignature(data);
            return Task.FromResult(Convert.FromBase64String(signature));
        }

        public void Dispose() { }
    }
}
