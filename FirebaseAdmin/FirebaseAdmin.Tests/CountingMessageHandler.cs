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

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Tests
{
    /// <summary>
    /// An <see cref="HttpMessageHandler"/> implementation that counts the number of requests
    /// processed.
    /// </summary>
    internal abstract class CountingMessageHandler : HttpMessageHandler
    {
        private int calls;

        public int Calls
        {
            get => this.calls;
        }

        protected sealed override Task<HttpResponseMessage> SendAsync(
          HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var count = Interlocked.Increment(ref this.calls);
            return this.DoSendAsync(request, count, cancellationToken);
        }

        protected abstract Task<HttpResponseMessage> DoSendAsync(
            HttpRequestMessage request, int count, CancellationToken cancellationToken);
    }
}