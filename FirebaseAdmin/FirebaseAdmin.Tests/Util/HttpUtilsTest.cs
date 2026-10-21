// Copyright 2026, Google Inc. All rights reserved.
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
using System.Globalization;
using System.Threading;
using Xunit;

namespace FirebaseAdmin.Util.Tests
{
    public class HttpUtilsTest
    {
        [Fact]
        public void EncodeQueryParamsNullOrEmpty()
        {
            Assert.Equal(string.Empty, HttpUtils.EncodeQueryParams(null));
            Assert.Equal(string.Empty, HttpUtils.EncodeQueryParams(new Dictionary<string, object>()));
        }

        [Fact]
        public void EncodeQueryParamsEscapesSpecialCharacters()
        {
            var queryParams = new Dictionary<string, object>()
            {
                { "pageToken", "token+1=2&3#4/5" },
                { "pageSize", 100 },
                { "key with spaces", null },
            };

            Assert.Equal(
                "?key%20with%20spaces=&pageSize=100&pageToken=token%2B1%3D2%263%234%2F5",
                HttpUtils.EncodeQueryParams(queryParams));
        }

        [Fact]
        public void EncodeQueryParamsUsesInvariantCulture()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
                var queryParams = new Dictionary<string, object>()
                {
                    { "ratio", 1.5 },
                };

                Assert.Equal("?ratio=1.5", HttpUtils.EncodeQueryParams(queryParams));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
