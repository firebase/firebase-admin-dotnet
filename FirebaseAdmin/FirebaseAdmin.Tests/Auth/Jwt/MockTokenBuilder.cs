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
using System.Threading.Tasks;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public sealed class MockTokenBuilder
    {
        public string ProjectId { get; set; }

        public string TenantId { get; set; }

        public string Uid { get; set; }

        public string IssuerPrefix { get; set; }

        public IClock Clock { get; set; }

        internal ISigner Signer { get; set; }

        public async Task<string> CreateTokenAsync(
            IDictionary<string, object> headerOverrides = null,
            IDictionary<string, object> payloadOverrides = null)
        {
            var header = new Dictionary<string, object>()
            {
                { "alg", "RS256" },
                { "typ", "jwt" },
                { "kid", "test-key-id" },
            };
            if (headerOverrides != null)
            {
                foreach (var entry in headerOverrides)
                {
                    header[entry.Key] = entry.Value;
                }
            }

            var payload = new Dictionary<string, object>()
            {
                { "sub", this.Uid },
                { "iss", $"{this.IssuerPrefix}/{this.ProjectId}" },
                { "aud", this.ProjectId },
                { "iat", this.Clock.UnixTimestamp() - (60 * 10) },
                { "exp", this.Clock.UnixTimestamp() + (60 * 50) },
            };
            if (this.TenantId != null)
            {
                payload["firebase"] = new Dictionary<string, object>
                {
                    { "tenant", this.TenantId },
                };
            }

            if (payloadOverrides != null)
            {
                foreach (var entry in payloadOverrides)
                {
                    payload[entry.Key] = entry.Value;
                }
            }

            return await JwtUtils.CreateSignedJwtAsync(header, payload, this.Signer);
        }

        public void AssertFirebaseToken(
            FirebaseToken decoded,
            IDictionary<string, object> expectedClaims = null)
        {
            Assert.Equal(this.ProjectId, decoded.Audience);
            Assert.Equal(this.Uid, decoded.Uid);
            Assert.Equal(this.Uid, decoded.Subject);

            // The default test token created by CreateTokenAsync has an issue time 10 minutes
            // ago, and an expiry time 50 minutes in the future.
            Assert.Equal(
                this.Clock.UnixTimestamp() - (60 * 10), decoded.IssuedAtTimeSeconds);
            Assert.Equal(
                this.Clock.UnixTimestamp() + (60 * 50), decoded.ExpirationTimeSeconds);

            if (expectedClaims != null)
            {
                if (this.TenantId != null)
                {
                    Assert.Contains(decoded.Claims, (kvp) => kvp.Key == "firebase");
                    Assert.Equal(expectedClaims.Count + 1, decoded.Claims.Count);
                }
                else
                {
                    Assert.Equal(expectedClaims.Count, decoded.Claims.Count);
                }

                foreach (var entry in expectedClaims)
                {
                    Assert.Equal(entry.Value, decoded.Claims[entry.Key]);
                }
            }
            else if (this.TenantId != null)
            {
                Assert.Equal("firebase", Assert.Single(decoded.Claims).Key);
            }
            else
            {
                Assert.Empty(decoded.Claims);
            }

            Assert.Equal(this.TenantId, decoded.TenantId);
        }
    }
}
