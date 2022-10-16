// Copyright 2022, Google Inc. All rights reserved.
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
    /// Service to send messages to the FCM service for delivery.
    /// </summary>
    public interface IFirebaseMessageSender
    {
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
        Task<string> SendAsync(Message message);

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
        Task<string> SendAsync(Message message, CancellationToken cancellationToken);

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
        Task<string> SendAsync(Message message, bool dryRun);

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
        Task<string> SendAsync(
            Message message, bool dryRun, CancellationToken cancellationToken);

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="FirebaseMessaging.SendAsync(FirebaseAdmin.Messaging.Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages);

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="FirebaseMessaging.SendAsync(FirebaseAdmin.Messaging.Message)"/>
        /// method, this is a significantly more efficient way to send multiple messages.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="messages">Up to 100 messages to send in the batch. Cannot be null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, CancellationToken cancellationToken);

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="FirebaseMessaging.SendAsync(FirebaseAdmin.Messaging.Message)"/>
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
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun);

        /// <summary>
        /// Sends all the messages in the given list via Firebase Cloud Messaging. Employs batching to
        /// send the entire list as a single RPC call. Compared to the <see cref="FirebaseMessaging.SendAsync(FirebaseAdmin.Messaging.Message)"/>
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
        Task<BatchResponse> SendAllAsync(IEnumerable<Message> messages, bool dryRun, CancellationToken cancellationToken);

        /// <summary>
        /// Sends the given multicast message to all the FCM registration tokens specified in it.
        /// </summary>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// messages.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <returns>A <see cref="BatchResponse"/> containing details of the batch operation's
        /// outcome.</returns>
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message);

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
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, CancellationToken cancellationToken);

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
        Task<BatchResponse> SendMulticastAsync(MulticastMessage message, bool dryRun);

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
        Task<BatchResponse> SendMulticastAsync(
            MulticastMessage message, bool dryRun, CancellationToken cancellationToken);
    }
}
