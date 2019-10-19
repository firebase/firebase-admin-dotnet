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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Error codes that can be raised by the Cloud Messaging APIs.
    /// </summary>
    public enum MessagingErrorCode
    {
        /// <summary>
        /// APNs certificate or web push auth key was invalid or missing.
        /// </summary>
        ThirdPartyAuthError,

        /// <summary>
        /// One or more argument specified in the request was invalid.
        /// </summary>
        InvalidArgument,

        /// <summary>
        /// Internal server error.
        /// </summary>
        Internal,

        /// <summary>
        /// Sending limit exceeded for the message target.
        /// </summary>
        QuotaExceeded,

        /// <summary>
        /// The authenticated sender ID is different from the sender ID for the registration token.
        /// </summary>
        SenderIdMismatch,

        /// <summary>
        /// Cloud Messaging service is temporarily unavailable.
        /// </summary>
        Unavailable,

        /// <summary>
        /// App instance was unregistered from FCM. This usually means that the token used is no
        /// longer valid and a new one must be used.
        /// </summary>
        Unregistered,
    }
}
