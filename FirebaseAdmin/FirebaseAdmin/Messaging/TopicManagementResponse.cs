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
using Newtonsoft.Json.Linq;

/// <summary>
/// The response produced by FCM topic management operations.
/// </summary>
public class TopicManagementResponse
{
    private readonly int successCount;
    private IReadOnlyList<Error> errors;

    /// <summary>
    /// Initializes a new instance of the <see cref="TopicManagementResponse"/> class.
    /// </summary>
    /// <param name="results">The results from the response produced by FCM topic management operations.</param>
    public TopicManagementResponse(List<JObject> results)
    {
        if (results == null || results.Count == 0)
        {
            throw new ArgumentException("unexpected response from topic management service");
        }

        var resultErrors = new List<Error>();
        for (var i = 0; i < results.Count; i++)
        {
            if (results[i].HasValues)
            {
                resultErrors.Add(new Error(i, results[i].Value<string>("error")));
            }
            else
            {
                this.successCount++;
            }
        }

        this.errors = resultErrors;
    }

    /// <summary>
    /// Gets the number of registration tokens that were successfully subscribed or unsubscribed.
    /// </summary>
    /// <returns>The number of registration tokens that were successfully subscribed or unsubscribed.</returns>
    public int GetSuccessCount()
    {
        return this.successCount;
    }

    /// <summary>
    /// Gets the number of registration tokens that could not be subscribed or unsubscribed, and resulted in an error.
    /// </summary>
    /// <returns>The number of failures.</returns>
    public int GetFailureCount()
    {
        return this.errors.Count;
    }

    /// <summary>
    /// Gets a list of errors encountered while executing the topic management operation.
    /// </summary>
    /// <returns>A non-null list.</returns>
    public IReadOnlyList<Error> GetErrors()
    {
        return this.errors;
    }

    /// <summary>
    /// A topic management error.
    /// </summary>
    public class Error
    {
        // Server error codes as defined in https://developers.google.com/instance-id/reference/server
        // TODO: Should we handle other error codes here (e.g. PERMISSION_DENIED)?
        private readonly IReadOnlyDictionary<string, string> errorCodes;
        private readonly string unknownError = "unknown-error";
        private readonly int index;
        private readonly string reason;

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>
        /// <param name="index">Index of the error in the error codes.</param>
        /// <param name="reason">Reason for the error.</param>
        public Error(int index, string reason)
        {
            this.errorCodes = new Dictionary<string, string>
            {
                { "INVALID_ARGUMENT", "invalid-argument" },
                { "NOT_FOUND", "registration-token-not-registered" },
                { "INTERNAL", "internal-error" },
                { "TOO_MANY_TOPICS", "too-many-topics" },
            };

            this.index = index;
            this.reason = this.errorCodes.ContainsKey(reason)
              ? this.errorCodes[reason] : this.unknownError;
        }

        /// <summary>
        /// Index of the registration token to which this error is related to.
        /// </summary>
        /// <returns>An index into the original registration token list.</returns>
        public int GetIndex()
        {
            return this.index;
        }

        /// <summary>
        /// String describing the nature of the error.
        /// </summary>
        /// <returns>A non-null, non-empty error message.</returns>
        public string GetReason()
        {
            return this.reason;
        }
    }
}