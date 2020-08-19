// Copyright 2020, Google Inc. All rights reserved.
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
using System.Linq;
using FirebaseAdmin.Auth.Users;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Used for looking up an account by provider.
    ///
    /// See <see cref="AbstractFirebaseAuth.GetUsersAsync(IReadOnlyCollection{UserIdentifier})"/>.
    /// </summary>
    public sealed class ProviderIdentifier : UserIdentifier
    {
        private readonly string providerId;
        private readonly string providerUid;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProviderIdentifier"/> class.
        /// </summary>
        /// <param name="providerId">The providerId.</param>
        /// <param name="providerUid">The providerUid.</param>
        public ProviderIdentifier(string providerId, string providerUid)
        {
            UserRecordArgs.CheckProvider(providerId, providerUid, required: true);
            this.providerId = providerId;
            this.providerUid = providerUid;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ProviderIdentifier({this.providerId}, {this.providerUid})";
        }

        internal override void Populate(GetAccountInfoRequest payload)
        {
            payload.AddFederatedUserId(this.providerId, this.providerUid);
        }

        internal override bool Matches(UserRecord userRecord)
        {
            return userRecord.ProviderData.Any(
                userInfo => this.providerId == userInfo.ProviderId && this.providerUid == userInfo.Uid);
        }
    }
}
