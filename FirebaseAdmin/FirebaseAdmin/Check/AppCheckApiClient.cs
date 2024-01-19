using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Check
{
    internal class AppCheckApiClient
    {
        private const string ApiUrlFormat = "https://firebaseappcheck.googleapis.com/v1/projects/{projectId}/apps/{appId}:exchangeCustomToken";
        private readonly FirebaseApp app;
        private string projectId;
        private string appId;

        public AppCheckApiClient(FirebaseApp value)
        {
            if (value == null || value.Options == null)
            {
                throw new ArgumentException("Argument passed to admin.appCheck() must be a valid Firebase app instance.");
            }

            this.app = value;
            this.projectId = this.app.Options.ProjectId;
        }

        public AppCheckApiClient(string appId)
        {
            this.appId = appId;
        }

        public async Task<AppCheckToken> ExchangeToken(string customToken)
        {
            if (customToken == null)
            {
                throw new ArgumentException("First argument passed to customToken must be a valid Firebase app instance.");
            }

            if (this.appId == null)
            {
                throw new ArgumentException("Second argument passed to appId must be a valid Firebase app instance.");
            }

            var url = this.GetUrl(this.appId);
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = new StringContent(customToken),
            };
            request.Headers.Add("X-Firebase-Client", "fire-admin-node/${utils.getSdkVersion()}");
            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new ArgumentException("Error exchanging token.");
            }

            var responseData = JObject.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
            string tokenValue = responseData["data"]["token"].ToString();
            int ttlValue = int.Parse(responseData["data"]["ttl"].ToString());
            AppCheckToken appCheckToken = new (tokenValue, ttlValue);
            return appCheckToken;
        }

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
