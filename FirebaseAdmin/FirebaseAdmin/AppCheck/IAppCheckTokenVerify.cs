using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// App Check Verify.
    /// </summary>
    internal interface IAppCheckTokenVerify
    {
        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature.
        /// </summary>
        /// <param name="app">Appcheck Generate Token.</param>
        /// <returns>AppCheckTokenVerify.</returns>
        AppCheckTokenVerify Create(FirebaseApp app);

        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature.
        /// </summary>
        /// <param name="token">Appcheck Generate Token.</param>
        /// <param name="cancellationToken">cancellaton Token.</param>
        /// <returns>A <see cref="Task{FirebaseToken}"/> representing the result of the asynchronous operation.</returns>
        Task<FirebaseToken> VerifyTokenAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken));
    }
}
