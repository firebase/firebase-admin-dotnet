using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Google.Apis.Requests.BatchRequest;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Class that facilitates sending requests to the Firebase App Check backend API.
    /// </summary>
    /// <returns>A task that completes with the creation of a new App Check token.</returns>
    /// <exception cref="ArgumentNullException">Thrown if an error occurs while creating the custom token.</exception>
    /// <value>The Firebase app instance.</value>
    internal class AppCheckApiClient
    {
        private const string ApiUrlFormat = "https://firebaseappcheck.googleapis.com/v1/projects/{projectId}/apps/{appId}:exchangeCustomToken";
        private const string OneTimeUseTokenVerificationUrlFormat = "https://firebaseappcheck.googleapis.com/v1beta/projects/{projectId}:verifyAppCheckToken";
        private readonly FirebaseApp app;
        private string projectId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppCheckApiClient"/> class.
        /// </summary>
        /// <param name="value"> Initailize FirebaseApp. </param>
        public AppCheckApiClient(FirebaseApp value)
        {
            if (value == null || value.Options == null)
            {
                throw new ArgumentException("Argument passed to admin.appCheck() must be a valid Firebase app instance.");
            }

            this.app = value;
            this.projectId = this.app.Options.ProjectId;
        }

        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="customToken"> The custom token to be exchanged. </param>
        /// <param name="appId"> The mobile App ID.</param>
        /// <returns>A <see cref="Task{AppCheckToken}"/> A promise that fulfills with a `AppCheckToken`.</returns>
        public async Task<AppCheckToken> ExchangeTokenAsync(string customToken, string appId)
        {
            if (string.IsNullOrEmpty(customToken))
            {
                throw new ArgumentNullException("First argument passed to customToken must be a valid Firebase app instance.");
            }

            if (string.IsNullOrEmpty(appId))
            {
                throw new ArgumentNullException("Second argument passed to appId must be a valid Firebase app instance.");
            }

            var url = this.GetUrl(appId);
            var content = new StringContent(JsonConvert.SerializeObject(new { customToken }), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = content,
            };
            request.Headers.Add("X-Firebase-Client", "fire-admin-node/" + $"{FirebaseApp.GetSdkVersion()}");
            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new InvalidOperationException($"BadRequest: {errorContent}");
            }
            else if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException("network error");
            }

            JObject responseData = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            string tokenValue = responseData["data"]["token"].ToString();
            int ttlValue = this.StringToMilliseconds(responseData["data"]["ttl"].ToString());
            AppCheckToken appCheckToken = new (tokenValue, ttlValue);
            return appCheckToken;
        }

        /// <summary>
        /// Exchange a signed custom token to App Check token.
        /// </summary>
        /// <param name="token"> The custom token to be exchanged. </param>
        /// <returns>A alreadyConsumed is true.</returns>
        public async Task<bool> VerifyReplayProtection(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentException("invalid-argument", "`token` must be a non-empty string.");
            }

            string url = this.GetVerifyTokenUrl();

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StringContent(token),
            };

            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);

            var responseData = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            bool alreadyConsumed = (bool)responseData["data"]["alreadyConsumed"];
            return alreadyConsumed;
        }

        /// <summary>
        /// Get Verify Token Url .
        /// </summary>
        /// <returns>A formatted verify token url.</returns>
        private string GetVerifyTokenUrl()
        {
            var urlParams = new Dictionary<string, string>
            {
                { "projectId", this.projectId },
            };

            string baseUrl = this.FormatString(OneTimeUseTokenVerificationUrlFormat, urlParams);
            return this.FormatString(baseUrl, null);
        }

        /// <summary>
        /// Get url from FirebaseApp Id .
        /// </summary>
        /// <param name="appId">The FirebaseApp Id.</param>
        /// <returns>A formatted verify token url.</returns>
        private string GetUrl(string appId)
        {
            if (string.IsNullOrEmpty(this.projectId))
            {
                this.projectId = this.app.GetProjectId();
            }

            if (string.IsNullOrEmpty(this.projectId))
            {
                string errorMessage = "Failed to determine project ID. Initialize the SDK with service account " +
                      "credentials or set project ID as an app option. Alternatively, set the " +
                      "GOOGLE_CLOUD_PROJECT environment variable.";
                throw new ArgumentException(
                    "unknown-error",
                    errorMessage);
            }

            var urlParams = new Dictionary<string, string>
            {
                { "projectId", this.projectId },
                { "appId", appId },
            };
            string baseUrl = this.FormatString(ApiUrlFormat, urlParams);
            return baseUrl;
        }

        /// <summary>
        /// Converts a duration string with the suffix `s` to milliseconds.
        /// </summary>
        /// <param name="duration">The duration as a string with the suffix "s" preceded by the number of seconds.</param>
        /// <returns> The duration in milliseconds.</returns>
        private int StringToMilliseconds(string duration)
        {
            if (string.IsNullOrEmpty(duration) || !duration.EndsWith("s"))
            {
                throw new ArgumentException("invalid-argument", "`ttl` must be a valid duration string with the suffix `s`.");
            }

            string modifiedString = duration.Remove(duration.Length - 1);
            return int.Parse(modifiedString) * 1000;
        }

        /// <summary>
        /// Formats a string of form 'project/{projectId}/{api}' and replaces with corresponding arguments {projectId: '1234', api: 'resource'}.
        /// </summary>
        /// <param name="str">The original string where the param need to be replaced.</param>
        /// <param name="urlParams">The optional parameters to replace in thestring.</param>
        /// <returns> The resulting formatted string. </returns>
        private string FormatString(string str, Dictionary<string, string> urlParams)
        {
            string formatted = str;
            foreach (var key in urlParams.Keys)
            {
                formatted = Regex.Replace(formatted, $"{{{key}}}", urlParams[key]);
            }

            return formatted;
        }
    }
}
