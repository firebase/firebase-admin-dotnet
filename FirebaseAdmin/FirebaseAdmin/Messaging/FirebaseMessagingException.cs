// Copyright 2018, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Common error type for all exceptions raised by Firebase messaging APIs.
    /// </summary>
    public class FirebaseMessagingException : FirebaseException
    {
        internal FirebaseMessagingException(int errorCode, string message)
            : base(message)
        {
            this.ErrorCode = errorCode;
        }

        internal FirebaseMessagingException(int errorCode, string message, Exception inner)
            : base(message, inner)
        {
            this.ErrorCode = errorCode;
        }

        /// <summary>
        /// Gets an error code that may provide more information about the error.
        /// </summary>
        public int ErrorCode { get; }
    }
}
