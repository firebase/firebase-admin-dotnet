using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Interface of Firebase App Check backend API .
    /// </summary>
    internal interface IAppCheckApiClient
    {
        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="customToken">The custom token to be exchanged.</param>
        /// <param name="appId">The mobile App ID.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public Task<AppCheckToken> ExchangeTokenAsync(string customToken, string appId);

        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="token"> The custom token to be exchanged. </param>
        /// <returns>A alreadyConsumed is true.</returns>
        public Task<bool> VerifyReplayProtection(string token);

        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="customToken"> The custom token to be exchanged. </param>
        /// <param name="appId"> The Id of Firebase App. </param>
        /// <returns> HttpResponseMessage .</returns>
        public Task<HttpResponseMessage> GetExchangeToken(string customToken, string appId);
    }
}
