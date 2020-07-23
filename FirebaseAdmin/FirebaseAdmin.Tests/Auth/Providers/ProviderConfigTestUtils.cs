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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        internal const string UnknownErrorResponse = @"{
            ""error"": {
                ""message"": ""UNKNOWN""
            }
        }";

        internal static readonly HttpMethod PatchMethod = new HttpMethod("PATCH");

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

        internal static IDictionary<string, string> ExtractQueryParams(
            MockMessageHandler.IncomingRequest req)
        {
            return req.Url.Query.Substring(1).Split('&').ToDictionary(
                entry => entry.Split('=')[0], entry => entry.Split('=')[1]);
        }

        public class InvalidListOptions : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                // {
                //    1st element: InvalidInput,
                //    2nd element: ExpectedError,
                // }
                yield return new object[]
                {
                    new ListProviderConfigsOptions()
                    {
                        PageSize = 101,
                    },
                    "Page size must not exceed 100.",
                };
                yield return new object[]
                {
                    new ListProviderConfigsOptions()
                    {
                        PageSize = 0,
                    },
                    "Page size must be positive.",
                };
                yield return new object[]
                {
                    new ListProviderConfigsOptions()
                    {
                        PageSize = -1,
                    },
                    "Page size must be positive.",
                };
                yield return new object[]
                {
                    new ListProviderConfigsOptions()
                    {
                        PageToken = string.Empty,
                    },
                    "Page token must not be empty.",
                };
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}
