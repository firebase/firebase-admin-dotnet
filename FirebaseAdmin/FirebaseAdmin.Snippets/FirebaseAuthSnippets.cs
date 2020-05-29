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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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

        internal static async Task GetUsersAsync()
        {
            // [START bulk_get_users]
            GetUsersResult result = await FirebaseAuth.DefaultInstance.GetUsersAsync(
                new List<UserIdentifier>
                {
                    new UidIdentifier("uid1"),
                    new EmailIdentifier("user2@example.com"),
                    new PhoneIdentifier("+15555550003"),
                    new ProviderIdentifier("google.com", "google_uid4")
                });
            
            Console.WriteLine("Successfully fetched user data:");
            foreach (UserRecord user in result.Users)
            {
                Console.WriteLine($"User: {user.Uid}");
            }

            Console.WriteLine("Unable to find users corresponding to these identifiers:");
            foreach (UserIdentifier uid in result.NotFound)
            {
                Console.WriteLine($"{uid}");
            }
            // [END bulk_get_users]
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

        internal static async Task DeleteUsersAsync()
        {
            // [START bulk_delete_users]
            DeleteUsersResult result = await FirebaseAuth.DefaultInstance.DeleteUsersAsync(new List<string>
                {
                    "uid1",
                    "uid2",
                    "uid3",
                });
            
            Console.WriteLine($"Successfully deleted {result.SuccessCount} users.");
            Console.WriteLine($"Failed to delete {result.FailureCount} users.");

            foreach (ErrorInfo err in result.Errors)
            {
                Console.WriteLine($"Error #{err.Index}, reason: {err.Message}");
            }
            // [END bulk_delete_users]
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

        internal static async Task RevokeIdTokens(string idToken)
        {
            string uid = "someUid";
            // [START revoke_tokens]
            await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(uid);
            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            Console.WriteLine("Tokens revoked at: " + user.TokensValidAfterTimestamp);
            // [END revoke_tokens]
        }

        internal static async Task VerifyIdTokenCheckRevoked(string idToken)
        {
            // [START verify_id_token_check_revoked]
            try
            {
                // Verify the ID token while checking if the token is revoked by passing checkRevoked
                // as true.
                bool checkRevoked = true;
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(
                    idToken, checkRevoked);
                // Token is valid and not revoked.
                string uid = decodedToken.Uid;
            }
            catch (FirebaseAuthException ex)
            {
                if (ex.AuthErrorCode == AuthErrorCode.RevokedIdToken)
                {
                    // Token has been revoked. Inform the user to re-authenticate or signOut() the user.
                }
                else
                {
                    // Token is invalid.
                }
            }

            // [END verify_id_token_check_revoked]
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
        private static void SendCustomEmail(string email, string displayName, string link) { }

        public class LoginRequest
        {
            public string IdToken { get; set; }
        }

        public class SessionCookieSnippets : ControllerBase
        {
            // [START session_login]
            // POST: /sessionLogin
            [HttpPost]
            public async Task<ActionResult> Login([FromBody] LoginRequest request)
            {
                // Set session expiration to 5 days.
                var options = new SessionCookieOptions()
                {
                    ExpiresIn = TimeSpan.FromDays(5),
                };

                try
                {
                    // Create the session cookie. This will also verify the ID token in the process.
                    // The session cookie will have the same claims as the ID token.
                    var sessionCookie = await FirebaseAuth.DefaultInstance
                        .CreateSessionCookieAsync(request.IdToken, options);

                    // Set cookie policy parameters as required.
                    var cookieOptions = new CookieOptions()
                    {
                        Expires = DateTimeOffset.UtcNow.Add(options.ExpiresIn),
                        HttpOnly = true,
                        Secure = true,
                    };
                    this.Response.Cookies.Append("session", sessionCookie, cookieOptions);
                    return this.Ok();
                }
                catch (FirebaseAuthException)
                {
                    return this.Unauthorized("Failed to create a session cookie");
                }
            }

            // [END session_login]

            // [START session_verify]
            // POST: /profile
            [HttpPost]
            public async Task<ActionResult> Profile()
            {
                var sessionCookie = this.Request.Cookies["session"];
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    // Session cookie is not available. Force user to login.
                    return this.Redirect("/login");
                }

                try
                {
                    // Verify the session cookie. In this case an additional check is added to detect
                    // if the user's Firebase session was revoked, user deleted/disabled, etc.
                    var checkRevoked = true;
                    var decodedToken = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(
                        sessionCookie, checkRevoked);
                    return ViewContentForUser(decodedToken);
                }
                catch (FirebaseAuthException)
                {
                    // Session cookie is invalid or revoked. Force user to login.
                    return this.Redirect("/login");
                }
            }

            // [END session_verify]

            // [START session_clear]
            // POST: /sessionLogout
            [HttpPost]
            public ActionResult ClearSessionCookie()
            {
                this.Response.Cookies.Delete("session");
                return this.Redirect("/login");
            }

            // [END session_clear]

            // [START session_clear_and_revoke]
            // POST: /sessionLogout
            [HttpPost]
            public async Task<ActionResult> ClearSessionCookieAndRevoke()
            {
                var sessionCookie = this.Request.Cookies["session"];
                try
                {
                    var decodedToken = await FirebaseAuth.DefaultInstance
                        .VerifySessionCookieAsync(sessionCookie);
                    await FirebaseAuth.DefaultInstance.RevokeRefreshTokensAsync(decodedToken.Uid);
                    this.Response.Cookies.Delete("session");
                    return this.Redirect("/login");
                }
                catch (FirebaseAuthException)
                {
                    return this.Redirect("/login");
                }
            }

            // [END session_clear_and_revoke]

            internal async Task<ActionResult> CheckAuthTime(string idToken)
            {
                // [START check_auth_time]
                // To ensure that cookies are set only on recently signed in users, check auth_time in
                // ID token before creating a cookie.
                var decodedToken = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                var authTime = new DateTime(1970, 1, 1).AddSeconds(
                    (long)decodedToken.Claims["auth_time"]);

                // Only process if the user signed in within the last 5 minutes.
                if (DateTime.UtcNow - authTime < TimeSpan.FromMinutes(5))
                {
                    var options = new SessionCookieOptions()
                    {
                        ExpiresIn = TimeSpan.FromDays(5),
                    };
                    var sessionCookie = await FirebaseAuth.DefaultInstance.CreateSessionCookieAsync(
                        idToken, options);
                    // Set cookie policy parameters as required.
                    this.Response.Cookies.Append("session", sessionCookie);
                    return this.Ok();
                }

                // User did not sign in recently. To guard against ID token theft, require
                // re-authentication.
                return this.Unauthorized("Recent sign in required");
                // [END check_auth_time]
            }

            internal async Task<ActionResult> CheckPermissions(string sessionCookie)
            {
                // [START session_verify_with_permission_check]
                try
                {
                    var checkRevoked = true;
                    var decodedToken = await FirebaseAuth.DefaultInstance.VerifySessionCookieAsync(
                        sessionCookie, checkRevoked);
                    object isAdmin;
                    if (decodedToken.Claims.TryGetValue("admin", out isAdmin) && (bool)isAdmin)
                    {
                        return ViewContentForAdmin(decodedToken);
                    }

                    return this.Unauthorized("Insufficient permissions");
                }
                catch (FirebaseAuthException)
                {
                    // Session cookie is invalid or revoked. Force user to login.
                    return this.Redirect("/login");
                }

                // [END session_verify_with_permission_check]
            }

            private static ActionResult ViewContentForUser(FirebaseToken token)
            {
                return null;
            }

            private static ActionResult ViewContentForAdmin(FirebaseToken token)
            {
                return null;
            }
        }
    }
}
