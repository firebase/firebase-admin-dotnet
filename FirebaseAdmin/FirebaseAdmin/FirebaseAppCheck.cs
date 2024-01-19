using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Check;
using Google.Apis.Auth;
using Newtonsoft.Json;
using RSAKey = System.Security.Cryptography.RSA;

namespace FirebaseAdmin
{
    /// <summary>
    /// Asynchronously creates a new Firebase App Check token for the specified Firebase app.
    /// </summary>
    /// <returns>A task that completes with the creation of a new App Check token.</returns>
    /// <exception cref="FirebaseAppCheckException">Thrown if an error occurs while creating the custom token.</exception>
    public sealed class FirebaseAppCheck
    {
        private static readonly string AppCheckIssuer = "https://firebaseappcheck.googleapis.com/";
        private static readonly string ProjectId;
        private static readonly string ScopedProjectId;
        private static readonly string JwksUrl = "https://firebaseappcheck.googleapis.com/v1/jwks";
        private static readonly IReadOnlyList<string> StandardClaims =
            ImmutableList.Create<string>("iss", "aud", "exp", "iat", "sub", "uid");

        private static List<Auth.Jwt.PublicKey> cachedKeys;

        /// <summary>
        /// Creates a new {@link AppCheckToken} that can be sent back to a client.
        /// </summary>
        /// <param name="appId">ID of Firebase App.</param>
        /// <param name="options">Options of FirebaseApp.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<AppCheckToken> CreateToken(string appId, AppCheckTokenOptions options = null)
        {
            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException("AppId must be a non-empty string.");
            }

            if (options == null)
            {
                var customOptions = AppCheckService.ValidateTokenOptions(options);
            }

            string customToken = " ";
            try
            {
                customToken = AppCheckTokenGernerator.CreateCustomToken(appId, options);
            }
            catch (Exception e)
            {
                throw new FirebaseAppCheckException("Error Create customToken", e.Message);
            }

            AppCheckApiClient appCheckApiClient = new AppCheckApiClient(appId);
            return await appCheckApiClient.ExchangeToken(customToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Verifies the format and signature of a Firebase App Check token.
        /// </summary>
        /// <param name="token"> The Firebase Auth JWT token to verify.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static async Task<Dictionary<string, FirebaseToken>> VerifyTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException("App check token " + token + " must be a non - empty string.");
            }

            try
            {
                FirebaseToken verified_claims = await Decode_and_verify(token).ConfigureAwait(false);
                Dictionary<string, FirebaseToken> appchecks = new ();
                appchecks.Add(ProjectId, verified_claims);
                return appchecks;
            }
            catch (Exception exception)
            {
                throw new ArgumentNullException("Verifying App Check token failed. Error:", exception);
            }
        }

        /// <summary>
        /// Get public key from jwksUrl.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public static async Task InitializeAsync()
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(JwksUrl).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    KeysRoot keysRoot = JsonConvert.DeserializeObject<KeysRoot>(responseString);
                    foreach (Key key in keysRoot.Keys)
                    {
                        var x509cert = new X509Certificate2(Encoding.UTF8.GetBytes(key.N));
                        RSAKey rsa = x509cert.GetRSAPublicKey();
                        cachedKeys.Add(new Auth.Jwt.PublicKey(key.Kid, rsa));
                    }

                    cachedKeys.ToImmutableList();
                }
                else
                {
                    throw new ArgumentNullException("Error Http request JwksUrl");
                }
            }
            catch (Exception exception)
            {
                throw new ArgumentNullException("Error Http request", exception);
            }
        }

        /// <summary>
        /// Decode_and_verify.
        /// </summary>
        /// <param name="token">The Firebase Auth JWT token to verify.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public static Task<FirebaseToken> Decode_and_verify(string token)
        {
            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw new FirebaseAppCheckException("Incorrect number of segments in Token");
            }

            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            var payload = JwtUtils.Decode<FirebaseToken.Args>(segments[1]);
            var projectIdMessage = $"Make sure the comes from the same Firebase "
                + "project as the credential used to initialize this SDK.";
            string issuer = AppCheckIssuer + ProjectId;
            string error = null;
            if (header.Algorithm != "RS256")
            {
                error = "The provided App Check token has incorrect algorithm. Expected RS256 but got '"
                    + header.Algorithm + "'";
            }
            else if (payload.Audience.Contains(ScopedProjectId))
            {
                error = "The provided App Check token has incorrect 'aud' (audience) claim.Expected "
                    + $"{ScopedProjectId} but got {payload.Audience}. {projectIdMessage} ";
            }
            else if (!(payload.Issuer is not null) || !payload.Issuer.StartsWith(AppCheckIssuer))
            {
                error = "The provided App Check token has incorrect 'iss' (issuer) claim.";
            }
            else if (string.IsNullOrEmpty(payload.Subject))
            {
                error = $"Firebase has no or empty subject (sub) claim.";
            }

            if (error != null)
            {
                throw new InvalidOperationException("invalid - argument" + error);
            }

            byte[] hash;
            using (var hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(
                    Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"));
            }

            var signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            var verified = cachedKeys.Any(key =>
                key.Id == header.KeyId && key.RSA.VerifyHash(
                    hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            if (verified)
            {
                var allClaims = JwtUtils.Decode<Dictionary<string, object>>(segments[1]);

                // Remove standard claims, so that only custom claims would remain.
                foreach (var claim in StandardClaims)
                {
                    allClaims.Remove(claim);
                }

                payload.Claims = allClaims.ToImmutableDictionary();
                return Task.FromResult(new FirebaseToken(payload));
            }

            return Task.FromResult(new FirebaseToken(payload));
        }

        /// <summary>
        /// Deleted all the apps created so far. Used for unit testing.
        /// </summary>
        public static void Delete()
        {
            lock (cachedKeys)
            {
                var copy = new List<Auth.Jwt.PublicKey>(cachedKeys);
                copy.Clear();

                if (cachedKeys.Count > 0)
                {
                    throw new InvalidOperationException("Failed to delete all apps");
                }
            }
        }
    }
}
