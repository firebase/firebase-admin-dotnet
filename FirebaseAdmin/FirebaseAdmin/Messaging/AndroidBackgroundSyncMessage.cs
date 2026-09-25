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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents a background sync message for Android V2 config.
    /// </summary>
    public sealed class AndroidBackgroundSyncMessage
    {
        /// <summary>
        /// Copies this background sync message, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal AndroidBackgroundSyncMessage CopyAndValidate()
        {
            return new AndroidBackgroundSyncMessage();
        }
    }
}
