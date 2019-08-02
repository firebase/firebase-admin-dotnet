// Copyright 2019, Google Inc. All rights reserved.
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

using System.Net.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// HTTP response and the body string read from it.
    /// </summary>
    internal class ResponseInfo
    {
        internal ResponseInfo(HttpResponseMessage response, string body)
        {
            this.HttpResponse = response.ThrowIfNull(nameof(response));
            this.Body = body.ThrowIfNull(nameof(body));
        }

        internal ResponseInfo(ResponseInfo other)
        : this(other.HttpResponse, other.Body) { }

        internal HttpResponseMessage HttpResponse { get; private set; }

        internal string Body { get; private set; }
    }
}
