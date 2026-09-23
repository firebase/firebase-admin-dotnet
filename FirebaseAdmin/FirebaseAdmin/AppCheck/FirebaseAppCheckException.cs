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

using System;
using System.Net.Http;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Exception type raised by Firebase App Check operations.
    /// </summary>
    public sealed class FirebaseAppCheckException : FirebaseException
    {
        internal FirebaseAppCheckException(
            ErrorCode code,
            string message,
            AppCheckErrorCode? appCheckCode = null,
            Exception inner = null,
            HttpResponseMessage response = null)
            : base(code, message, inner, response)
        {
            this.AppCheckErrorCode = appCheckCode;
        }

        /// <summary>
        /// Gets the App Check error code associated with this exception. May be null.
        /// </summary>
        public AppCheckErrorCode? AppCheckErrorCode { get; }
    }
}
