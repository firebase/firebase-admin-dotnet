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

using System.Collections.Generic;
using System.Linq;
using Google.Apis.Util;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Response from an operation that sends FCM messages to multiple recipients.
    /// See <see cref="FirebaseMessaging.SendMulticastAsync(MulticastMessage)"/>.
    /// </summary>
    public sealed class BatchResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BatchResponse"/> class.
        /// </summary>
        /// <param name="responses">The responses.</param>
        internal BatchResponse(IEnumerable<SendResponse> responses)
        {
            responses.ThrowIfNull(nameof(responses));

            this.Responses = new List<SendResponse>(responses);
            this.SuccessCount = responses.Where(response => response.IsSuccess).Count();
        }

        /// <summary>
        /// Gets information about all responses for the batch.
        /// </summary>
        public IReadOnlyList<SendResponse> Responses { get; }

        /// <summary>
        /// Gets a count of how many of the responses in <see cref="Responses"/> were
        /// successful.
        /// </summary>
        public int SuccessCount { get; }

        /// <summary>
        /// Gets a count of how many of the responses in <see cref="Responses"/> were
        /// unsuccessful.
        /// </summary>
        public int FailureCount => this.Responses.Count - this.SuccessCount;
    }
}
