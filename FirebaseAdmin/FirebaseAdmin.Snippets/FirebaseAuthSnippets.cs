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
            Console.WriteLine($"Successfully fetched user data: {userRecord.Uid}");
            // [END get_user_by_id]
        }

        internal static async Task GetUserByEmailAsync(string email)
        {
            // [START get_user_by_email]
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserByEmailAsync(email);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine($"Successfully fetched user data: {userRecord.Uid}");
            // [END get_user_by_email]
        }

        internal static async Task GetUserByPhoneNumberAsync(string phoneNumber)
        {
            // [START get_user_by_phone]
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.GetUserByPhoneNumberAsync(phoneNumber);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine($"Successfully fetched user data: {userRecord.Uid}");
            // [END get_user_by_phone]
        }

        internal static async Task CreateUserAsync()
        {
            // [START create_user]
            UserRecordArgs args = new UserRecordArgs()
            {
                Email = "user@example.com",
                EmailVerified = false,
                PhoneNumber = "+11234567890",
                Password = "secretPassword",
                DisplayName = "John Doe",
                PhotoUrl = "http://www.example.com/12345678/photo.png",
                Disabled = false,
            };
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine($"Successfully created new user: {userRecord.Uid}");
            // [END create_user]
        }

        internal static async Task CreateUserWithUidAsync()
        {
            // [START create_user_with_uid]
            UserRecordArgs args = new UserRecordArgs()
            {
                Uid = "some-uid",
                Email = "user@example.com",
                PhoneNumber = "+11234567890",
            };
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.CreateUserAsync(args);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine($"Successfully created new user: {userRecord.Uid}");
            // [END create_user_with_uid]
        }

        internal static async Task UpdateUserAsync(string uid)
        {
            // [START update_user]
            UserRecordArgs args = new UserRecordArgs()
            {
                Uid = uid,
                Email = "modifiedUser@example.com",
                PhoneNumber = "+11234567890",
                EmailVerified = true,
                Password = "newPassword",
                DisplayName = "Jane Doe",
                PhotoUrl = "http://www.example.com/12345678/photo.png",
                Disabled = true,
            };
            UserRecord userRecord = await FirebaseAuth.DefaultInstance.UpdateUserAsync(args);
            // See the UserRecord reference doc for the contents of userRecord.
            Console.WriteLine($"Successfully updated user: {userRecord.Uid}");
            // [END update_user]
        }

        internal static async Task DeleteUserAsync(string uid)
        {
            // [START delete_user]
            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            Console.WriteLine("Successfully deleted user.");
            // [END delete_user]
        }

        internal static async Task ListAllUsersAsync()
        {
            // [START list_all_users]
            // Start listing users from the beginning, 1000 at a time.
            var pagedEnumerable = FirebaseAuth.DefaultInstance.ListUsersAsync(null);
            var responses = pagedEnumerable.AsRawResponses().GetEnumerator();
            while (await responses.MoveNext())
            {
                ExportedUserRecords response = responses.Current;
                foreach (ExportedUserRecord user in response.Users)
                {
                    Console.WriteLine($"User: {user.Uid}");
                }
            }

            // Iterate through all users. This will still retrieve users in batches,
            // buffering no more than 1000 users in memory at a time.
            var enumerator = FirebaseAuth.DefaultInstance.ListUsersAsync(null).GetEnumerator();
            while (await enumerator.MoveNext())
            {
                ExportedUserRecord user = enumerator.Current;
                Console.WriteLine($"User: {user.Uid}");
            }

            // [END list_all_users]
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

        internal static async Task SetCustomUserClaimsScriptAsync()
        {
            // [START set_custom_user_claims_script]
            UserRecord user = await FirebaseAuth.DefaultInstance
                .GetUserByEmailAsync("user@admin.example.com");
            // Confirm user is verified.
            if (user.EmailVerified)
            {
                var claims = new Dictionary<string, object>()
                {
                    { "admin", true },
                };
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, claims);
            }

            // [END set_custom_user_claims_script]
        }

        internal static async Task SetCustomUserClaimsIncrementalAsync()
        {
            // [START set_custom_user_claims_incremental]
            UserRecord user = await FirebaseAuth.DefaultInstance
                .GetUserByEmailAsync("user@admin.example.com");
            // Add incremental custom claims without overwriting the existing claims.
            object isAdmin;
            if (user.CustomClaims.TryGetValue("admin", out isAdmin) && (bool)isAdmin)
            {
                var claims = new Dictionary<string, object>(user.CustomClaims);
                // Add level.
                claims["level"] = 10;
                // Add custom claims for additional privileges.
                await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync(user.Uid, claims);
            }

            // [END set_custom_user_claims_incremental]
        }

        internal static ActionCodeSettings InitActionCodeSettings()
        {
            // [START init_action_code_settings]
            var actionCodeSettings = new ActionCodeSettings()
            {
                Url = "https://www.example.com/checkout?cartId=1234",
                HandleCodeInApp = true,
                IosBundleId = "com.example.ios",
                AndroidPackageName = "com.example.android",
                AndroidInstallApp = true,
                AndroidMinimumVersion = "12",
                DynamicLinkDomain = "coolapp.page.link",
            };
            // [END init_action_code_settings]
            return actionCodeSettings;
        }

        internal static async Task GeneratePasswordResetLink()
        {
            var actionCodeSettings = InitActionCodeSettings();
            var displayName = "Example User";
            // [START password_reset_link]
            var email = "user@example.com";
            var link = await FirebaseAuth.DefaultInstance.GeneratePasswordResetLinkAsync(
                email, actionCodeSettings);
            // Construct email verification template, embed the link and send
            // using custom SMTP server.
            SendCustomEmail(email, displayName, link);
            // [END password_reset_link]
        }

        internal static async Task GenerateEmailVerificationLink()
        {
            var actionCodeSettings = InitActionCodeSettings();
            var displayName = "Example User";
            // [START email_verification_link]
            var email = "user@example.com";
            var link = await FirebaseAuth.DefaultInstance.GenerateEmailVerificationLinkAsync(
                email, actionCodeSettings);
            // Construct email verification template, embed the link and send
            // using custom SMTP server.
            SendCustomEmail(email, displayName, link);
            // [END email_verification_link]
        }

        internal static async Task GenerateSignInWithEmailLink()
        {
            var actionCodeSettings = InitActionCodeSettings();
            var displayName = "Example User";
            // [START sign_in_with_email_link]
            var email = "user@example.com";
            var link = await FirebaseAuth.DefaultInstance.GenerateSignInWithEmailLinkAsync(
                email, actionCodeSettings);
            // Construct email verification template, embed the link and send
            // using custom SMTP server.
            SendCustomEmail(email, displayName, link);
            // [END sign_in_with_email_link]
        }

        // Place holder method to make the compiler happy. This is referenced by all email action
        // link snippets.
        private static void SendCustomEmail(String email, String displayName, String link) {}
    }
}
