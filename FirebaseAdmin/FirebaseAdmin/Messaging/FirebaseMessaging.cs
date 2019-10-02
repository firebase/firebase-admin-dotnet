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
    public sealed class FirebaseMessaging : IFirebaseService
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
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        public async Task<string> SendAsync(Message message)
        {
            return await this.SendAsync(message, false)
                .ConfigureAwait(false);
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
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> SendAsync(Message message, CancellationToken cancellationToken)
        {
            return await this.SendAsync(message, false, cancellationToken)
                .ConfigureAwait(false);
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
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        public async Task<string> SendAsync(Message message, bool dryRun)
        {
            return await this.SendAsync(message, dryRun, default(CancellationToken))
                .ConfigureAwait(false);
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
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
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
            return await this.messagingClient.SendAsync(
                message, dryRun, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="SendAsync(Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages)
        {
            return await this.SendAllAsync(messages, false)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="SendAsync(Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            return await this.SendAllAsync(messages, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="SendAsync(Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun)
        {
            return await this.SendAllAsync(messages, dryRun, default)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="SendAsync(Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun, CancellationToken cancellationToken)
        {
            return await this.messagingClient.SendAllAsync(messages, dryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the given multicast message to all the FCM registration tokens specified in it.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message)
        {
            return await this.SendMulticastAsync(message, false)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the given multicast message to all the FCM registration tokens specified in it.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message, CancellationToken cancellationToken)
        {
            return await this.SendMulticastAsync(message, false, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the given multicast message to all the FCM registration tokens specified in it.
        /// <para>If the <paramref name="dryRun"/> option is set to true, the message will not be
        /// actually sent to the recipients. Instead, the FCM service performs all the necessary
        /// validations, and emulates the send operation. This is a good way to check if a
        /// certain message will be accepted by FCM for delivery.</para>
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun)
        {
            return await this.SendMulticastAsync(message, dryRun, default)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Sends the given multicast message to all the FCM registration tokens specified in it.
        /// <para>If the <paramref name="dryRun"/> option is set to true, the message will not be
        /// actually sent to the recipients. Instead, the FCM service performs all the necessary
        /// validations, and emulates the send operation. This is a good way to check if a
        /// certain message will be accepted by FCM for delivery.</para>
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        public async Task<BatchResponse> SendMulticastAsync(
            MulticastMessage message, bool dryRun, CancellationToken cancellationToken)
        {
            return await this.SendAllAsync(
                message.GetMessageList(), dryRun, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribes a list of registration tokens to a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to subscribe.</param>
        /// <param name="topic">The topic name to subscribe to. /topics/ will be prepended to the topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving details about the topic subscription operations.</returns>
        public async Task<TopicManagementResponse> SubscribeToTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic)
        {
            return await this.instanceIdClient.SubscribeToTopicAsync(registrationTokens, topic)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribes a list of registration tokens from a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to unsubscribe.</param>
        /// <param name="topic">The topic name to unsubscribe from. /topics/ will be prepended to the topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving details about the topic unsubscription operations.</returns>
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
