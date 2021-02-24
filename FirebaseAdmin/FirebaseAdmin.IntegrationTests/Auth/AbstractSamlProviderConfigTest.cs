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

namespace FirebaseAdmin.IntegrationTests.Auth
{
    [TestCaseOrderer(
        "FirebaseAdmin.IntegrationTests.TestRankOrderer", "FirebaseAdmin.IntegrationTests")]
    public abstract class AbstractSamlProviderConfigTest<T>
    where T : AbstractFirebaseAuth
    {
        private readonly SamlProviderConfigFixture<T> fixture;
        private readonly T auth;
        private readonly string providerId;

        public AbstractSamlProviderConfigTest(SamlProviderConfigFixture<T> fixture)
        {
            this.fixture = fixture;
            this.auth = fixture.Auth;
            this.providerId = fixture.ProviderId;
        }

        [Fact]
        [TestRank(0)]
        public void CreateProviderConfig()
        {
            var config = this.fixture.ProviderConfig;

            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task GetProviderConfig()
        {
            var config = await this.auth.GetSamlProviderConfigAsync(this.providerId);

            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(10)]
        public async Task ListProviderConfig()
        {
            SamlProviderConfig config = null;

            var pagedEnumerable = this.auth.ListSamlProviderConfigsAsync(null);
            var enumerator = pagedEnumerable.GetAsyncEnumerator();
            while (await enumerator.MoveNextAsync())
            {
                if (enumerator.Current.ProviderId == this.providerId)
                {
                    config = enumerator.Current;
                    break;
                }
            }

            Assert.NotNull(config);
            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("SAML_DISPLAY_NAME", config.DisplayName);
            Assert.True(config.Enabled);
            Assert.Equal("IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[0]);
            Assert.Equal("RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal("https://projectId.firebaseapp.com/__/auth/handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(20)]
        public async Task UpdateProviderConfig()
        {
            var args = new SamlProviderConfigArgs()
            {
                ProviderId = this.providerId,
                DisplayName = "UPDATED_SAML_DISPLAY_NAME",
                Enabled = false,
                IdpEntityId = "UPDATED_IDP_ENTITY_ID",
                SsoUrl = "https://example.com/updated-login",
                X509Certificates = new List<string>
                {
                    SamlProviderConfigFixture<T>.X509Certificates[1],
                },
                RpEntityId = "UPDATED_RP_ENTITY_ID",
                CallbackUrl = "https://projectId.firebaseapp.com/__/auth/updated-handler",
            };

            var config = await this.auth.UpdateProviderConfigAsync(args);

            Assert.Equal(this.providerId, config.ProviderId);
            Assert.Equal("UPDATED_SAML_DISPLAY_NAME", config.DisplayName);
            Assert.False(config.Enabled);
            Assert.Equal("UPDATED_IDP_ENTITY_ID", config.IdpEntityId);
            Assert.Equal("https://example.com/updated-login", config.SsoUrl);
            Assert.Single(config.X509Certificates, SamlProviderConfigFixture<T>.X509Certificates[1]);
            Assert.Equal("UPDATED_RP_ENTITY_ID", config.RpEntityId);
            Assert.Equal(
                "https://projectId.firebaseapp.com/__/auth/updated-handler", config.CallbackUrl);
        }

        [Fact]
        [TestRank(30)]
        public async Task DeleteProviderConfig()
        {
            await this.auth.DeleteProviderConfigAsync(this.providerId);
            this.fixture.ProviderConfig = null;

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                () => this.auth.GetSamlProviderConfigAsync(this.providerId));
            Assert.Equal(ErrorCode.NotFound, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.ConfigurationNotFound, exception.AuthErrorCode);
        }
    }
}
