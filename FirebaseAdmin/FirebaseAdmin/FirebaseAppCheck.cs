using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Check;
using Google.Apis.Logging;

namespace FirebaseAdmin
{
    /// <summary>
    /// Asynchronously creates a new Firebase App Check token for the specified Firebase app.
    /// </summary>
    /// <returns>A task that completes with the creation of a new App Check token.</returns>
    /// <exception cref="ArgumentNullException">Thrown if an error occurs while creating the custom token.</exception>
    /// <value>The Firebase app instance.</value>
    public sealed class FirebaseAppCheck
    {
        private const string DefaultProjectId = "[DEFAULT]";
        private static Dictionary<string, FirebaseAppCheck> appChecks = new Dictionary<string, FirebaseAppCheck>();

        private readonly AppCheckApiClient apiClient;
        private readonly FirebaseTokenVerifier appCheckTokenVerifier;
        private readonly FirebaseTokenFactory tokenFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebaseAppCheck"/> class.
        /// </summary>
        /// <param name="value"> Initailize FirebaseApp. </param>
        public FirebaseAppCheck(FirebaseApp value)
        {
            this.apiClient = new AppCheckApiClient(value);
            this.tokenFactory = FirebaseTokenFactory.Create(value);
            this.appCheckTokenVerifier = FirebaseTokenVerifier.CreateAppCheckVerifier(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebaseAppCheck"/> class.
        /// </summary>
        /// <param name="app"> Initailize FirebaseApp. </param>
        /// <returns>A <see cref="Task{FirebaseAppCheck}"/> Representing the result of the asynchronous operation.</returns>
        public static FirebaseAppCheck Create(FirebaseApp app)
        {
            string appId = app.Name;

            if (app == null)
            {
                throw new ArgumentNullException("FirebaseApp must not be null or empty");
            }

            lock (appChecks)
            {
                if (appChecks.ContainsKey(appId))
                {
                    if (appId == DefaultProjectId)
                    {
                        throw new ArgumentException("The default FirebaseAppCheck already exists.");
                    }
                    else
                    {
                        throw new ArgumentException($"FirebaseApp named {appId} already exists.");
                    }
                }
            }

            var appCheck = new FirebaseAppCheck(app);
            appChecks.Add(appId, appCheck);
            return appCheck;
        }

        /// <summary>
        /// Creates a new AppCheckToken that can be sent back to a client.
        /// </summary>
        /// <param name="appId">The app ID to use as the JWT app_id.</param>
        /// <param name="options">Optional options object when creating a new App Check Token.</param>
        /// <returns>A <see cref="Task{AppCheckToken}"/> Representing the result of the asynchronous operation.</returns>
        public async Task<AppCheckToken> CreateToken(string appId, AppCheckTokenOptions options = null)
        {
            string customToken = await this.tokenFactory.CreateCustomTokenAppIdAsync(appId, options)
                .ConfigureAwait(false);

            return await this.apiClient.ExchangeTokenAsync(customToken, appId).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies a Firebase App Check token (JWT). If the token is valid, the promise is
        /// fulfilled with the token's decoded claims; otherwise, the promise is
        /// rejected.
        /// </summary>
        /// <param name="appCheckToken"> TThe App Check token to verify.</param>
        /// <param name="options"> Optional VerifyAppCheckTokenOptions object when verifying an App Check Token.</param>
        /// <returns>A <see cref="Task{VerifyAppCheckTokenResponse}"/> representing the result of the asynchronous operation.</returns>
        public async Task<AppCheckVerifyResponse> VerifyToken(string appCheckToken, VerifyAppCheckTokenOptions options = null)
        {
            if (string.IsNullOrEmpty(appCheckToken))
            {
                throw new ArgumentNullException("App check token " + appCheckToken + " must be a non - empty string.");
            }

            FirebaseToken verifiedToken = await this.appCheckTokenVerifier.VerifyTokenAsync(appCheckToken).ConfigureAwait(false);
            bool alreadyConsumed = await this.apiClient.VerifyReplayProtection(verifiedToken).ConfigureAwait(false);
            AppCheckVerifyResponse result;

            if (!alreadyConsumed)
            {
                result = new AppCheckVerifyResponse(verifiedToken.AppId, verifiedToken, alreadyConsumed);
            }
            else
            {
                result = new AppCheckVerifyResponse(verifiedToken.AppId, verifiedToken);
            }

            return result;
        }

        /// <summary>
        /// Deleted all the appChecks created so far. Used for unit testing.
        /// </summary>
        internal static void DeleteAll()
        {
            FirebaseApp.DeleteAll();
            appChecks.Clear();
        }
    }
}
