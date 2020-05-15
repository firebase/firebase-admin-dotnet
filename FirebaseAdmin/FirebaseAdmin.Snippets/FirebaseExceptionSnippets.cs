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
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Messaging;

namespace FirebaseAdmin.Snippets
{
    internal class FirebaseExceptionSnippets
    {
        internal static async Task GenericErrorHandler(string idToken)
        {
            // [START generic_error_handler]
            try
            {
                var token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                PerformPrivilegedOperation(token.Uid);
            }
            catch (FirebaseAuthException ex)
            {
                // A generic error handler that logs the error message, and ignores all
                // other properties.
                Console.WriteLine($"Failed to verify ID token: {ex.Message}");
            }

            // [END generic_error_handler]
        }

        internal static async Task ServiceErrorCode(string idToken)
        {
            // [START service_error_code]
            try
            {
                var token = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                PerformPrivilegedOperation(token.Uid);
            }
            catch (FirebaseAuthException ex)
            {
                // Some exception types expose a service-specific error code. Applications can
                // implement custom error handling logic based on these error codes.
                if (ex.AuthErrorCode == AuthErrorCode.ExpiredIdToken)
                {
                    Console.WriteLine("ID token has expired");
                }
                else if (ex.AuthErrorCode == AuthErrorCode.InvalidIdToken)
                {
                    Console.WriteLine("ID token is malformed or invalid");
                }
                else
                {
                    Console.WriteLine($"Failed to verify ID token: {ex.Message}");
                }
            }

            // [END service_error_code]
        }

        internal static async Task PlatformErrorCode(string deviceToken)
        {
            // [START platform_error_code]
            var notification = CreateNotification(deviceToken);
            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(notification);
            }
            catch (FirebaseMessagingException ex)
            {
                // All exceptions contain a platform-level error code. Applications can inspect
                // both the platform-level error code and any service-level error codes when
                // implementing error handling logic.
                if (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
                {
                    // Service-level error code
                    Console.WriteLine("App instance has been unregistered");
                    RemoveTokenFromDatabase(deviceToken);
                }
                else if (ex.ErrorCode == ErrorCode.Unavailable)
                {
                    // Platform-level error code
                    Console.WriteLine("FCM service is temporarily unavailable");
                    ScheduleForRetry(notification, TimeSpan.FromHours(1));
                }
                else
                {
                    Console.WriteLine($"Failed to send notification: {ex.Message}");
                }
            }

            // [END platform_error_code]
        }

        internal static async Task HttpResponse(string deviceToken)
        {
            // [START http_response]
            var notification = CreateNotification(deviceToken);
            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(notification);
            }
            catch (FirebaseMessagingException ex)
            {
                // If the exception was caused by a backend service error, applications can
                // inspect the original error response received from the backend service to
                // implement more advanced error handling behavior.
                var response = ex.HttpResponse;
                if (response != null)
                {
                    Console.WriteLine($"FCM service responded with HTTP {response.StatusCode}");
                    foreach (var entry in response.Headers)
                    {
                        Console.WriteLine($">>> {entry.Key}: {entry.Value}");
                    }

                    var body = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(">>>");
                    Console.WriteLine($">>> {body}");
                }
            }

            // [END http_response]
        }

        private static void PerformPrivilegedOperation(string uid) { }

        private static Message CreateNotification(string deviceToken)
        {
            return new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = "Test notification",
                },
            };
        }

        private static void RemoveTokenFromDatabase(string deviceToken) { }

        private static void ScheduleForRetry(Message message, TimeSpan waitTime) { }
    }
}
