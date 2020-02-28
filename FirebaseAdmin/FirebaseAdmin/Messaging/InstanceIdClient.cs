// Copyright 2019, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A helper class for interacting with the Firebase Instance ID service.Implements the FCM
    /// topic management functionality.
    /// </summary>
    internal sealed class InstanceIdClient : IDisposable
    {
        private const string IidHost = "https://iid.googleapis.com";

        private const string IidSubscriberPath = "iid/v1:batchAdd";

        private const string IidUnsubscribePath = "iid/v1:batchRemove";

        private readonly ErrorHandlingHttpClient<FirebaseMessagingException> httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="InstanceIdClient"/> class.
        /// </summary>
        /// <param name="clientFactory">A default implentation of the HTTP client factory.</param>
        /// <param name="credential">An instance of the <see cref="GoogleCredential"/> class.</param>
        /// <param name="retryOptions">An instance of the <see cref="RetryOptions"/> class.</param>
        internal InstanceIdClient(
            HttpClientFactory clientFactory, GoogleCredential credential, RetryOptions retryOptions = null)
        {
            this.httpClient = new ErrorHandlingHttpClient<FirebaseMessagingException>(
                new ErrorHandlingHttpClientArgs<FirebaseMessagingException>()
                {
                    HttpClientFactory = clientFactory.ThrowIfNull(nameof(clientFactory)),
                    Credential = credential.ThrowIfNull(nameof(credential)),
                    RequestExceptionHandler = MessagingErrorHandler.Instance,
                    ErrorResponseHandler = InstanceIdErrorHandler.Instance,
                    DeserializeExceptionHandler = MessagingErrorHandler.Instance,
                    RetryOptions = retryOptions,
                });
        }

        /// <summary>
        /// Subscribes a list of registration tokens to a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to subscribe.</param>
        /// <param name="topic">The topic name to subscribe to. /topics/ will be prepended to the
        /// topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving
        /// details about the topic subscription operations.</returns>
        public async Task<TopicManagementResponse> SubscribeToTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic)
        {
            return await this.SendInstanceIdRequest(registrationTokens, topic, IidSubscriberPath)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribes a list of registration tokens from a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to unsubscribe.</param>
        /// <param name="topic">The topic name to unsubscribe from. /topics/ will be prepended to
        /// the topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving
        /// details about the topic unsubscription operations.</returns>
        public async Task<TopicManagementResponse> UnsubscribeFromTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic)
        {
            return await this.SendInstanceIdRequest(registrationTokens, topic, IidUnsubscribePath)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Dispose the HttpClient.
        /// </summary>
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        internal static InstanceIdClient Create(FirebaseApp app)
        {
            return new InstanceIdClient(
                app.Options.HttpClientFactory,
                app.Options.Credential,
                RetryOptions.Default);
        }

        private async Task<TopicManagementResponse> SendInstanceIdRequest(
            IReadOnlyList<string> registrationTokens, string topic, string path)
        {
            this.ValidateRegistrationTokenList(registrationTokens);

            string url = $"{IidHost}/{path}";
            var body = new InstanceIdServiceRequest
            {
                Topic = this.GetPrefixedTopic(topic),
                RegistrationTokens = registrationTokens,
            };

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(url),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };

            request.Headers.Add("access_token_auth", "true");

            var response = await this.httpClient
                .SendAndDeserializeAsync<InstanceIdServiceResponse>(request)
                .ConfigureAwait(false);
            return new TopicManagementResponse(response.Result);
        }

        private void ValidateRegistrationTokenList(IReadOnlyList<string> registrationTokens)
        {
            if (registrationTokens == null)
            {
                throw new ArgumentNullException("Registration token list must not be null");
            }

            var count = registrationTokens.Count;
            if (count == 0)
            {
                throw new ArgumentException("Registration token list must not be empty");
            }

            if (count > 1000)
            {
                throw new ArgumentException("Registration token list must not contain more than 1000 tokens");
            }

            foreach (var registrationToken in registrationTokens)
            {
                if (string.IsNullOrEmpty(registrationToken))
                {
                    throw new ArgumentException("Registration tokens must not be null or empty");
                }
            }
        }

        private string GetPrefixedTopic(string topic)
        {
            if (topic.StartsWith("/topics/"))
            {
                return topic;
            }
            else
            {
                return "/topics/" + topic;
            }
        }

        private class InstanceIdServiceRequest
        {
            [JsonProperty("to")]
            public string Topic { get; set; }

            [JsonProperty("registration_tokens")]
            public IEnumerable<string> RegistrationTokens { get; set; }
        }

        private class InstanceIdServiceError
        {
            [JsonProperty("error")]
            public string ErrorCode { get; set; }
        }

        private class InstanceIdErrorHandler : HttpErrorHandler<FirebaseMessagingException>
        {
            internal static readonly InstanceIdErrorHandler Instance = new InstanceIdErrorHandler();

            private InstanceIdErrorHandler() { }

            protected override FirebaseMessagingException CreateException(FirebaseExceptionArgs args)
            {
                var errorCode = this.GetErrorCode(args.ResponseBody);
                var message = args.Message;
                if (!string.IsNullOrEmpty(errorCode))
                {
                    message = $"Error while calling the IID service: {errorCode}";
                }

                return new FirebaseMessagingException(
                    args.Code,
                    message,
                    null,
                    response: args.HttpResponse);
            }

            private string GetErrorCode(string response)
            {
                try
                {
                    var iidError = NewtonsoftJsonSerializer.Instance.Deserialize<InstanceIdServiceError>(
                        response);
                    return iidError.ErrorCode;
                }
                catch
                {
                    return null;
                }
            }
        }
  }
}
