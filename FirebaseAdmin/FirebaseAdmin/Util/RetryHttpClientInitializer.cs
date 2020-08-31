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
    /// An HTTP client initializer that configures clients to retry failing HTTP requests on
    /// low-level exceptions and unsuccessful HTTP responses. Retry conditions and other parameters
    /// are configured via <see cref="RetryOptions"/>. Supports exponential back-off and the
    /// "Retry-After" header in error responses.
    /// </summary>
    internal sealed class RetryHttpClientInitializer : IConfigurableHttpClientInitializer
    {
        private readonly RetryOptions retryOptions;
        private readonly BackOffHandler backOffHandler;

        internal RetryHttpClientInitializer(
            RetryOptions retryOptions, IClock clock = null, IWaiter waiter = null)
        {
            this.retryOptions = retryOptions.ThrowIfNull(nameof(retryOptions));
            this.backOffHandler = new RetryAfterAwareBackOffHandler(
                this.retryOptions, clock, waiter);
        }

        public void Initialize(ConfigurableHttpClient client)
        {
            client.MessageHandler.AddExceptionHandler(this.backOffHandler);
            client.MessageHandler.AddUnsuccessfulResponseHandler(this.backOffHandler);

            // NumTries is the global setting that controls how many times the
            // ConfigurableMessageHandler is willing to retry a request. This defaults to 3 in the
            // Google API client and does not honor the ExponentialBackOff.MaxRetries setting. See
            // https://github.com/googleapis/google-api-dotnet-client/issues/1461.
            client.MessageHandler.NumTries = this.retryOptions.MaxRetries + 1;
        }

        /// <summary>
        /// A <see cref="BackOffHandler"/> that retries failing HTTP requests with exponential back-off
        /// If an HTTP error response contains the "Retry-After" header, the delay indicated in the
        /// header takes precedence over exponential back-off. If the delay indicated in the header
        /// is longer than <see cref="BackOffHandler.MaxTimeSpan"/>, no retries are performed.
        /// </summary>
        private sealed class RetryAfterAwareBackOffHandler : BackOffHandler
        {
            private readonly IClock clock;
            private readonly IWaiter waiter;

            internal RetryAfterAwareBackOffHandler(
                RetryOptions retryOptions, IClock clock = null, IWaiter waiter = null)
            : base(CreateInitializer(retryOptions))
            {
                this.clock = clock ?? SystemClock.Default;
                this.waiter = waiter;
            }

            public override async Task<bool> HandleResponseAsync(
                HandleUnsuccessfulResponseArgs args)
            {
                // if the func returns true try to handle this current failed try
                if (this.IsRetryEligible(args))
                {
                    var delay = this.GetDelayFromResponse(args.Response);

                    // Retry-After header can specify very long delay intervals (e.g. 24 hours). If
                    // we cannot wait that long, we should not perform any retries at all. In
                    // general it is not correct to retry earlier than what the server has
                    // recommended to us.
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
                    await this.waiter.Wait(ts, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await base.Wait(ts, cancellationToken).ConfigureAwait(false);
                }
            }

            private static Initializer CreateInitializer(RetryOptions retryOptions)
            {
                var backOff = new FactoredExponentialBackOff(
                    TimeSpan.Zero, retryOptions.MaxRetries, retryOptions.BackOffFactor);
                return new Initializer(backOff)
                {
                    MaxTimeSpan = retryOptions.MaxTimeSpan,
                    HandleExceptionFunc = retryOptions.HandleExceptionFunc,
                    HandleUnsuccessfulResponseFunc = retryOptions.HandleUnsuccessfulResponseFunc,
                };
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

        /// <summary>
        /// An <see cref="IBackOff"/> that multiplies the back-off durations produced by
        /// <see cref="ExponentialBackOff"/> with a constant factor. The factor can be set to 0
        /// to disable exponential back-off.
        /// </summary>
        private sealed class FactoredExponentialBackOff : IBackOff
        {
            private readonly ExponentialBackOff expBackOff;
            private readonly double factor;

            internal FactoredExponentialBackOff(
                TimeSpan deltaBackOff, int maximumNumOfRetries, double factor = 1.0)
            {
                if (factor < 0)
                {
                    throw new ArgumentException("Factor must not be negative.");
                }

                this.expBackOff = new ExponentialBackOff(deltaBackOff, maximumNumOfRetries);
                this.factor = factor;
            }

            public int MaxNumOfRetries
            {
                get => this.expBackOff.MaxNumOfRetries;
            }

            public TimeSpan GetNextBackOff(int currentRetry)
            {
                var timeSpan = this.expBackOff.GetNextBackOff(currentRetry);
                return TimeSpan.FromMilliseconds(timeSpan.TotalMilliseconds * this.factor);
            }
        }
    }
}
