// Copyright 2020, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Contains additional metadata associated with a user account.
    /// </summary>
    public sealed class UserMetadata
    {
        private readonly long creationTimestampMillis;
        private readonly long lastSignInTimestampMillis;
        private readonly DateTime? lastRefreshTimestamp;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserMetadata"/> class with the specified creation and last sign-in timestamps.
        /// </summary>
        /// <param name="creationTimestamp">A timestamp representing the date and time that the user account was created.</param>
        /// <param name="lastSignInTimestamp">A timestamp representing the date and time that the user account was last signed-on to.</param>
        /// <param name="lastRefreshTimestamp">A timestamp representing the date and time that the user account was last refreshed.</param>
        public UserMetadata(long creationTimestamp, long lastSignInTimestamp, DateTime? lastRefreshTimestamp)
        {
            this.creationTimestampMillis = creationTimestamp;
            this.lastSignInTimestampMillis = lastSignInTimestamp;
            this.lastRefreshTimestamp = lastRefreshTimestamp;
        }

        /// <summary>
        /// Gets a timestamp representing the date and time that the account was created.
        /// If not available this property is <c>null</c>.
        /// </summary>
        public DateTime? CreationTimestamp
        {
            get => this.ToDateTime(this.creationTimestampMillis);
        }

        /// <summary>
        /// Gets a timestamp representing the last time that the user has signed in. If the user
        /// has never signed in this property is <c>null</c>.
        /// </summary>
        public DateTime? LastSignInTimestamp
        {
            get => this.ToDateTime(this.lastSignInTimestampMillis);
        }

        /// <summary>
        /// Gets the time at which the user was last active (ID token refreshed), or <c>null</c>
        /// if the user was never active.
        /// </summary>
        public DateTime? LastRefreshTimestamp
        {
            get => this.lastRefreshTimestamp;
        }

        private DateTime? ToDateTime(long millisFromEpoch)
        {
            if (millisFromEpoch == 0)
            {
                return null;
            }

            return UserRecord.UnixEpoch.AddMilliseconds(millisFromEpoch);
        }
    }
}
