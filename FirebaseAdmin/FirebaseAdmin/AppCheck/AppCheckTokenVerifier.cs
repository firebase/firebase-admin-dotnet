using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using Google.Apis.Auth;
using Google.Apis.Util;

namespace FirebaseAdmin.AppCheck
{
    internal class AppCheckTokenVerifier
    {
        private const string AppCheckIssuer = "https://firebaseappcheck.googleapis.com/";
        private const string JWKSURL = "https://firebaseappcheck.googleapis.com/v1/jwks";

        private static readonly IReadOnlyList<string> StandardClaims =
            ImmutableList.Create<string>("iss", "aud", "exp", "iat", "sub", "uid");

        internal AppCheckTokenVerifier(Args args)
        {
            args.ThrowIfNull(nameof(args));
            this.ProjectId = args.ProjectId;
            this.KeySource = args.KeySource.ThrowIfNull(nameof(args.KeySource));
            this.Clock = args.Clock ?? SystemClock.Default;
        }

        internal IClock Clock { get; }

        internal string ProjectId { get; }

        internal IPublicKeySource KeySource { get; }

        /// <summary>
        /// Verifies the format and signature of a Firebase App Check token.
        /// </summary>
        /// <param name="token">The Firebase Auth JWT token to verify.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous operation.</param>
        /// <returns>A task that completes with a <see cref="AppCheckDecodedToken"/> representing
        /// a user with the specified user ID.</returns>
        public async Task<AppCheckDecodedToken> VerifyTokenAsync(
            string token, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new FirebaseAppCheckException(
                      ErrorCode.InvalidArgument,
                      "App Check token must not be null or empty.",
                      AppCheckErrorCode.InvalidArgument);
            }

            if (string.IsNullOrEmpty(this.ProjectId))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "Must initialize app with a cert credential or set your Firebase project ID as the GOOGLE_CLOUD_PROJECT environment variable to verify an App Check token.",
                    AppCheckErrorCode.InvalidCredential);
            }

            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw new FirebaseAppCheckException(
                      ErrorCode.InvalidArgument,
                      "Incorrect number of segments in app check token.",
                      AppCheckErrorCode.InvalidArgument);
            }

            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            var payload = JwtUtils.Decode<AppCheckDecodedToken.Args>(segments[1]);

            var projectIdMessage = $"Incorrect number of segments in app check Token."
                + "project as the credential used to initialize this SDK.";
            var scopedProjectId = $"projects/{this.ProjectId}";
            string errorMessage = string.Empty;

            if (header.Algorithm != "RS256")
            {
                errorMessage = "The provided app check token has incorrect algorithm. Expected 'RS256'" +
                " but got " + $"{header.Algorithm}" + ".";
            }
            else if (payload.Audience.Length > 0 || payload.Audience.Contains(scopedProjectId))
            {
                errorMessage = "The provided app check token has incorrect \"aud\" (audience) claim. Expected " +
                    scopedProjectId + "but got" + payload.Audience + "." + projectIdMessage;
            }
            else if (payload.Issuer.StartsWith(AppCheckIssuer))
            {
                errorMessage = $"The provided app check token has incorrect \"iss\" (issuer) claim.";
            }
            else if (payload.Subject == null)
            {
                errorMessage = "The provided app check token has no \"sub\" (subject) claim.";
            }
            else if (payload.Subject == string.Empty)
            {
                errorMessage = "The provided app check token has an empty string \"sub\" (subject) claim.";
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    errorMessage,
                    AppCheckErrorCode.InvalidArgument);
            }

            await this.VerifySignatureAsync(segments, header.KeyId, cancellationToken)
                .ConfigureAwait(false);
            var allClaims = JwtUtils.Decode<Dictionary<string, object>>(segments[1]);

            // Remove standard claims, so that only custom claims would remain.
            foreach (var claim in StandardClaims)
            {
                allClaims.Remove(claim);
            }

            payload.Claims = allClaims.ToImmutableDictionary();
            return new AppCheckDecodedToken(payload);
        }

        internal static AppCheckTokenVerifier Create(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var projectId = app.GetProjectId();
            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException(
                    "Must initialize FirebaseApp with a project ID to verify session cookies.");
            }

            IPublicKeySource keySource = new HttpPublicKeySource(
                JWKSURL, SystemClock.Default, app.Options.HttpClientFactory);

            var args = new Args
            {
                ProjectId = projectId,
                KeySource = keySource,
            };
            return new AppCheckTokenVerifier(args);
        }

        /// <summary>
        /// Verifies the integrity of a JWT by validating its signature. The JWT must be specified
        /// as an array of three segments (header, body and signature).
        /// </summary>
        private async Task VerifySignatureAsync(
            string[] segments, string keyId, CancellationToken cancellationToken)
        {
            byte[] hash;
            using (var hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(
                    Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"));
            }

            var signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            var keys = await this.KeySource.GetPublicKeysAsync(cancellationToken)
                .ConfigureAwait(false);
            var verified = keys.Any(key =>
                key.Id == keyId && key.RSA.VerifyHash(
                    hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            if (!verified)
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "Failed to verify app check signature.",
                    AppCheckErrorCode.InvalidCredential);
            }
        }

        internal sealed class Args
        {
            internal IClock Clock { get; set; }

            internal string ProjectId { get; set; }

            internal IPublicKeySource KeySource { get; set; }
        }
    }
}
