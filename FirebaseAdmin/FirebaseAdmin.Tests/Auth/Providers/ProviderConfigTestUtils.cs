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
using System.Net.Http;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Auth.Providers.Tests
{
    internal sealed class ProviderConfigTestUtils
    {
        internal const string ConfigNotFoundResponse = @"{
            ""error"": {
                ""message"": ""CONFIGURATION_NOT_FOUND""
            }
        }";

        private static readonly string ClientVersion =
            $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        internal static FirebaseAuth CreateFirebaseAuth(HttpMessageHandler handler = null)
        {
            var providerConfigManager = new ProviderConfigManager(new ProviderConfigManager.Args
            {
                Credential = MockCredential,
                ProjectId = "project1",
                ClientFactory = new MockHttpClientFactory(handler ?? new MockMessageHandler()),
                RetryOptions = RetryOptions.NoBackOff,
            });
            var args = FirebaseAuth.Args.CreateDefault();
            args.ProviderConfigManager = new Lazy<ProviderConfigManager>(providerConfigManager);
            return new FirebaseAuth(args);
        }

        internal static void AssertClientVersionHeader(MockMessageHandler.IncomingRequest request)
        {
            Assert.Contains(ClientVersion, request.Headers.GetValues("X-Client-Version"));
        }
    }
}