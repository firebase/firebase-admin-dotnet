using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Tests.AppCheck
{
    internal abstract class CountingAppCheckHandler : HttpMessageHandler
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
