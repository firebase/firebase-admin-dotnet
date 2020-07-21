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
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public class SamlProviderConfigTest : IClassFixture<SamlProviderConfigFixture>
    {
        private readonly SamlProviderConfigFixture fixture;

        public SamlProviderConfigTest(SamlProviderConfigFixture fixture)
        {
            this.fixture = fixture;
        }

        [Fact]
        [TestRank(0)]
        public void CreateProviderConfig()
        {
            var config = this.fixture.ProviderConfig;

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetProviderConfig()
        {
            var config = await FirebaseAuth.DefaultInstance.GetSamlProviderConfigAsync(
                this.fixture.ProviderId);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task ListProviderConfig()
        {
            SamlProviderConfig config = null;

            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListSamlProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetEnumerator();
            while (await enumerator.MoveNext())
            {
                if (enumerator.Current.ProviderId == this.fixture.ProviderId)
                {
                    config = enumerator.Current;
                    break;
                }
            }

            Assert.NotNull(config);
            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateProviderConfig()
        {
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = this.fixture.ProviderId,
                DisplayName = "UPDATED_SAML_DISPLAY_NAME",
                Enabled = false,
                IdpEntityId = "UPDATED_IDP_ENTITY_ID",
                SsoUrl = "https://example.com/updated-login",
                X509Certificates = new List<string>
                {
                    SamlProviderConfigFixture.X509Certificates[1],
                },
                RpEntityId = "UPDATED_RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/updated-handler",
            };

            var config = await FirebaseAuth.DefaultInstance.UpdateProviderConfigAsync(args);

            Assert.Equal(this.fixture.ProviderId, config.ProviderId);
            Assert.Equal("UPDATED_SAML_DISPLAY_NAME", config.DisplayName);
            Assert.False(config.Enabled);
            Assert.Equal("UPDATED_IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/updated-login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture.X509Certificates[1]);
            Assert.Equal("UPDATED_RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal(
                "https://projectId.firebaseapp.com/__/auth/updated-handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteProviderConfig()
        {
            await FirebaseAuth.DefaultInstance.DeleteProviderConfigAsync(this.fixture.ProviderId);

            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => FirebaseAuth.DefaultInstance.GetSamlProviderConfigAsync(this.fixture.ProviderId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }

    public class SamlProviderConfigFixture : ProviderConfigFixture<SamlProviderConfig>
    {
        internal static readonly IList<string> X509Certificates = new List<string>()
        {
            "-----BEGIN CERTIFICATE-----\nMIICZjCCAc+gAwIBAgIBADANBgkqhkiG9w0BAQ0FADBQMQswCQYDVQQGEwJ1czEL\nMAkGA1UECAwCQ0ExDTALBgNVBAoMBEFjbWUxETAPBgNVBAMMCGFjbWUuY29tMRIw\nEAYDVQQHDAlTdW5ueXZhbGUwHhcNMTgxMjA2MDc1MTUxWhcNMjgxMjAzMDc1MTUx\nWjBQMQswCQYDVQQGEwJ1czELMAkGA1UECAwCQ0ExDTALBgNVBAoMBEFjbWUxETAP\nBgNVBAMMCGFjbWUuY29tMRIwEAYDVQQHDAlTdW5ueXZhbGUwgZ8wDQYJKoZIhvcN\nAQEBBQADgY0AMIGJAoGBAKphmggjiVgqMLXyzvI7cKphscIIQ+wcv7Dld6MD4aKv\n7Jqr8ltujMxBUeY4LFEKw8Terb01snYpDotfilaG6NxpF/GfVVmMalzwWp0mT8+H\nyzyPj89mRcozu17RwuooR6n1ofXjGcBE86lqC21UhA3WVgjPOLqB42rlE9gPnZLB\nAgMBAAGjUDBOMB0GA1UdDgQWBBS0iM7WnbCNOnieOP1HIA+Oz/ML+zAfBgNVHSME\nGDAWgBS0iM7WnbCNOnieOP1HIA+Oz/ML+zAMBgNVHRMEBTADAQH/MA0GCSqGSIb3\nDQEBDQUAA4GBAF3jBgS+wP+K/jTupEQur6iaqS4UvXd//d4vo1MV06oTLQMTz+rP\nOSMDNwxzfaOn6vgYLKP/Dcy9dSTnSzgxLAxfKvDQZA0vE3udsw0Bd245MmX4+GOp\nlbrN99XP1u+lFxCSdMUzvQ/jW4ysw/Nq4JdJ0gPAyPvL6Qi/3mQdIQwx\n-----END CERTIFICATE-----\n",
            "-----BEGIN CERTIFICATE-----\nMIICZjCCAc+gAwIBAgIBADANBgkqhkiG9w0BAQ0FADBQMQswCQYDVQQGEwJ1czEL\nMAkGA1UECAwCQ0ExDTALBgNVBAoMBEFjbWUxETAPBgNVBAMMCGFjbWUuY29tMRIw\nEAYDVQQHDAlTdW5ueXZhbGUwHhcNMTgxMjA2MDc1ODE4WhcNMjgxMjAzMDc1ODE4\nWjBQMQswCQYDVQQGEwJ1czELMAkGA1UECAwCQ0ExDTALBgNVBAoMBEFjbWUxETAP\nBgNVBAMMCGFjbWUuY29tMRIwEAYDVQQHDAlTdW5ueXZhbGUwgZ8wDQYJKoZIhvcN\nAQEBBQADgY0AMIGJAoGBAKuzYKfDZGA6DJgQru3wNUqv+S0hMZfP/jbp8ou/8UKu\nrNeX7cfCgt3yxoGCJYKmF6t5mvo76JY0MWwA53BxeP/oyXmJ93uHG5mFRAsVAUKs\ncVVb0Xi6ujxZGVdDWFV696L0BNOoHTfXmac6IBoZQzNNK4n1AATqwo+z7a0pfRrJ\nAgMBAAGjUDBOMB0GA1UdDgQWBBSKmi/ZKMuLN0ES7/jPa7q7jAjPiDAfBgNVHSME\nGDAWgBSKmi/ZKMuLN0ES7/jPa7q7jAjPiDAMBgNVHRMEBTADAQH/MA0GCSqGSIb3\nDQEBDQUAA4GBAAg2a2kSn05NiUOuWOHwPUjW3wQRsGxPXtbhWMhmNdCfKKteM2+/\nLd/jz5F3qkOgGQ3UDgr3SHEoWhnLaJMF4a2tm6vL2rEIfPEK81KhTTRxSsAgMVbU\nJXBz1md6Ur0HlgQC7d1CHC8/xi2DDwHopLyxhogaZUxy9IaRxUEa2vJW\n-----END CERTIFICATE-----\n",
        };

        public SamlProviderConfigFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            var providerId = $"saml.{GetRandomIdentifier()}";
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = providerId,
                DisplayName = "SAML_DISPLAY_NAME",
                Enabled = true,
                IdpEntityId = "IDP_ENTITY_ID",
                SsoUrl = "https://example.com/login",
                X509Certificates = new List<string> { X509Certificates[0] },
                RpEntityId = "RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/handler",
            };
            this.ProviderConfig = FirebaseAuth.DefaultInstance
                .CreateProviderConfigAsync(args)
                .Result;
            this.ProviderId = providerId;
        }
    }
}
