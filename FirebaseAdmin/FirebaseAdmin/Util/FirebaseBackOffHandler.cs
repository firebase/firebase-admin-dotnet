using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Util;

namespace FirebaseAdmin.Util
{
    internal class FirebaseBackOffHandler : BackOffHandler
    {
        private static readonly IBackOff DefaultBackOff = new ExponentialBackOff(TimeSpan.Zero, 4);
        private static readonly Initializer DefaultInitializer = new Initializer(DefaultBackOff)
        {
            MaxTimeSpan = TimeSpan.FromMinutes(1),
        };

        internal FirebaseBackOffHandler(Initializer initializer)
        : base(initializer) { }

        internal FirebaseBackOffHandler()
        : this(DefaultInitializer) { }

        public override async Task<bool> HandleResponseAsync(HandleUnsuccessfulResponseArgs args)
        {
            // if the func returns true try to handle this current failed try
            if (this.HandleUnsuccessfulResponseFunc != null && this.HandleUnsuccessfulResponseFunc(args.Response))
            {
                if (!args.SupportsRetry || this.BackOff.MaxNumOfRetries < args.CurrentFailedTry)
                {
                    return false;
                }

                var retryAfter = args.Response.Headers.RetryAfter;
                var timeSpan = retryAfter?.Delta ?? TimeSpan.Zero;
                if (timeSpan > this.MaxTimeSpan)
                {
                    return false;
                }
                else if (timeSpan > TimeSpan.Zero)
                {
                    await this.Wait(timeSpan, args.CancellationToken).ConfigureAwait(false);
                    return true;
                }
            }

            return await base.HandleResponseAsync(args).ConfigureAwait(false);
        }
    }
}
