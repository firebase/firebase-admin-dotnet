using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth;
using Google.Apis.Util;
using Newtonsoft.Json;

[assembly: InternalsVisibleToAttribute("FirebaseAdmin.Tests,PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010081328559eaab41" +
"055b84af73469863499d81625dcbba8d8decb298b69e0f783a0958cf471fd4f76327b85a7d4b02" +
"3003684e85e61cf15f13150008c81f0b75a252673028e530ea95d0c581378da8c6846526ab9597" +
"4c6d0bc66d2462b51af69968a0e25114bde8811e0d6ee1dc22d4a59eee6a8bba4712cba839652f" +
"badddb9c")]

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// A helper class that creates Firebase custom tokens.
    /// </summary>
    internal class AppCheckTokenGenerator
    {
        public const string FirebaseAudience = "https://identitytoolkit.googleapis.com/"
           + "google.identity.identitytoolkit.v1.IdentityToolkit";

        public const string FirebaseAppCheckAudience = "https://firebaseappcheck.googleapis.com/google.firebase.appcheck.v1.TokenExchangeService";

        public const int OneMinuteInSeconds = 60;
        public const int OneMinuteInMills = OneMinuteInSeconds * 1000;
        public const int OneDayInMills = 24 * 60 * 60 * 1000;

        public static readonly DateTime UnixEpoch = new DateTime(
            1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static readonly ImmutableList<string> ReservedClaims = ImmutableList.Create(
            "acr",
            "amr",
            "at_hash",
            "aud",
            "auth_time",
            "azp",
            "cnf",
            "c_hash",
            "exp",
            "firebase",
            "iat",
            "iss",
            "jti",
            "nbf",
            "nonce",
            "sub");

        internal AppCheckTokenGenerator(Args args)
        {
            args.ThrowIfNull(nameof(args));

            this.Clock = args.Clock ?? SystemClock.Default;
            this.IsEmulatorMode = args.IsEmulatorMode;
            this.Signer = this.IsEmulatorMode ?
                EmulatorSigner.Instance : args.Signer.ThrowIfNull(nameof(args.Signer));
        }

        internal ISigner Signer { get; }

        internal IClock Clock { get; }

        internal string TenantId { get; }

        internal bool IsEmulatorMode { get; }

        public void Dispose()
        {
            this.Signer.Dispose();
        }

        internal static AppCheckTokenGenerator Create(FirebaseApp app)
        {
            ISigner signer = null;
            var serviceAccount = app.Options.Credential.ToServiceAccountCredential();
            if (serviceAccount != null)
            {
                // If the app was initialized with a service account, use it to sign
                // tokens locally.
                signer = new ServiceAccountSigner(serviceAccount);
            }
            else if (string.IsNullOrEmpty(app.Options.ServiceAccountId))
            {
                // If no service account ID is specified, attempt to discover one and invoke the
                // IAM service with it.
                signer = IAMSigner.Create(app);
            }
            else
            {
                // If a service account ID is specified, invoke the IAM service with it.
                signer = FixedAccountIAMSigner.Create(app);
            }

            var args = new Args
            {
                Signer = signer,
                IsEmulatorMode = Utils.IsEmulatorModeFromEnvironment,
            };
            return new AppCheckTokenGenerator(args);
        }

        internal async Task<string> CreateCustomTokenAsync(
            string appId,
            AppCheckTokenOptions options = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentException("appId must not be null or empty");
            }
            else if (appId.Length > 128)
            {
                throw new ArgumentException("appId must not be longer than 128 characters");
            }

            string customOptions = " ";
            if (options != null)
            {
                customOptions = this.ValidateTokenOptions(options).ToString();
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

        private int ValidateTokenOptions(AppCheckTokenOptions options)
        {
            if (options == null)
            {
                throw new ArgumentException("invalid-argument", "AppCheckTokenOptions must be a non-null object.");
            }

            if (options.TtlMillis < (OneMinuteInMills * 30) || options.TtlMillis > (OneDayInMills * 7))
            {
                throw new ArgumentException("invalid-argument", "ttlMillis must be a duration in milliseconds between 30 minutes and 7 days (inclusive).");
            }

            return options.TtlMillis;
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

            internal string TenantId { get; set; }

            internal bool IsEmulatorMode { get; set; }
        }
    }
}
