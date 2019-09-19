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
using System.Net.Http;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// Options for customizing how failing HTTP requests should be retried.
    /// </summary>
    internal sealed class RetryOptions
    {
        internal static RetryOptions Default
        {
            get
            {
                return new RetryOptions()
                {
                    MaxRetries = 4,
                    MaxTimeSpan = TimeSpan.FromSeconds(30),

                    // Retry on all exceptions except TaskCancelledException and
                    // OperationCancelledException.
                    HandleExceptionFunc = BackOffHandler.Initializer.DefaultHandleExceptionFunc,

                    // Retry 503 errors.
                    HandleUnsuccessfulResponseFunc =
                        BackOffHandler.Initializer.DefaultHandleUnsuccessfulResponseFunc,
                };
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of retry attempts.
        /// </summary>
        internal int MaxRetries { get; set; }

        /// <summary>
        /// Gets or sets the maximum time span for wait for a retry.
        /// </summary>
        internal TimeSpan MaxTimeSpan { get; set; }

        /// <summary>
        /// Gets or sets the function that determines which HTTP responses should be retried.
        /// If not specified, unsuccessful responses are not retried.
        /// </summary>
        internal Func<HttpResponseMessage, bool> HandleUnsuccessfulResponseFunc { get; set; }

        /// <summary>
        /// Gets or sets the function that determines which exceptions should be retried.
        /// If not specified, exceptions are not retried.
        /// </summary>
        internal Func<Exception, bool> HandleExceptionFunc { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IWaiter"/> that determines how delay operations are
        /// performed. Useful for testing. If not specified, implements delays via the Task
        /// API.
        /// </summary>
        internal IWaiter Waiter { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="IClock"/> that is used to determine current system time.
        /// This is used when comparing timestamps sent in the "Retry-After" header.
        /// </summary>
        internal IClock Clock { get; set; }

        internal BackOffHandler.Initializer CreateInitializer()
        {
            var backOff = new ExponentialBackOff(TimeSpan.Zero, this.MaxRetries);
            return new BackOffHandler.Initializer(backOff)
            {
                MaxTimeSpan = this.MaxTimeSpan,
                HandleExceptionFunc = this.HandleExceptionFunc,
                HandleUnsuccessfulResponseFunc = this.HandleUnsuccessfulResponseFunc,
            };
        }
    }
}
