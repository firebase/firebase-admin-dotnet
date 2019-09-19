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
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An interface that enables controlling how wait operations are implemented.
    /// This is mainly useful during unit testing.
    /// </summary>
    internal interface IWaiter
    {
        /// <summary>
        /// Waits for the specified time span.
        /// </summary>
        /// <param name="ts">Time span to wait for.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes when the wait is over.</returns>
        Task Wait(TimeSpan ts, CancellationToken cancellationToken);
    }
}
