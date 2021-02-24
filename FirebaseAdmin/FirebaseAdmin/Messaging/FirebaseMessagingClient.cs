// Copyright 2018, Google Inc. All rights reserved.
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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A client for making authorized HTTP calls to the FCM backend service. Handles request
    /// serialization, response parsing, and HTTP error handling.
    /// </summary>
    internal sealed class FirebaseMessagingClient : IDisposable
    {
        private const string FcmBaseUrl = "https://fcm.googleapis.com";
        private const string FcmSendUrl = FcmBaseUrl + "/v1/projects/{0}/messages:send";
        private const string FcmBatchUrl = FcmBaseUrl + "/batch";

        private readonly ErrorHandlingHttpClient<FirebaseMessagingException> httpClient;
        private readonly string sendUrl;
        private readonly string restPath;
        private readonly FCMClientService fcmClientService;

        internal FirebaseMessagingClient(Args args)
        {
            if (string.IsNullOrEmpty(args.ProjectId))
            {
                throw new ArgumentException(
                    "Project ID is required to access messaging service. Use a service account "
                    + "credential or set the project ID explicitly via AppOptions. Alternatively "
                    + "you can set the project ID via the GOOGLE_CLOUD_PROJECT environment "
                    + "variable.");
            }

            this.httpClient = new ErrorHandlingHttpClient<FirebaseMessagingException>(
                new ErrorHandlingHttpClientArgs<FirebaseMessagingException>()
                {
                    HttpClientFactory = args.ClientFactory.ThrowIfNull(nameof(args.ClientFactory)),
                    Credential = args.Credential.ThrowIfNull(nameof(args.Credential)),
                    RequestExceptionHandler = MessagingErrorHandler.Instance,
                    ErrorResponseHandler = MessagingErrorHandler.Instance,
                    DeserializeExceptionHandler = MessagingErrorHandler.Instance,
                    RetryOptions = args.RetryOptions,
                });
            this.fcmClientService = new FCMClientService(new BaseClientService.Initializer()
            {
                HttpClientFactory = args.ClientFactory,
                HttpClientInitializer = args.Credential,
                ApplicationName = ClientVersion,
            });
            this.sendUrl = string.Format(FcmSendUrl, args.ProjectId);
            this.restPath = this.sendUrl.Substring(FcmBaseUrl.Length);
        }

        internal static string ClientVersion
        {
            get
            {
                return $"fire-admin-dotnet/{FirebaseApp.GetSdkVersion()}";
            }
        }

        /// <summary>
        /// Sends a message to the FCM service for delivery. The message gets validated both by
        /// the Admin SDK, and the remote FCM service. A successful return value indicates
        /// that the message has been successfully sent to FCM, where it has been accepted by the
        /// FCM service.
        /// </summary>
        /// <returns>A task that completes with a message ID string, which represents
        /// successful handoff to FCM.</returns>
        /// <exception cref="ArgumentNullException">If the message argument is null.</exception>
        /// <exception cref="ArgumentException">If the message contains any invalid
        /// fields.</exception>
        /// <exception cref="FirebaseMessagingException">If an error occurs while sending the
        /// message.</exception>
        /// <param name="message">The message to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the message will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public async Task<string> SendAsync(
            Message message,
            bool dryRun = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var body = new SendRequest()
            {
                Message = message.ThrowIfNull(nameof(message)).CopyAndValidate(),
                ValidateOnly = dryRun,
            };

            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(this.sendUrl),
                Content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body),
            };
            AddCommonHeaders(request);
            var response = await this.httpClient
                .SendAndDeserializeAsync<SingleMessageResponse>(request, cancellationToken)
                .ConfigureAwait(false);

            return response.Result.Name;
        }

        /// <summary>
        /// Sends all messages in a single batch.
        /// </summary>
        /// <param name="messages">The messages to be sent. Must not be null.</param>
        /// <param name="dryRun">A boolean indicating whether to perform a dry run (validation
        /// only) of the send. If set to true, the messages will be sent to the FCM backend service,
        /// but it will not be delivered to any actual recipients.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        /// <returns>A task that completes with a <see cref="BatchResponse"/>, giving details about
        /// the batch operation.</returns>
        public async Task<BatchResponse> SendAllAsync(
            IEnumerable<Message> messages,
            bool dryRun = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var copyOfMessages = messages.ThrowIfNull(nameof(messages))
                .Select((message) => message.CopyAndValidate())
                .ToList();

            if (copyOfMessages.Count < 1)
            {
                throw new ArgumentException("At least one message is required.");
            }

            if (copyOfMessages.Count > 500)
            {
                throw new ArgumentException("At most 500 messages are allowed.");
            }

            try
            {
                return await this.SendBatchRequestAsync(copyOfMessages, dryRun, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException e)
            {
                throw MessagingErrorHandler.Instance.HandleHttpRequestException(e);
            }
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
            this.fcmClientService.Dispose();
        }

        internal static FirebaseMessagingClient Create(FirebaseApp app)
        {
            var args = new Args
            {
                ClientFactory = app.Options.HttpClientFactory,
                Credential = app.Options.Credential,
                ProjectId = app.GetProjectId(),
                RetryOptions = RetryOptions.Default,
            };

            return new FirebaseMessagingClient(args);
        }

        private static void AddCommonHeaders(HttpRequestMessage request)
        {
            request.Headers.Add("X-Firebase-Client", ClientVersion);
            request.Headers.Add("X-GOOG-API-FORMAT-VERSION", "2");
        }

        private async Task<BatchResponse> SendBatchRequestAsync(
            IEnumerable<Message> messages,
            bool dryRun,
            CancellationToken cancellationToken)
        {
            var responses = new List<SendResponse>();

            var batch = this.CreateBatchRequest(
                messages,
                dryRun,
                (content, error, index, message) =>
                {
                    SendResponse sendResponse;
                    if (error != null)
                    {
                        var json = (error as ContentRetainingRequestError).Content;
                        var exception = MessagingErrorHandler.Instance.HandleHttpErrorResponse(
                            message, json);
                        sendResponse = SendResponse.FromException(exception);
                    }
                    else if (content != null)
                    {
                        sendResponse = SendResponse.FromMessageId(content.Name);
                    }
                    else
                    {
                        var exception = new FirebaseMessagingException(
                            ErrorCode.Unknown,
                            $"Unexpected batch response. Response status code: {message.StatusCode}.");
                        sendResponse = SendResponse.FromException(exception);
                    }

                    responses.Add(sendResponse);
                });

            await batch.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            return new BatchResponse(responses);
        }

        private BatchRequest CreateBatchRequest(
            IEnumerable<Message> messages,
            bool dryRun,
            BatchRequest.OnResponse<SingleMessageResponse> callback)
        {
            var batch = new BatchRequest(this.fcmClientService, FcmBatchUrl);

            foreach (var message in messages)
            {
                var body = new SendRequest()
                {
                    Message = message,
                    ValidateOnly = dryRun,
                };
                batch.Queue(new FCMClientServiceRequest(this.fcmClientService, this.restPath, body), callback);
            }

            return batch;
        }

        /// <summary>
        /// Represents the envelope message accepted by the FCM backend service, including the message
        /// payload and other options like <c>validate_only</c>.
        /// </summary>
        internal class SendRequest
        {
            [Newtonsoft.Json.JsonProperty("message")]
            public Message Message { get; set; }

            [Newtonsoft.Json.JsonProperty("validate_only")]
            public bool ValidateOnly { get; set; }
        }

        /// <summary>
        /// Represents the response messages sent by the FCM backend service when sending a single
        /// message. Primarily consists of the message ID (Name) that indicates success handoff to FCM.
        /// </summary>
        internal class SingleMessageResponse
        {
            [Newtonsoft.Json.JsonProperty("name")]
            public string Name { get; set; }
        }

        internal sealed class Args
        {
            internal HttpClientFactory ClientFactory { get; set; }

            internal GoogleCredential Credential { get; set; }

            internal string ProjectId { get; set; }

            internal RetryOptions RetryOptions { get; set; }
        }

        private sealed class FCMClientService : BaseClientService
        {
            public FCMClientService(Initializer initializer)
            : base(initializer) { }

            public override string Name => "FCM";

            public override string BaseUri => FcmBaseUrl;

            public override string BasePath => null;

            public override IList<string> Features => null;

            public override async Task<RequestError> DeserializeError(HttpResponseMessage response)
            {
                var error = await base.DeserializeError(response).ConfigureAwait(false);

                // Read the full response text here and add it to the RequestError so it can be
                // used in the batch request callback.
                // See https://github.com/googleapis/google-api-dotnet-client/issues/1632.
                var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new ContentRetainingRequestError(error, content);
            }
        }

        private sealed class ContentRetainingRequestError : RequestError
        {
            internal ContentRetainingRequestError(RequestError error, string content)
            {
                this.Code = error.Code;
                this.Message = error.Message;
                this.Errors = error.Errors;
                this.Content = content;
            }

            internal string Content { get; }
        }

        private sealed class FCMClientServiceRequest : ClientServiceRequest<string>
        {
            private readonly string restPath;
            private readonly SendRequest body;

            public FCMClientServiceRequest(FCMClientService clientService, string restPath, SendRequest body)
            : base(clientService)
            {
                this.restPath = restPath;
                this.body = body;
                this.ModifyRequest = (request) =>
                {
                    AddCommonHeaders(request);
                };
                this.InitParameters();
            }

            public override string HttpMethod => "POST";

            public override string RestPath => this.restPath;

            public override string MethodName => throw new NotImplementedException();

            protected override object GetBody()
            {
                return this.body;
            }
        }
    }
}
