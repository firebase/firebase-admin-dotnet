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

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using FirebaseAdmin.Auth.Tests;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Providers.Tests
{
    public class ProviderTestConfig
    {
        public static readonly IEnumerable<object[]> TestConfigs = new List<object[]>()
        {
            new object[] { ProviderTestConfig.ForFirebaseAuth() },
            new object[] { ProviderTestConfig.ForTenantAwareFirebaseAuth("tenant1") },
        };

        public static readonly IEnumerable<object[]> InvalidProvierIds = new List<object[]>()
        {
            new object[] { ProviderTestConfig.ForFirebaseAuth(), null },
            new object[] { ProviderTestConfig.ForFirebaseAuth(), string.Empty },
            new object[] { ProviderTestConfig.ForTenantAwareFirebaseAuth("tenant1"), null },
            new object[] { ProviderTestConfig.ForTenantAwareFirebaseAuth("tenant1"), string.Empty },
        };

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

        private const string MockProjectId = "project1";

        private static readonly string ClientVersion =
            $"DotNet/Admin/{FirebaseApp.GetSdkVersion()}";

        private readonly AuthBuilder authBuilder;

        private ProviderTestConfig(string tenantId = null)
        {
            this.authBuilder = new AuthBuilder
            {
                ProjectId = MockProjectId,
                RetryOptions = RetryOptions.NoBackOff,
                TenantId = tenantId,
            };
        }

        public string TenantId => this.authBuilder.TenantId;

        public static ProviderTestConfig ForFirebaseAuth()
        {
            return new ProviderTestConfig();
        }

        public static ProviderTestConfig ForTenantAwareFirebaseAuth(string tenantId)
        {
            return new ProviderTestConfig(tenantId);
        }

        public static IEnumerator<object[]> WithTestConfigs(IEnumerator<object[]> args)
        {
            foreach (var config in TestConfigs)
            {
                while (args.MoveNext())
                {
                    yield return config.Concat(args.Current).ToArray();
                }
            }
        }

        public AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler = null)
        {
            var options = new TestOptions
            {
                ProviderConfigRequestHandler = handler ?? new MockMessageHandler(),
            };
            return this.authBuilder.Build(options);
        }

        internal void AssertRequest(
            string expectedSuffix, MockMessageHandler.IncomingRequest request)
        {
            var tenantInfo = this.TenantId != null ? $"/tenants/{this.TenantId}" : string.Empty;
            var expectedPath = $"/v2/projects/{MockProjectId}{tenantInfo}/{expectedSuffix}";
            Assert.Equal(expectedPath, request.Url.PathAndQuery);
            Assert.Contains(ClientVersion, request.Headers.GetValues("X-Client-Version"));
        }

        public class InvalidListOptions : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator() => WithTestConfigs(this.MakeEnumerator());

            public IEnumerator<object[]> MakeEnumerator()
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
