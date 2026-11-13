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

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Options for verifying a Firebase App Check token.
    /// </summary>
    public sealed class VerifyAppCheckTokenOptions
    {
        /// <summary>
        /// Gets or sets a value indicating whether to consume the token on the App Check backend
        /// to protect against replay attacks.
        /// </summary>
        public bool Consume { get; set; }
    }
}
