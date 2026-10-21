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

using Google.Apis.Util;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Represents the result of verifying an App Check token.
    /// </summary>
    public sealed class VerifyAppCheckTokenResponse
    {
        internal VerifyAppCheckTokenResponse(
            string appId, DecodedAppCheckToken token, bool? alreadyConsumed = null)
        {
            this.AppId = appId.ThrowIfNullOrEmpty(nameof(appId));
            this.Token = token.ThrowIfNull(nameof(token));
            this.AlreadyConsumed = alreadyConsumed;
        }

        /// <summary>
        /// Gets the App ID associated with the App Check token.
        /// </summary>
        public string AppId { get; }

        /// <summary>
        /// Gets the decoded App Check token claims.
        /// </summary>
        public DecodedAppCheckToken Token { get; }

        /// <summary>
        /// Gets a value indicating whether the token was already consumed.
        /// <c>null</c> if consumption was not requested.
        /// </summary>
        public bool? AlreadyConsumed { get; }
    }
}
