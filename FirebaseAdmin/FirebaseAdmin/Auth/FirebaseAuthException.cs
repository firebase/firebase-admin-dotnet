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


namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Generic exception related to Firebase Authentication. 
    /// Check the error code and message for more details.
    /// </summary>
    internal sealed class FirebaseAuthException : FirebaseException
    {
        /// <summary>
        /// An error code that may provide more information about the error.
        /// </summary>
        public string ErrorCode { get; private set; }

        public FirebaseAuthException(string errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
