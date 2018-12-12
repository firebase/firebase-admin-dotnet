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
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// This is the entry point to all server-side Firebase Cloud Messaging (FCM) operations. You
    /// can get an instance of this class via <c>FirebaseMessaging.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseMessaging: IFirebaseService
    {
        private readonly FirebaseMessagingClient _messagingClient;

        private FirebaseMessaging(FirebaseApp app)
        {
            _messagingClient = new FirebaseMessagingClient(
                new HttpClientFactory(), app.Options.Credential, app.GetProjectId());
        }

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        public async Task<string> SendAsync(Message message)
        {
            return await SendAsync(message, false);
        }

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> SendAsync(Message message, CancellationToken cancellationToken)
        {
            return await SendAsync(message, false, cancellationToken);
        }

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// <para>If the <paramref name="dryRun"/> option is set to true, the message will not be
        /// actually sent to the recipients. Instead, the FCM service performs all the necessary
        /// validations, and emulates the send operation. This is a good way to check if a
        /// certain message will be accepted by FCM for delivery.</para>
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        public async Task<string> SendAsync(Message message, bool dryRun)
        {
            return await SendAsync(message, dryRun, default(CancellationToken));
        }

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// <para>If the <paramref name="dryRun"/> option is set to true, the message will not be
        /// actually sent to the recipients. Instead, the FCM service performs all the necessary
        /// validations, and emulates the send operation. This is a good way to check if a
        /// certain message will be accepted by FCM for delivery.</para>
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> SendAsync(
            Message message, bool dryRun, CancellationToken cancellationToken)
        {
            return await _messagingClient.SendAsync(
                message, dryRun, cancellationToken).ConfigureAwait(false);
        }

        void IFirebaseService.Delete()
        {
            _messagingClient.Dispose();
        }

        /// <summary>
        /// The messaging instance associated with the default Firebase app. This property is
        /// <c>null</c> if the default app doesn't yet exist.
        /// </summary>
        public static FirebaseMessaging DefaultInstance
        {
            get
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    return null;
                }
                return GetMessaging(app);
            }
        }

        /// <summary>
        /// Returns the messaging instance for the specified app.
        /// </summary>
        /// <returns>The <see cref="FirebaseMessaging"/> instance associated with the specified
        /// app.</returns>
        /// <exception cref="System.ArgumentNullException">If the app argument is null.</exception>
        /// <param name="app">An app instance.</param>
        public static FirebaseMessaging GetMessaging(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }
            return app.GetOrInit<FirebaseMessaging>(typeof(FirebaseMessaging).Name, () => 
            {
                return new FirebaseMessaging(app);
            });
        }
    }
}
