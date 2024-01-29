using System;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Messaging.Util;
using Google.Apis.Auth;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// A helper class that creates Firebase custom tokens.
    /// </summary>
    internal class AppCheckTokenFactory : IDisposable
    {
        public const string FirebaseAppCheckAudience = "https://firebaseappcheck.googleapis.com/google.firebase.appcheck.v1.TokenExchangeService";

        public const int OneMinuteInSeconds = 60;
        public const int OneMinuteInMills = OneMinuteInSeconds * 1000;
        public const int OneDayInMills = 24 * 60 * 60 * 1000;

        public static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        internal AppCheckTokenFactory(Args args)
        {
            args.ThrowIfNull(nameof(args));

            this.Clock = args.Clock ?? SystemClock.Default;
            this.IsEmulatorMode = args.IsEmulatorMode;
            this.Signer = this.IsEmulatorMode ?
                EmulatorSigner.Instance : args.Signer.ThrowIfNull(nameof(args.Signer));
        }

        internal ISigner Signer { get; }

        internal IClock Clock { get; }

        internal bool IsEmulatorMode { get; }

        public void Dispose()
        {
            this.Signer.Dispose();
        }

        /// <summary>
        /// Creates a new custom token that can be exchanged to an App Check token.
        /// </summary>
        /// <param name="appId">The mobile App ID.</param>
        /// <param name="options">Options for AppCheckToken with ttl. Possibly null.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous.</param>
        /// <returns>A Promise fulfilled with a custom token signed with a service account key that can be exchanged to an App Check token.</returns>
        public async Task<string> CreateCustomTokenAsync(
            string appId,
            AppCheckTokenOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "`appId` must be a non-empty string.",
                    AppCheckErrorCode.InvalidArgument);
            }

            string customOptions = " ";
            if (options != null)
            {
                customOptions = this.ValidateTokenOptions(options);
            }

            var header = new JsonWebSignature.Header()
            {
                Algorithm = this.Signer.Algorithm,
                Type = "JWT",
            };

            var issued = (int)(this.Clock.UtcNow - UnixEpoch).TotalSeconds;
            var keyId = await this.Signer.GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var payload = new CustomTokenPayload()
            {
                AppId = appId,
                Issuer = keyId,
                Subject = keyId,
                Audience = FirebaseAppCheckAudience,
                IssuedAtTimeSeconds = issued,
                ExpirationTimeSeconds = issued + (OneMinuteInSeconds * 5),
                Ttl = customOptions,
            };

            return await JwtUtils.CreateSignedJwtAsync(
                header, payload, this.Signer).ConfigureAwait(false);
        }

        internal static AppCheckTokenFactory Create(FirebaseApp app)
        {
            ISigner signer = null;
            var serviceAccount = app.Options.Credential.ToServiceAccountCredential();
            if (serviceAccount != null)
            {
                signer = new ServiceAccountSigner(serviceAccount);
            }
            else if (string.IsNullOrEmpty(app.Options.ServiceAccountId))
            {
                signer = IAMSigner.Create(app);
            }
            else
            {
                signer = FixedAccountIAMSigner.Create(app);
            }

            var args = new Args
            {
                Signer = signer,
                IsEmulatorMode = Utils.IsEmulatorModeFromEnvironment,
            };
            return new AppCheckTokenFactory(args);
        }

        private string ValidateTokenOptions(AppCheckTokenOptions options)
        {
            if (options == null)
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "AppCheckTokenOptions must be a non-null object.",
                    AppCheckErrorCode.InvalidArgument);
            }

            if (options.TtlMillis < (OneMinuteInMills * 30) || options.TtlMillis > (OneDayInMills * 7))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "ttlMillis must be a duration in milliseconds between 30 minutes and 7 days (inclusive).",
                    AppCheckErrorCode.InvalidArgument);
            }

            return TimeConverter.LongMillisToString(options.TtlMillis);
        }

        internal class CustomTokenPayload : JsonWebToken.Payload
        {
            [JsonPropertyAttribute("app_id")]
            public string AppId { get; set; }

            [JsonPropertyAttribute("ttl")]
            public string Ttl { get; set; }
        }

        internal sealed class Args
        {
            internal ISigner Signer { get; set; }

            internal IClock Clock { get; set; }

            internal bool IsEmulatorMode { get; set; }
        }
    }
}
