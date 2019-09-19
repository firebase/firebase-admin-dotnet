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
    internal sealed class RetryOptions
    {
        public int MaxRetries { get; set; }

        public TimeSpan MaxTimeSpan { get; set; }

        public Func<HttpResponseMessage, bool> HandleUnsuccessfulResponseFunc { get; set; }

        public Func<Exception, bool> HandleExceptionFunc { get; set; }

        public IWaiter Waiter { get; set; }

        public IClock Clock { get; set; }

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
