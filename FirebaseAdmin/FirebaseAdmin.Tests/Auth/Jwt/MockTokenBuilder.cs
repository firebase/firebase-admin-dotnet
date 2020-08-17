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
        internal string ProjectId { get; set; }

        internal string TenantId { get; set; }

        internal string Uid { get; set; }

        internal string IssuerPrefix { get; set; }

        internal IClock Clock { get; set; }

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

            var uid = this.Uid.ThrowIfNullOrEmpty(nameof(this.Uid));
            var issuerPrefix = this.IssuerPrefix.ThrowIfNullOrEmpty(nameof(this.IssuerPrefix));
            var projectId = this.ProjectId.ThrowIfNullOrEmpty(nameof(this.ProjectId));
            var clock = this.Clock.ThrowIfNull(nameof(this.Clock));
            var payload = new Dictionary<string, object>()
            {
                { "sub", uid },
                { "iss", $"{issuerPrefix}/{projectId}" },
                { "aud", projectId },
                { "iat", clock.UnixTimestamp() - (60 * 10) },
                { "exp", clock.UnixTimestamp() + (60 * 50) },
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

            var signer = this.Signer.ThrowIfNull(nameof(this.Signer));
            return await JwtUtils.CreateSignedJwtAsync(header, payload, signer);
        }

        public void AssertFirebaseToken(
            FirebaseToken decoded,
            IDictionary<string, object> expectedClaims = null)
        {
            Assert.Equal(this.ProjectId, decoded.Audience);
            Assert.Equal(this.Uid, decoded.Uid);
            Assert.Equal(this.Uid, decoded.Subject);
            Assert.Equal(this.TenantId, decoded.TenantId);

            // The default test token created by CreateTokenAsync has an issue time 10 minutes
            // ago, and an expiry time 50 minutes in the future.
            expectedClaims = this.CopyClaims(expectedClaims);
            object iat;
            if (!expectedClaims.TryGetValue("iat", out iat))
            {
                iat = this.Clock.UnixTimestamp() - (60 * 10);
            }

            object exp;
            if (!expectedClaims.TryGetValue("exp", out exp))
            {
                exp = this.Clock.UnixTimestamp() + (60 * 50);
            }

            Assert.Equal((long)iat, decoded.IssuedAtTimeSeconds);
            Assert.Equal((long)exp, decoded.ExpirationTimeSeconds);

            this.RemoveStandardClaims(expectedClaims);
            if (this.TenantId != null)
            {
                Assert.Equal(expectedClaims.Count + 1, decoded.Claims.Count);
                Assert.Contains(decoded.Claims, (kvp) => kvp.Key == "firebase");
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

        private IDictionary<string, object> CopyClaims(
            IDictionary<string, object> claims)
        {
            return new Dictionary<string, object>(claims ?? new Dictionary<string, object>());
        }

        private void RemoveStandardClaims(IDictionary<string, object> claims)
        {
            // These are the only standard claims currently used in tests.
            var standardClaims = new List<string> { "exp", "iat", "iss", "aud", "sub" };
            foreach (var claim in standardClaims)
            {
                claims.Remove(claim);
            }
        }
    }
}
