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

using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    internal sealed class FirebaseTokenVerifierArgs
    {
        public string ProjectId { get; set; }

        public string ShortName { get; set; }

        public string Operation { get; set; }

        public string Url { get; set; }

        public string Issuer { get; set; }

        public IClock Clock { get; set; }

        public IPublicKeySource PublicKeySource { get; set; }

        public AuthErrorCode InvalidTokenCode { get; set; }

        public AuthErrorCode ExpiredTokenCode { get; set; }

        internal static FirebaseTokenVerifierArgs ForIdTokens(
            string projectId, IPublicKeySource keySource, IClock clock = null)
        {
            return new FirebaseTokenVerifierArgs()
            {
                ProjectId = projectId,
                ShortName = "ID token",
                Operation = "VerifyIdTokenAsync()",
                Url = "https://firebase.google.com/docs/auth/admin/verify-id-tokens",
                Issuer = "https://securetoken.google.com/",
                Clock = clock ?? SystemClock.Default,
                PublicKeySource = keySource,
                InvalidTokenCode = AuthErrorCode.InvalidIdToken,
                ExpiredTokenCode = AuthErrorCode.ExpiredIdToken,
            };
        }

        internal static FirebaseTokenVerifierArgs ForSessionCookies(
            string projectId, IPublicKeySource keySource, IClock clock = null)
        {
            return new FirebaseTokenVerifierArgs()
            {
                ProjectId = projectId,
                ShortName = "session cookie",
                Operation = "VerifySessionCookieAsync()",
                Url = "https://firebase.google.com/docs/auth/admin/manage-cookies",
                Issuer = "https://session.firebase.google.com/",
                Clock = clock ?? SystemClock.Default,
                PublicKeySource = keySource,
                InvalidTokenCode = AuthErrorCode.InvalidSessionCookie,
                ExpiredTokenCode = AuthErrorCode.ExpiredSessionCookie,
            };
        }
    }
}
