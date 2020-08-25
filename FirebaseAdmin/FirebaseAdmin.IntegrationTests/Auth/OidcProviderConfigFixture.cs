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

using System;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Providers;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public abstract class OidcProviderConfigFixture<T> : IDisposable
    where T : AbstractFirebaseAuth
    {
        public OidcProviderConfigFixture()
        {
            var providerId = $"oidc.{AuthIntegrationUtils.GetRandomIdentifier()}";
            var args = new OidcProviderConfigArgs
            {
                ProviderId = providerId,
                DisplayName = "OIDC_DISPLAY_NAME",
                Enabled = true,
                ClientId = "OIDC_CLIENT_ID",
                Issuer = "https://oidc.com/issuer",
            };
            this.ProviderConfig = this.Auth.CreateProviderConfigAsync(args).Result;
        }

        public abstract T Auth { get; }

        public OidcProviderConfig ProviderConfig { get; set; }

        public string ProviderId => this.ProviderConfig.ProviderId;

        public virtual void Dispose()
        {
            if (this.ProviderConfig != null)
            {
                this.Auth.DeleteProviderConfigAsync(this.ProviderConfig.ProviderId).Wait();
            }
        }
    }
}
