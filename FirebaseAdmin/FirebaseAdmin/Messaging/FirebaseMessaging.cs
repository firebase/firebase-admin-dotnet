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
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// This is the entry point to all server-side Firebase Cloud Messaging (FCM) operations. You
    /// can get an instance of this class via <c>FirebaseMessaging.DefaultInstance</c>.
    /// </summary>
    public sealed class FirebaseMessaging : IFirebaseService, IFirebaseMessageSender, IFirebaseMessageSubscriber
    {
        private readonly FirebaseMessagingClient messagingClient;
        private readonly InstanceIdClient instanceIdClient;

        private FirebaseMessaging(FirebaseApp app)
        {
            this.messagingClient = FirebaseMessagingClient.Create(app);
            this.instanceIdClient = InstanceIdClient.Create(app);
        }

        /// <summary>
        /// Gets the messaging instance associated with the default Firebase app. This property is
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

        /// <inheritdoc />
        public async Task<string> SendAsync(Message message)
        {
            return await this.SendAsync(message, false)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> SendAsync(Message message, CancellationToken cancellationToken)
        {
            return await this.SendAsync(message, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> SendAsync(Message message, bool dryRun)
        {
            return await this.SendAsync(message, dryRun, default(CancellationToken))
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> SendAsync(
            Message message, bool dryRun, CancellationToken cancellationToken)
        {
            return await this.messagingClient.SendAsync(
                message, dryRun, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages)
        {
            return await this.SendAllAsync(messages, false)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return await this.SendAllAsync(messages, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun)
        {
            return await this.SendAllAsync(messages, dryRun, default)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun, CancellationToken cancellationToken)
        {
            return await this.messagingClient.SendAllAsync(messages, dryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message)
        {
            return await this.SendMulticastAsync(message, false)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message, CancellationToken cancellationToken)
        {
            return await this.SendMulticastAsync(message, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun)
        {
            return await this.SendMulticastAsync(message, dryRun, default)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<BatchResponse> SendMulticastAsync(
            MulticastMessage message, bool dryRun, CancellationToken cancellationToken)
        {
            return await this.SendAllAsync(
                message.GetMessageList(), dryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TopicManagementResponse> SubscribeToTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic)
        {
            return await this.instanceIdClient.SubscribeToTopicAsync(registrationTokens, topic)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TopicManagementResponse> UnsubscribeFromTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic)
        {
            return await this.instanceIdClient.UnsubscribeFromTopicAsync(registrationTokens, topic)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseMessaging"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            this.messagingClient.Dispose();
            this.instanceIdClient.Dispose();
        }
    }
}
