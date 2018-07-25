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

using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Util;
using System;

namespace FirebaseAdmin
{
    /// <summary>
    /// A collection of extension methods for internal use in the SDK.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Extracts and returns the underlying <see cref="ServiceAccountCredential"/> from a
        /// <see cref="GoogleCredential"/>. Returns null if the <c>GoogleCredential</c> is not
        /// based on a service account.
        /// </summary>
        public static ServiceAccountCredential ToServiceAccountCredential(this GoogleCredential credential)
        {
            if (credential.UnderlyingCredential is GoogleCredential)
            {
                return ((GoogleCredential) credential.UnderlyingCredential).ToServiceAccountCredential();
            }
            return credential.UnderlyingCredential as ServiceAccountCredential;
        }

        /// <summary>
        /// Creates a default (unauthenticated) <see cref="ConfigurableHttpClient"/> from the
        /// factory.
        /// </summary> 
        public static ConfigurableHttpClient CreateDefaultHttpClient(this HttpClientFactory clientFactory)
        {
            return clientFactory.CreateHttpClient(new CreateHttpClientArgs());
        }

        /// <summary>
        /// Returns a Unix-styled timestamp (seconds from epoch) from the <see cref="IClock"/>.
        /// </summary>
        public static long UnixTimestamp(this IClock clock)
        {
            return (long) (clock.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}
