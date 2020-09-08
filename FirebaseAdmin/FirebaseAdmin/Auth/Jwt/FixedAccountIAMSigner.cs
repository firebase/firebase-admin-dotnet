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

using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Auth.Jwt
{
    /// <summary>
    /// An <see cref="ISigner"/> implementation that uses the IAM service to sign data. Unlike
    /// <see cref="IAMSigner"/> this class does not attempt to auto discover a service account ID.
    /// Insterad it must be initialized with a fixed service account ID string.
    /// </summary>
    internal sealed class FixedAccountIAMSigner : IAMSigner
    {
        private readonly string keyId;

        public FixedAccountIAMSigner(Args args)
        : base(args.ClientFactory, args.Credential, args.RetryOptions)
        {
            this.keyId = args.KeyId.ThrowIfNullOrEmpty(nameof(args.KeyId));
        }

        public override Task<string> GetKeyIdAsync(
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(this.keyId);
        }

        internal static new FixedAccountIAMSigner Create(FirebaseApp app)
        {
            var args = new FixedAccountIAMSigner.Args()
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                KeyId = app.Options.ServiceAccountId,
                RetryOptions = RetryOptions.Default,
            };

            return new FixedAccountIAMSigner(args);
        }

        internal class Args
        {
            internal HttpClientFactory ClientFactory { get; set; }

            internal GoogleCredential Credential { get; set; }

            internal string KeyId { get; set; }

            internal RetryOptions RetryOptions { get; set; }
        }
    }
}
