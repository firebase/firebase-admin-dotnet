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
        public TopicManagementResponse(InstanceIdServiceResponse instanceIdServiceResponse)
        {
            if (instanceIdServiceResponse == null || instanceIdServiceResponse.ResultCount == 0)
            {
                throw new ArgumentException("unexpected response from topic management service");
            }

            var resultErrors = new List<Error>();
            for (var i = 0; i < instanceIdServiceResponse.Results.Count; i++)
            {
                var result = instanceIdServiceResponse.Results[i];
                if (result.HasError)
                {
                    resultErrors.Add(new Error(i, result.Error));
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
        public IReadOnlyList<Error> Errors { get; private set; }

        /// <summary>
        /// A topic management error.
        /// </summary>
        public sealed class Error
        {
            // Server error codes as defined in https://developers.google.com/instance-id/reference/server
            // TODO: Should we handle other error codes here (e.g. PERMISSION_DENIED)?
            private static IReadOnlyDictionary<string, string> errorCodes;
            private readonly string unknownError = "unknown-error";

            /// <summary>
            /// Initializes a new instance of the <see cref="Error"/> class.
            /// </summary>
            /// <param name="index">Index of the error in the error codes.</param>
            /// <param name="reason">Reason for the error.</param>
            public Error(int index, string reason)
            {
                errorCodes = new Dictionary<string, string>
                {
                    { "INVALID_ARGUMENT", "invalid-argument" },
                    { "NOT_FOUND", "registration-token-not-registered" },
                    { "INTERNAL", "internal-error" },
                    { "TOO_MANY_TOPICS", "too-many-topics" },
                };

                this.Index = index;
                this.Reason = errorCodes.ContainsKey(reason)
                  ? errorCodes[reason] : this.unknownError;
            }

            /// <summary>
            /// Gets the registration token to which this error is related to.
            /// </summary>
            /// <returns>An index into the original registration token list.</returns>
            public int Index { get; private set; }

            /// <summary>
            /// Gets the nature of the error.
            /// </summary>
            /// <returns>A non-null, non-empty error message.</returns>
            public string Reason { get; private set; }
        }
    }
}
