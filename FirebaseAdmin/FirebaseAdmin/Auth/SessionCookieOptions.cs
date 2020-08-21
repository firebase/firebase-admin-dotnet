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
    /// Options for the
    /// <see cref="FirebaseAuth.CreateSessionCookieAsync(string, SessionCookieOptions)"/>
    /// API.
    /// </summary>
    public sealed class SessionCookieOptions
    {
        /// <summary>
        /// Gets or sets the duration until the cookie is expired. Must be between 5 minutes
        /// and 14 days. The backend service uses seconds precision for this parameter.
        /// </summary>
        public TimeSpan ExpiresIn { get; set; }

        internal SessionCookieOptions CopyAndValidate()
        {
            var copy = new SessionCookieOptions()
            {
                ExpiresIn = this.ExpiresIn,
            };
            if (copy.ExpiresIn < TimeSpan.FromMinutes(5))
            {
                throw new ArgumentException("ExpiresIn must be at least 5 minutes");
            }
            else if (copy.ExpiresIn > TimeSpan.FromDays(14))
            {
                throw new ArgumentException("ExpiresIn must be at most 14 days");
            }

            return copy;
        }
    }
}
