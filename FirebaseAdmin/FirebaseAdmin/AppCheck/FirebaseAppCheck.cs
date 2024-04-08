using System;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Asynchronously creates a new Firebase App Check token for the specified Firebase app.
    /// </summary>
    /// <returns>A task that completes with the creation of a new App Check token.</returns>
    /// <exception cref="ArgumentNullException">Thrown if an error occurs while creating the custom token.</exception>
    /// <value>The Firebase app instance.</value>
    public sealed class FirebaseAppCheck : IFirebaseService
    {
        private readonly AppCheckApiClient appCheckApiClient;
        private readonly AppCheckTokenFactory appCheckTokenFactory;
        private readonly AppCheckTokenVerifier appCheckTokenVerifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="FirebaseAppCheck"/> class.
        /// </summary>
        /// <param name="app"> Initailize FirebaseApp. </param>
        public FirebaseAppCheck(FirebaseApp app)
        {
            this.appCheckApiClient = AppCheckApiClient.Create(app);
            this.appCheckTokenFactory = AppCheckTokenFactory.Create(app);
            this.appCheckTokenVerifier = AppCheckTokenVerifier.Create(app);
        }

        /// <summary>
        /// Gets the messaging instance associated with the default Firebase app. This property is
        /// <c>null</c> if the default app doesn't yet exist.
        /// </summary>
        public static FirebaseAppCheck DefaultInstance
        {
            get
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    return null;
                }

                return GetAppCheck(app);
            }
        }

        /// <summary>
        /// Returns the messaging instance for the specified app.
        /// </summary>
        /// <returns>The <see cref="FirebaseAppCheck"/> instance associated with the specified
        /// app.</returns>
        /// <exception cref="System.ArgumentNullException">If the app argument is null.</exception>
        /// <param name="app">An app instance.</param>
        public static FirebaseAppCheck GetAppCheck(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }

            return app.GetOrInit<FirebaseAppCheck>(typeof(FirebaseAppCheck).Name, () =>
            {
                return new FirebaseAppCheck(app);
            });
        }

        /// <summary>
        /// Creates a new AppCheckToken that can be sent back to a client.
        /// </summary>
        /// <param name="appId">The app ID to use as the JWT app_id.</param>
        /// <param name="options">Optional options object when creating a new App Check Token.</param>
        /// <returns>A <see cref="Task{AppCheckToken}"/> A promise that fulfills with a `AppCheckToken`.</returns>
        public async Task<AppCheckToken> CreateTokenAsync(string appId, AppCheckTokenOptions options = null)
        {
            string customToken = await this.appCheckTokenFactory.CreateCustomTokenAsync(appId, options, default(CancellationToken)).ConfigureAwait(false);

            return await this.appCheckApiClient.ExchangeTokenAsync(customToken, appId).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies a Firebase App Check token (JWT). If the token is valid, the promise is
        /// fulfilled with the token's decoded claims; otherwise, the promise is
        /// rejected.
        /// </summary>
        /// <param name="appCheckToken"> TThe App Check token to verify.</param>
        /// <param name="options">  Optional {@link VerifyAppCheckTokenOptions}  object when verifying an App Check Token.</param>
        /// <returns>A <see cref="Task{VerifyAppCheckTokenResponse}"/> A promise fulfilled with the token's decoded claims if the App Check token is valid; otherwise, a rejected promise.</returns>
        public async Task<AppCheckVerifyTokenResponse> VerifyTokenAsync(string appCheckToken, AppCheckVerifyTokenOptions options = null)
        {
            if (string.IsNullOrEmpty(appCheckToken))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    $"App check token {appCheckToken} must be a non - empty string.",
                    AppCheckErrorCode.InvalidArgument);
            }

            AppCheckDecodedToken decodedToken = await this.appCheckTokenVerifier.VerifyTokenAsync(appCheckToken).ConfigureAwait(false);

            if (options.Consume)
            {
                bool alreadyConsumed = await this.appCheckApiClient.VerifyReplayProtectionAsync(appCheckToken).ConfigureAwait(false);

                return new AppCheckVerifyTokenResponse()
                {
                    AppId = decodedToken.AppId,
                    AlreadyConsumed = alreadyConsumed,
                    Token = decodedToken,
                };
            }

            return new AppCheckVerifyTokenResponse()
            {
                AppId = decodedToken.AppId,
                Token = decodedToken,
            };
        }

        /// <summary>
        /// Deletes this <see cref="AppCheckApiClient"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            this.appCheckApiClient.Dispose();
            this.appCheckTokenFactory.Dispose();
        }
    }
}
