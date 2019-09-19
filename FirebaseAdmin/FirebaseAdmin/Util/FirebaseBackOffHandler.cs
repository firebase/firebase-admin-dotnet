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
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    internal class FirebaseBackOffHandler : BackOffHandler
    {
        internal const int MaxRetries = 4;
        private const int MaxTimeSpanSeconds = 30;

        private readonly IClock clock;

        internal FirebaseBackOffHandler(Initializer initializer = null, IClock clock = null)
        : base(initializer ?? CreateDefaultInitializer())
        {
            this.clock = clock ?? SystemClock.Default;
        }

        public override async Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            // if the func returns true try to handle this current failed try
            if (this.HandleUnsuccessfulResponseFunc != null && this.HandleUnsuccessfulResponseFunc(args.Response))
            {
                if (!args.SupportsRetry || this.BackOff.MaxNumOfRetries < args.CurrentFailedTry)
                {
                    return false;
                }

                var delay = this.GetDelayFromResponse(args.Response);
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

        private static Initializer CreateDefaultInitializer()
        {
            var backOff = new ExponentialBackOff(TimeSpan.Zero, MaxRetries);
            return new BackOffHandler.Initializer(backOff)
            {
                MaxTimeSpan = TimeSpan.FromSeconds(MaxTimeSpanSeconds),
            };
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
