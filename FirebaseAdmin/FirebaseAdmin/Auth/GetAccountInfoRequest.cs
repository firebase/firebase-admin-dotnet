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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a request to lookup account information.
    /// </summary>
    internal sealed class GetAccountInfoRequest
    {
        private IList<string> uids = null;
        private IList<string> emails = null;
        private IList<string> phoneNumbers = null;
        private IList<FederatedUserId> federatedUserIds = null;

        public void AddUid(string uid)
        {
            if (this.uids == null)
            {
                this.uids = new List<string>();
            }

            this.uids.Add(uid);
        }

        public void AddEmail(string email)
        {
            if (this.emails == null)
            {
                this.emails = new List<string>();
            }

            this.emails.Add(email);
        }

        public void AddPhoneNumber(string phoneNumber)
        {
            if (this.phoneNumbers == null)
            {
                this.phoneNumbers = new List<string>();
            }

            this.phoneNumbers.Add(phoneNumber);
        }

        public void AddFederatedUserId(string providerId, string providerUid)
        {
            if (this.federatedUserIds == null)
            {
                this.federatedUserIds = new List<FederatedUserId>();
            }

            this.federatedUserIds.Add(new FederatedUserId { ProviderId = providerId, RawId = providerUid });
        }

        internal IDictionary<string, object> Build()
        {
            var result = new Dictionary<string, object>();

            if (this.uids != null)
            {
                result.Add("localId", this.uids);
            }

            if (this.emails != null)
            {
                result.Add("email", this.emails);
            }

            if (this.phoneNumbers != null)
            {
                result.Add("phoneNumber", this.phoneNumbers);
            }

            if (this.federatedUserIds != null)
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
