using Google.Apis.Auth;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// 
    /// </summary>
    internal class TokenProvider 
    {
        private readonly ISigner _signer;
        private GoogleOAuthAccessToken _token;
        private readonly IClock _clock;
        private readonly ConfigurableHttpClient _httpClient;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="signer"></param>
        /// <param name="clock"></param>
        /// <param name="configurableHttpClient"></param>
        public TokenProvider(
            ISigner signer, 
            IClock clock,
            ConfigurableHttpClient configurableHttpClient)
        {
            _signer = signer;
            _clock = clock;
            _httpClient = configurableHttpClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public async Task<GoogleOAuthAccessToken> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_token!= null && !_token.IsExpired())
            {
                return _token;
            }
            var token = await CreateAuthJwtAsync(cancellationToken).ConfigureAwait(false);

            _token = await RequestAccessToken(token, cancellationToken);

            return _token;
        }


        private async Task<string> CreateAuthJwtAsync(CancellationToken cancellationToken = default)
        {
            var header = new JsonWebSignature.Header()
            {
                Algorithm = "RS256",
                Type = "JWT"
            };

            var issued = (int)_clock.UnixTimestamp();
            var keyId = await _signer.GetKeyIdAsync(cancellationToken).ConfigureAwait(false);
            var payload = new Dictionary<string, object>
            {
                {"scope", "https://www.googleapis.com/auth/cloud-platform https://www.googleapis.com/auth/firebase.database https://www.googleapis.com/auth/firebase.messaging https://www.googleapis.com/auth/identitytoolkit https://www.googleapis.com/auth/userinfo.email" },
                {"iat", issued},
                {"exp", issued + 3600},
                {"aud", "https://accounts.google.com/o/oauth2/token"},
                {"iss", keyId},
            };

            return await JwtUtils.CreateSignedJwtAsync(
                header, payload, _signer, cancellationToken).ConfigureAwait(false);
        }

        private async Task<GoogleOAuthAccessToken> RequestAccessToken(string token, CancellationToken cancellationToken = default)
        {
            var dic = new Dictionary<string, string>
            {
                {"grant_type",  "urn:ietf:params:oauth:grant-type:jwt-bearer"},
                {"assertion", token }
            };
            var authJwtContent = await _httpClient.PostAsync("https://accounts.google.com/o/oauth2/token", new FormUrlEncodedContent(dic));
            var authJwtString = await authJwtContent.Content.ReadAsStringAsync();
            return NewtonsoftJsonSerializer.Instance.Deserialize<GoogleOAuthAccessToken>(authJwtString);

        }
    }
}
