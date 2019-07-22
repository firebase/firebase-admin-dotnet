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

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An interface for handling <see cref="HttpRequestException"/> instances encountered while
    /// sending HTTP requests.
    /// </summary>
    /// <typeparam name="T">Subtype of <see cref="FirebaseException"/> raised by this
    /// error handler.</typeparam>
    internal interface IHttpRequestExceptionHandler<T>
    where T : FirebaseException
    {
        /// <summary>
        /// Handles the exceptions occured while sending HTTP requests, and turns
        /// them into appropriate instances of <see cref="FirebaseException"/>.
        /// </summary>
        /// <returns>A <see cref="FirebaseException"/> instance.</returns>
        /// <param name="exception">The exception encountered while sending a request.</param>
        T HandleHttpRequestException(HttpRequestException exception);
    }
}
