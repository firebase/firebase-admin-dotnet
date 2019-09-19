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
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// A <see cref="BackOffHandler"/> that retries failing HTTP requests with exponential back-off
    /// If an HTTP error response contains the "Retry-After" header, the delay indicated in the
    /// header takes precedence over exponential back-off. If the delay indicated in the header
    /// is longer than <see cref="BackOffHandler.MaxTimeSpan"/>, no retries are performed.
    /// </summary>
    internal sealed class FirebaseBackOffHandler : BackOffHandler
    {
        private readonly IClock clock;
        private readonly IWaiter waiter;

        internal FirebaseBackOffHandler(RetryOptions retryOptions)
        : base(retryOptions.CreateInitializer())
        {
            this.clock = retryOptions.Clock ?? SystemClock.Default;
            this.waiter = retryOptions.Waiter;
        }

        public override async Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            // if the func returns true try to handle this current failed try
            if (this.IsRetryEligible(args))
            {
                var delay = this.GetDelayFromResponse(args.Response);

                // Retry-After header can specify very long delay intervals (e.g. 24 hours). If we
                // cannot wait that long, we should not perform any retries at all. In general it
                // is not correct to retry earlier than what the server has recommended to us.
                if (delay > this.MaxTimeSpan)
                {
                    return false;
                }
                else if (delay > TimeSpan.Zero)
                {
                    await this.Wait(delay, args.CancellationToken).ConfigureAwait(false);
                    return true;
                }
            }

            return await base.HandleResponseAsync(args).ConfigureAwait(false);
        }

        protected override async Task Wait(TimeSpan ts, CancellationToken cancellationToken)
        {
            if (this.waiter != null)
            {
                await this.waiter.Wait(ts, cancellationToken);
            }
            else
            {
                await base.Wait(ts, cancellationToken);
            }
        }

        private bool IsRetryEligible(HandleUnsuccessfulResponseArgs args)
        {
            return this.HandleUnsuccessfulResponseFunc != null &&
                this.HandleUnsuccessfulResponseFunc(args.Response) &&
                args.SupportsRetry &&
                args.CurrentFailedTry <= this.BackOff.MaxNumOfRetries;
        }

        private TimeSpan GetDelayFromResponse(HttpResponseMessage response)
        {
            var retryAfter = response.Headers.RetryAfter;
            if (retryAfter == null)
            {
                return TimeSpan.Zero;
            }

            var date = retryAfter.Date;
            if (date.HasValue)
            {
                return date.Value.UtcDateTime.Subtract(this.clock.UtcNow);
            }

            return retryAfter.Delta ?? TimeSpan.Zero;
        }
    }
}
