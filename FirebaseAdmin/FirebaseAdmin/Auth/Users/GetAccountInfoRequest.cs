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

namespace FirebaseAdmin.Auth.Users
{
    /// <summary>
    /// Represents a request to look up account information.
    /// </summary>
    internal sealed class GetAccountInfoRequest
    {
        private readonly IList<string> uids = new List<string>();
        private readonly IList<string> emails = new List<string>();
        private readonly IList<string> phoneNumbers = new List<string>();
        private readonly IList<FederatedUserId> federatedUserIds = new List<FederatedUserId>();

        public void AddUid(string uid)
        {
            this.uids.Add(uid);
        }

        public void AddEmail(string email)
        {
            this.emails.Add(email);
        }

        public void AddPhoneNumber(string phoneNumber)
        {
            this.phoneNumbers.Add(phoneNumber);
        }

        public void AddFederatedUserId(string providerId, string providerUid)
        {
            this.federatedUserIds.Add(new FederatedUserId { ProviderId = providerId, RawId = providerUid });
        }

        internal IDictionary<string, object> Build()
        {
            var result = new Dictionary<string, object>();

            if (this.uids.Any())
            {
                result.Add("localId", this.uids);
            }

            if (this.emails.Any())
            {
                result.Add("email", this.emails);
            }

            if (this.phoneNumbers.Any())
            {
                result.Add("phoneNumber", this.phoneNumbers);
            }

            if (this.federatedUserIds.Any())
            {
                result.Add(
                    "federatedUserId",
                    this.federatedUserIds.Select(federatedUserId => federatedUserId.Build()));
            }

            return result;
        }

        private struct FederatedUserId
        {
            internal string ProviderId;
            internal string RawId;

            internal IDictionary<string, object> Build()
            {
                return new Dictionary<string, object>
                {
                    { "providerId", this.ProviderId },
                    { "rawId", this.RawId },
                };
            }
        }
    }
}
