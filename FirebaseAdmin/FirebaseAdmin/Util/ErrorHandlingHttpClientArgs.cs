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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// Arguments for constructing an instance of <see cref="ErrorHandlingHttpClient{T}"/>.
    /// </summary>
    /// <typeparam name="T">Subtype of <see cref="FirebaseException"/> raised by the
    /// HTTP client constructed using these arguments.</typeparam>
    internal sealed class ErrorHandlingHttpClientArgs<T>
    where T : FirebaseException
    {
        internal HttpClientFactory HttpClientFactory { get; set; }

        internal GoogleCredential Credential { get; set; }

        internal IHttpResponseDeserializer Deserializer { get; set; }

        internal IHttpErrorResponseHandler<T> ErrorResponseHandler { get; set; }

        internal IHttpRequestExceptionHandler<T> RequestExceptionHandler { get; set; }

        internal IDeserializeExceptionHandler<T> DeserializeExceptionHandler { get; set; }

        internal RetryOptions RetryOptions { get; set; }
    }
}
