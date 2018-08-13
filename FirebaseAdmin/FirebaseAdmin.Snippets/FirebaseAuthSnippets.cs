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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace FirebaseAdmin.Snippets
{
    class FirebaseAuthSnippets
    {
        static async Task CreateCustomTokenAsync()
        {
            // [START custom_token]
            var uid = "some-uid";

            string customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);
            // Send token back to client
            // [END custom_token]
            Console.WriteLine("Created custom token: {0}", customToken);
        }

        static async Task CreateCustomTokenWithClaimsAsync()
        {
            // [START custom_token_with_claims]
            var uid = "some-uid";
            var additionalClaims = new Dictionary<string, object>()
            {
                { "premiumAccount", true },
            };

            string customToken = await FirebaseAuth.DefaultInstance
                .CreateCustomTokenAsync(uid, additionalClaims);
            // Send token back to client
            // [END custom_token_with_claims]
            Console.WriteLine("Created custom token: {0}", customToken);
        }

        static async Task VeridyIdTokenAsync(string idToken)
        {
            // [START verify_id_token]
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken);
            string uid = decodedToken.Uid;
            // [END verify_id_token]
            Console.WriteLine("Decoded ID token from user: {0}", uid);
        }
    }
}
