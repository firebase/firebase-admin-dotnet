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
    /// <see cref="SendResponse"/> for more details.
    /// </summary>
    public sealed class SendItemResponse
    {
        private SendItemResponse(string messageId)
        {
            this.MessageId = messageId;
        }

        private SendItemResponse(FirebaseException exception)
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
        public FirebaseException Exception { get; }

        /// <summary>
        /// Gets a value indicating whether the send operation was successful or not. When this property
        /// is <see langword="true"/>, <see cref="MessageId"/> is guaranteed to return a
        /// non-<see langword="null"/> value. When this property is <see langword="false"/>,
        /// <see cref="Exception"/> is guaranteed to return a non-<see langword="null"/> value.
        /// </summary>
        public bool IsSuccess => !string.IsNullOrEmpty(this.MessageId);

        internal static SendItemResponse FromMessageId(string messageId)
        {
            if (string.IsNullOrEmpty(messageId))
            {
                throw new ArgumentException($"{nameof(messageId)} must be provided and not an empty string.", nameof(messageId));
            }

            return new SendItemResponse(messageId);
        }

        internal static SendItemResponse FromException(FirebaseException exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return new SendItemResponse(exception);
        }
    }
}
