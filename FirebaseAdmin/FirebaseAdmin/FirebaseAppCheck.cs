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
using Google.Apis.Auth;
using Newtonsoft.Json;
using RSAKey = System.Security.Cryptography.RSA;

namespace FirebaseAdmin
{
    internal class FirebaseAppCheck
    {
        private readonly string appCheckIssuer = "https://firebaseappcheck.googleapis.com/";
        private readonly string jwksUrl = "https://firebaseappcheck.googleapis.com/v1/jwks";
        private Dictionary<string, FirebaseToken> appCheck = new Dictionary<string, FirebaseToken>();
        private string projectId;
        private string scopedProjectId;
        private List<Auth.Jwt.PublicKey> cachedKeys;
        private IReadOnlyList<string> standardClaims =
            ImmutableList.Create<string>("iss", "aud", "exp", "iat", "sub", "uid");

        private FirebaseAppCheck(FirebaseApp app)
        {
            this.scopedProjectId = "projects/" + this.projectId;
            FirebaseTokenVerifier tokenVerifier = FirebaseTokenVerifier.CreateIdTokenVerifier(app);
            this.projectId = tokenVerifier.ProjectId;
        }

        public static async Task<FirebaseAppCheck> CreateAsync(FirebaseApp app)
        {
            FirebaseAppCheck appCheck = new (app);
            bool result = await appCheck.Init().ConfigureAwait(false); // If Init fails, handle it accordingly
            if (!result)
            {
                return appCheck;
                throw new ArgumentException("Error App check initilaization ");
            }

            return appCheck;
        }

        public async Task<bool> Init()
        {
            try
            {
                using var client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(this.jwksUrl).ConfigureAwait(false);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    KeysRoot keysRoot = JsonConvert.DeserializeObject<KeysRoot>(responseString);
                    foreach (Key key in keysRoot.Keys)
                    {
                        var x509cert = new X509Certificate2(Encoding.UTF8.GetBytes(key.N));
                        RSAKey rsa = x509cert.GetRSAPublicKey();
                        this.cachedKeys.Add(new Auth.Jwt.PublicKey(key.Kid, rsa));
                    }

                    this.cachedKeys.ToImmutableList();
                    return true;
                }
                else
                {
                    throw new ArgumentException("Error Http request JwksUrl");
                }
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Error Http request", exception);
            }
        }

        public async Task<Dictionary<string, FirebaseToken>> VerifyTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("App check token " + token + " must be a non - empty string.");
            }

            try
            {
                FirebaseToken verified_claims = await this.Decode_and_verify(token).ConfigureAwait(false);
                Dictionary<string, FirebaseToken> appchecks = new ();
                appchecks.Add(this.projectId, verified_claims);
                return appchecks;
            }
            catch (Exception exception)
            {
                throw new ArgumentException("Verifying App Check token failed. Error:", exception);
            }
        }

        private Task<FirebaseToken> Decode_and_verify(string token)
        {
            string[] segments = token.Split('.');
            if (segments.Length != 3)
            {
                throw new ArgumentException("Incorrect number of segments in Token");
            }

            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            var payload = JwtUtils.Decode<FirebaseToken.Args>(segments[1]);
            var projectIdMessage = $"Make sure the comes from the same Firebase "
                + "project as the credential used to initialize this SDK.";
            string issuer = this.appCheckIssuer + this.projectId;
            string error = null;
            if (header.Algorithm != "RS256")
            {
                error = "The provided App Check token has incorrect algorithm. Expected RS256 but got '"
                    + header.Algorithm + "'";
            }
            else if (payload.Audience.Contains(this.scopedProjectId))
            {
                error = "The provided App Check token has incorrect 'aud' (audience) claim.Expected "
                    + $"{this.scopedProjectId} but got {payload.Audience}. {projectIdMessage} ";
            }
            else if (!(payload.Issuer is not null) || !payload.Issuer.StartsWith(this.appCheckIssuer))
            {
                error = "The provided App Check token has incorrect 'iss' (issuer) claim.";
            }
            else if (string.IsNullOrEmpty(payload.Subject))
            {
                error = $"Firebase has no or empty subject (sub) claim.";
            }

            if (error != null)
            {
                throw new ArgumentException("invalid - argument" + error);
            }

            byte[] hash;
            using (var hashAlg = SHA256.Create())
            {
                hash = hashAlg.ComputeHash(
                    Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}"));
            }

            var signature = JwtUtils.Base64DecodeToBytes(segments[2]);
            var verified = this.cachedKeys.Any(key =>
                key.Id == header.KeyId && key.RSA.VerifyHash(
                    hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));
            if (verified)
            {
                var allClaims = JwtUtils.Decode<Dictionary<string, object>>(segments[1]);

                // Remove standard claims, so that only custom claims would remain.
                foreach (var claim in this.standardClaims)
                {
                    allClaims.Remove(claim);
                }

                payload.Claims = allClaims.ToImmutableDictionary();
                return Task.FromResult(new FirebaseToken(payload));
            }

            return Task.FromResult(new FirebaseToken(payload));
        }
    }
}
