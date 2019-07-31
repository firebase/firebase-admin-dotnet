// Copyright 2019, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// The response produced by FCM topic management operations.
    /// </summary>
    public sealed class TopicManagementResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TopicManagementResponse"/> class.
        /// </summary>
        /// <param name="instanceIdServiceResponse">The results from the response produced by FCM topic management operations.</param>
        internal TopicManagementResponse(InstanceIdServiceResponse instanceIdServiceResponse)
        {
            if (instanceIdServiceResponse == null)
            {
                throw new ArgumentNullException("Unexpected null response from topic management service");
            }

            if (instanceIdServiceResponse.ResultCount == 0)
            {
                throw new ArgumentNullException("Unexpected empty response from topic management service");
            }

            var resultErrors = new List<ErrorInfo>();
            for (var i = 0; i < instanceIdServiceResponse.Results.Count; i++)
            {
                var result = instanceIdServiceResponse.Results[i];
                if (result.HasError)
                {
                    resultErrors.Add(new ErrorInfo(i, result.Error));
                }
                else
                {
                    this.SuccessCount++;
                }
            }

            this.Errors = resultErrors;
        }

        /// <summary>
        /// Gets the number of registration tokens that were successfully subscribed or unsubscribed.
        /// </summary>
        /// <returns>The number of registration tokens that were successfully subscribed or unsubscribed.</returns>
        public int SuccessCount { get; private set; }

        /// <summary>
        /// Gets the number of registration tokens that could not be subscribed or unsubscribed, and resulted in an error.
        /// </summary>
        /// <returns>The number of failures.</returns>
        public int FailureCount => this.Errors.Count;

        /// <summary>
        /// Gets a list of errors encountered while executing the topic management operation.
        /// </summary>
        /// <returns>A non-null list.</returns>
        public IReadOnlyList<ErrorInfo> Errors { get; private set; }
    }
}
