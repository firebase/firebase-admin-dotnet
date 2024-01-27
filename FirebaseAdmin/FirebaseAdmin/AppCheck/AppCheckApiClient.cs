using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Class that facilitates sending requests to the Firebase App Check backend API.
    /// </summary>
    internal sealed class AppCheckApiClient : IDisposable
    {
        private const string AppCheckUrlFormat = "https://firebaseappcheck.googleapis.com/v1/projects/{projectId}/apps/{appId}:exchangeCustomToken";
        private const string OneTimeUseTokenVerificationUrlFormat = "https://firebaseappcheck.googleapis.com/v1beta/projects/{projectId}:verifyAppCheckToken";

        private readonly ErrorHandlingHttpClient<FirebaseAppCheckException> httpClient;
        private readonly string projectId;

        internal AppCheckApiClient(Args args)
        {
            string noProjectId = "Project ID is required to access app check service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.";
            if (string.IsNullOrEmpty(args.ProjectId))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    noProjectId,
                    AppCheckErrorCode.InvalidArgument);
            }

            this.httpClient = new ErrorHandlingHttpClient<FirebaseAppCheckException>(
                new ErrorHandlingHttpClientArgs<FirebaseAppCheckException>()
                {
                    HttpClientFactory = args.ClientFactory.ThrowIfNull(nameof(args.ClientFactory)),
                    Credential = args.Credential.ThrowIfNull(nameof(args.Credential)),
                    RequestExceptionHandler = AppCheckErrorHandler.Instance,
                    ErrorResponseHandler = AppCheckErrorHandler.Instance,
                    DeserializeExceptionHandler = AppCheckErrorHandler.Instance,
                    RetryOptions = args.RetryOptions,
                });
            this.projectId = args.ProjectId;
        }

        internal static string ClientVersion
        {
            get
            {
                return $"fire-admin-dotnet/{FirebaseApp.GetSdkVersion()}";
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="customToken">The custom token to be exchanged.</param>
        /// <param name="appId">The mobile App ID.</param>
        /// <returns>A promise that fulfills with a `AppCheckToken`.</returns>
        public async Task<AppCheckToken> ExchangeTokenAsync(string customToken, string appId)
        {
            if (string.IsNullOrEmpty(customToken))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "customToken must be a non-empty string.",
                    AppCheckErrorCode.InvalidArgument);
            }

            if (string.IsNullOrEmpty(appId))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "appId must be a non-empty string.",
                    AppCheckErrorCode.InvalidArgument);
            }

            var body = new ExchangeTokenRequest()
            {
                CustomToken = customToken,
            };

            var url = this.GetUrl(appId);
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };
            AddCommonHeaders(request);

            try
            {
                var response = await this.httpClient
                    .SendAndDeserializeAsync<ExchangeTokenResponse>(request)
                    .ConfigureAwait(false);

                var appCheck = this.ToAppCheckToken(response.Result);

                return appCheck;
            }
            catch (HttpRequestException ex)
            {
                throw AppCheckErrorHandler.Instance.HandleHttpRequestException(ex);
            }
        }

        public async Task<bool> VerifyReplayProtectionAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "`tokne` must be a non-empty string.",
                    AppCheckErrorCode.InvalidArgument);
            }

            var body = new VerifyTokenRequest()
            {
                AppCheckToken = token,
            };

            string url = this.GetVerifyTokenUrl();
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };
            AddCommonHeaders(request);

            bool ret = false;

            try
            {
                var response = await this.httpClient
                    .SendAndDeserializeAsync<VerifyTokenResponse>(request)
                    .ConfigureAwait(false);

                ret = response.Result.AlreadyConsumed;
            }
            catch (HttpRequestException e)
            {
                AppCheckErrorHandler.Instance.HandleHttpRequestException(e);
            }

            return ret;
        }

        internal static AppCheckApiClient Create(FirebaseApp app)
        {
            var args = new Args
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                ProjectId = app.Options.ProjectId,
                RetryOptions = RetryOptions.Default,
            };

            return new AppCheckApiClient(args);
        }

        private static void AddCommonHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("X-Firebase-Client", ClientVersion);
        }

        private AppCheckToken ToAppCheckToken(ExchangeTokenResponse resp)
        {
            if (resp == null || string.IsNullOrEmpty(resp.Token))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.PermissionDenied,
                    "Token is not valid",
                    AppCheckErrorCode.AppCheckTokenExpired);
            }

            if (string.IsNullOrEmpty(resp.Ttl) || !resp.Ttl.EndsWith("s"))
            {
                throw new FirebaseAppCheckException(
                    ErrorCode.InvalidArgument,
                    "`ttl` must be a valid duration string with the suffix `s`.",
                    AppCheckErrorCode.InvalidArgument);
            }

            return new AppCheckToken(resp.Token, this.StringToMilliseconds(resp.Ttl));
        }

        private string GetUrl(string appId)
        {
            if (string.IsNullOrEmpty(this.projectId))
            {
                string errorMessage = "Failed to determine project ID. Initialize the SDK with service account " +
                      "credentials or set project ID as an app option. Alternatively, set the " +
                      "GOOGLE_CLOUD_PROJECT environment variable.";

                throw new FirebaseAppCheckException(
                    ErrorCode.Unknown,
                    errorMessage,
                    AppCheckErrorCode.UnknownError);
            }

            var urlParams = new Dictionary<string, string>
            {
                { "projectId", this.projectId },
                { "appId", appId },
            };

            return HttpUtils.FormatString(AppCheckUrlFormat, urlParams);
        }

        private int StringToMilliseconds(string duration)
        {
            string modifiedString = duration.Remove(duration.Length - 1);
            return int.Parse(modifiedString) * 1000;
        }

        private string GetVerifyTokenUrl()
        {
            if (string.IsNullOrEmpty(this.projectId))
            {
                string errorMessage = "Failed to determine project ID. Initialize the SDK with service account " +
                      "credentials or set project ID as an app option. Alternatively, set the " +
                      "GOOGLE_CLOUD_PROJECT environment variable.";

                throw new FirebaseAppCheckException(
                    ErrorCode.Unknown,
                    errorMessage,
                    AppCheckErrorCode.UnknownError);
            }

            var urlParams = new Dictionary<string, string>
            {
                { "projectId", this.projectId },
            };

            return HttpUtils.FormatString(OneTimeUseTokenVerificationUrlFormat, urlParams);
        }

        internal sealed class Args
        {
            internal HttpClientFactory ClientFactory { get; set; }

            internal GoogleCredential Credential { get; set; }

            internal string ProjectId { get; set; }

            internal RetryOptions RetryOptions { get; set; }
        }

        internal class ExchangeTokenRequest
        {
            [Newtonsoft.Json.JsonProperty("customToken")]
            public string CustomToken { get; set; }
        }

        internal class ExchangeTokenResponse
        {
            [Newtonsoft.Json.JsonProperty("token")]
            public string Token { get; set; }

            [Newtonsoft.Json.JsonProperty("ttl")]
            public string Ttl { get; set; }
        }

        internal class VerifyTokenRequest
        {
            [Newtonsoft.Json.JsonProperty("appCheckToken")]
            public string AppCheckToken { get; set; }
        }

        internal class VerifyTokenResponse
        {
            [Newtonsoft.Json.JsonProperty("alreadyConsumed")]
            public bool AlreadyConsumed { get; set; }
        }
    }
}
