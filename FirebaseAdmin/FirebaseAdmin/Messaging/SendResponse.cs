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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// The result of an individual send operation that was executed as part of a batch. See
    /// <see cref="BatchResponse"/> for more details.
    /// </summary>
    public sealed class SendResponse
    {
        private SendResponse(string messageId)
        {
            this.MessageId = messageId;
        }

        private SendResponse(FirebaseMessagingException exception)
        {
            this.Exception = exception;
        }

        /// <summary>
        /// Gets a message ID string if the send operation was successful. Otherwise returns null.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets an exception if the send operation failed. Otherwise returns null.
        /// </summary>
        public FirebaseMessagingException Exception { get; }

        /// <summary>
        /// Gets a value indicating whether the send operation was successful or not. When this property
        /// is <c>true</c>, <see cref="MessageId"/> is guaranteed to return a
        /// non-null value. When this property is <c>false</c>,
        /// <see cref="Exception"/> is guaranteed to return a non-null value.
        /// </summary>
        public bool IsSuccess => !string.IsNullOrEmpty(this.MessageId);

        internal static SendResponse FromMessageId(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                throw new ArgumentException($"{nameof(messageId)} must be provided and not an empty string.", nameof(messageId));
            }

            return new SendResponse(messageId);
        }

        internal static SendResponse FromException(FirebaseMessagingException exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new SendResponse(exception);
        }
    }
}
