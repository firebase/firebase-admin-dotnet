using System;
using System.Collections.Generic;
using System.Text;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Check;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Json;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Initializes static members of the <see cref="AppCheckTokenGernerator"/> class.
    /// </summary>
    public class AppCheckTokenGernerator
    {
        private static readonly string AppCheckAudience = "https://firebaseappcheck.googleapis.com/google.firebase.appcheck.v1.TokenExchangeService";
        private readonly CyptoSigner signer;
        private string appId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppCheckTokenGernerator"/> class.
        /// </summary>
        /// <param name="appId">FirebaseApp Id.</param>
        public AppCheckTokenGernerator(string appId)
        {
            this.appId = appId;
        }

        /// <summary>
        /// Initializes static members of the <see cref="AppCheckTokenGernerator"/> class.
        /// </summary>
        /// <param name="appId"> FirebaseApp Id.</param>
        /// <param name="options"> FirebaseApp AppCheckTokenOptions.</param>
        /// <returns> Created token.</returns>
        public static string CreateCustomToken(string appId, AppCheckTokenOptions options)
        {
            var customOptions = new Dictionary<string, string>();

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            if (options == null)
            {
                customOptions.Add(AppCheckService.ValidateTokenOptions(options));
            }

            CyptoSigner signer = new (appId);
            string account = signer.GetAccountId();

            var header = new Dictionary<string, string>()
            {
                { "alg", "RS256" },
                { "typ", "JWT" },
            };
            var iat = Math.Floor(DateTime.now() / 1000);
            var payload = new Dictionary<string, string>()
            {
                { "iss", account },
                { "sub", account },
                { "app_id", appId },
                { "aud", AppCheckAudience },
                { "exp", iat + 300 },
                { "iat", iat },
            };

            foreach (var each in customOptions)
            {
                payload.Add(each.Key, each.Value);
            }

            string token = Encode(header) + Encode(payload);
            return token;
        }

        private static string Encode(object obj)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(obj);
            return UrlSafeBase64Encode(Encoding.UTF8.GetBytes(json));
        }

        private static string UrlSafeBase64Encode(byte[] bytes)
        {
            var base64Value = Convert.ToBase64String(bytes);
            return base64Value.TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
