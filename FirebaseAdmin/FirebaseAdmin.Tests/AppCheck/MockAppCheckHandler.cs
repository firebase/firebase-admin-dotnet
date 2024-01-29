using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Json;

namespace FirebaseAdmin.Tests.AppCheck
{
    internal sealed class MockAppCheckHandler : CountingAppCheckHandler
    {
        private readonly List<IncomingRequest> requests = new List<IncomingRequest>();

        public MockAppCheckHandler()
        {
            this.StatusCode = HttpStatusCode.OK;
        }

        public delegate void SetHeaders(HttpResponseHeaders respHeaders, HttpContentHeaders contentHeaders);

        public delegate object GetResponse(IncomingRequest request);

        public delegate HttpStatusCode GetStatusCode(IncomingRequest request);

        public IReadOnlyList<IncomingRequest> Requests
        {
            get => this.requests;
        }

        public string LastRequestBody
        {
            get => this.requests.LastOrDefault()?.Body;
        }

        public HttpRequestHeaders LastRequestHeaders
        {
            get => this.requests.LastOrDefault()?.Headers;
        }

        public HttpStatusCode StatusCode { get; set; }

        public object Response { get; set; }

        public Exception Exception { get; set; }

        public SetHeaders ApplyHeaders { get; set; }

        public GetResponse GenerateResponse { get; set; }

        public GetStatusCode GenerateStatusCode { get; set; }

        protected override async Task<HttpResponseMessage> DoSendAsync(
            HttpRequestMessage request, int count, CancellationToken cancellationToken)
        {
            var incomingRequest = await IncomingRequest.CreateAsync(request);
            this.requests.Add(incomingRequest);

            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            if (this.Exception != null)
            {
                tcs.SetException(this.Exception);
                return await tcs.Task;
            }

            if (this.GenerateResponse != null)
            {
                this.Response = this.GenerateResponse(incomingRequest);
            }

            if (this.GenerateStatusCode != null)
            {
                this.StatusCode = this.GenerateStatusCode(incomingRequest);
            }

            string json;
            if (this.Response is byte[])
            {
                json = Encoding.UTF8.GetString(this.Response as byte[]);
            }
            else if (this.Response is string)
            {
                json = this.Response as string;
            }
            else if (this.Response is IList<string>)
            {
                json = (this.Response as IList<string>)[count - 1];
            }
            else
            {
                json = NewtonsoftJsonSerializer.Instance.Serialize(this.Response);
            }

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var resp = new HttpResponseMessage();
            resp.StatusCode = this.StatusCode;
            resp.Content = content;
            if (this.ApplyHeaders != null)
            {
                this.ApplyHeaders(resp.Headers, content.Headers);
            }

            tcs.SetResult(resp);
            return await tcs.Task;
        }

        internal sealed class IncomingRequest
        {
            internal HttpMethod Method { get; private set; }

            internal Uri Url { get; private set; }

            internal HttpRequestHeaders Headers { get; private set; }

            internal string Body { get; private set; }

            internal static async Task<IncomingRequest> CreateAsync(HttpRequestMessage request)
            {
                return new IncomingRequest()
                {
                    Method = request.Method,
                    Url = request.RequestUri,
                    Headers = request.Headers,
                    Body = request.Content != null ? await request.Content.ReadAsStringAsync() : null,
                };
            }
        }
    }
}
