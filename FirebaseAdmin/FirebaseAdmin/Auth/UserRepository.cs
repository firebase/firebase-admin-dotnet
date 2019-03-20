using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth
{
    internal class UserRepository
    {
        private readonly ConfigurableHttpClient _httpClient;
        private readonly string baseUrl = "https://www.googleapis.com";
        private readonly TokenProvider _tokenProvider;

        /// <summary>
        /// 
        /// </summary>
        internal UserRepository(
            TokenProvider tokenProvider,
            HttpClientFactory clientFactory, 
            GoogleCredential credential)
        {
            _httpClient = clientFactory.CreateAuthorizedHttpClient(credential);
            _httpClient.BaseAddress = new Uri(baseUrl);
            _tokenProvider = tokenProvider;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task DeleteUserAsync(string uid, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(uid))
            {
                throw new ArgumentException("uid must not be null or empty");
            }
            else if (uid.Length > 128)
            {
                throw new ArgumentException("uid must not be longer than 128 characters");
            }
            return DeleteUser(uid,  cancellationToken);
        }

        private async Task DeleteUser(string uid, CancellationToken cancellationToken = default)
        {
            var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token.TokenType, token.AccessToken);

            var result = await _httpClient.PostJsonAsync("/identitytoolkit/v3/relyingparty/deleteAccount", new
            {
                localId = uid,
            }, cancellationToken);

            result.EnsureSuccessStatusCode();
        }

        internal static UserRepository Create(FirebaseApp app)
        {
            var factory = new HttpClientFactory();
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
                signer = new IAMSigner(new HttpClientFactory(), app.Options.Credential);
            }
            else
            {
                // If a service account ID is specified, invoke the IAM service with it.
                signer = new FixedAccountIAMSigner(factory, app.Options.Credential, app.Options.ServiceAccountId);
            }
            
            var tokenProvider = new TokenProvider(signer, SystemClock.Default, factory.CreateAuthorizedHttpClient(app.Options.Credential));

            return new UserRepository(tokenProvider, factory, app.Options.Credential);
        }
    }
}
