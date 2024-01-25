using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// App Check Token generator.
    /// </summary>
    internal interface IAppCheckTokenGenerator
    {
        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature.
        /// </summary>
        /// <param name="app">Appcheck Generate Token.</param>
        /// <returns>AppCheckTokenVerify.</returns>
        AppCheckTokenGenerator Create(FirebaseApp app);

        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature.
        /// </summary>
        /// <param name="appId">The Id of FirebaseApp.</param>
        /// <param name="options">AppCheck Token Option.</param>
        /// <param name="cancellationToken">Cancelation Token.</param>
        /// <returns>A <see cref="Task{FirebaseToken}"/> representing the result of the asynchronous operation.</returns>
        Task<string> CreateCustomTokenAsync(
            string appId,
            AppCheckTokenOptions options = null,
            CancellationToken cancellationToken = default);
    }
}
