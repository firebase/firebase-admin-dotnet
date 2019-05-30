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
    internal class FirebaseAuthSnippets
    {
        internal static async Task CreateCustomTokenAsync()
        {
            // [START custom_token]
            var uid = "some-uid";

            string customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);
            // Send token back to client
            // [END custom_token]
            Console.WriteLine("Created custom token: {0}", customToken);
        }

        internal static async Task CreateCustomTokenWithClaimsAsync()
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

        internal static async Task VeridyIdTokenAsync(string idToken)
        {
            // [START verify_id_token]
            FirebaseToken decodedToken = await FirebaseAuth.DefaultInstance
                .VerifyIdTokenAsync(idToken);
            string uid = decodedToken.Uid;
            // [END verify_id_token]
            Console.WriteLine("Decoded ID token from user: {0}", uid);
        }

        internal static async Task GetUserAsync(string uid)
        {
            // [START get_user_by_id]
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine("Successfully fetched user data: {0}", userRecord.Uid);
            // [END get_user_by_id]
        }

        internal static async Task DeleteUserAsync(string uid)
        {
            // [START delete_user]
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            Console.WriteLine("Successfully deleted user.");
            // [END delete_user]
        }

        internal static async Task SetCustomUserClaimsAsync(string uid)
        {
            // [START set_custom_user_claims]
            // Set admin privileges on the user corresponding to uid.
            var claims = new Dictionary<string, object>()
            {
                { "admin", true },
            };
            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(uid, claims);
            // The new custom claims will propagate to the user's ID token the
            // next time a new one is issued.
            // [END set_custom_user_claims]

            var idToken = "id_token";
            // [START verify_custom_claims]
            // Verify the ID token first.
            FirebaseToken decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            object isAdmin;
            if (decoded.Claims.TryGetValue("admin", out isAdmin))
            {
                if ((bool)isAdmin)
                {
                    // Allow access to requested admin resource.
                }
            }

            // [END verify_custom_claims]

            // [START read_custom_user_claims]
            // Lookup the user associated with the specified uid.
            UserRecord user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            Console.WriteLine(user.CustomClaims["admin"]);
            // [END read_custom_user_claims]
        }
    }
}
