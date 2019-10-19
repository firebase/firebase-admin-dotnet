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

using System;
using System.Net.Http;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Exception type raised by Firebase Auth APIs.
    /// </summary>
    public sealed class FirebaseAuthException : FirebaseException
    {
        internal FirebaseAuthException(
          ErrorCode code,
          string message,
          AuthErrorCode? fcmCode = null,
          Exception inner = null,
          HttpResponseMessage response = null)
        : base(code, message, inner, response)
        {
            this.AuthErrorCode = fcmCode;
        }

        /// <summary>
        /// Gets the Firease Auth error code associated with this exception. May be null.
        /// </summary>
        public AuthErrorCode? AuthErrorCode { get; private set; }
    }
}
