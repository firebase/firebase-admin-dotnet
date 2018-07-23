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

namespace FirebaseAdmin
{
    /// <summary>
    /// A collection of extension methods for internal use.
    /// </summary>
    internal static class Extensions  
    {
        public static ServiceAccountCredential ToServiceAccountCredential(this GoogleCredential credential)
        {
            if (credential.UnderlyingCredential is GoogleCredential)
            {
                return ((GoogleCredential) credential.UnderlyingCredential).ToServiceAccountCredential();
            }
            return credential.UnderlyingCredential as ServiceAccountCredential;
        }
    }
}
