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
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Util.Tests
{
    public class ErrorHandlingHttpClientTest
    {
        [Fact]
        public async Task SuccessfulRequest()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""foo"": ""bar""}",
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory));

            var response = await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                this.CreateRequest());

            Assert.NotNull(response.HttpResponse);
            Assert.Equal(handler.Response, response.Body);
            Assert.Single(response.Result);
            Assert.Equal("bar", response.Result["foo"]);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task SuccessfulAuthorizedRequest()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""foo"": ""bar""}",
            };
            var factory = new MockHttpClientFactory(handler);
            var credential = GoogleCredential.FromAccessToken("test-token");
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory, credential));

            var response = await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                this.CreateRequest());

            Assert.NotNull(response.HttpResponse);
            Assert.Equal(handler.Response, response.Body);
            Assert.Single(response.Result);
            Assert.Equal("bar", response.Result["foo"]);
            Assert.Equal(1, handler.Calls);
            Assert.Equal(
                "Bearer test-token",
                handler.LastRequestHeaders.GetValues("Authorization").First());
        }

        [Fact]
        public async Task NetworkError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Low-level network error"),
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory));

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                    this.CreateRequest()));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal("Network error", exception.Message);
            Assert.Same(handler.Exception, exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }

        [Fact]
        public async Task ErrorResponse()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory));

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                    this.CreateRequest()));

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal("Example error message: {}", exception.Message);
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);

            var errorPayload = await exception.HttpResponse.Content.ReadAsStringAsync();
            Assert.Equal("{}", errorPayload);
        }

        [Fact]
        public async Task DeserializeError()
        {
            var handler = new MockMessageHandler()
            {
                Response = "not json",
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory));

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                    this.CreateRequest()));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal("Response parse error", exception.Message);
            Assert.NotNull(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);

            var errorPayload = await exception.HttpResponse.Content.ReadAsStringAsync();
            Assert.Equal("not json", errorPayload);
        }

        [Fact]
        public async Task CustomDeserializer()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{""foo"": ""bar""}",
            };
            var factory = new MockHttpClientFactory(handler);
            var deserializer = new TestResponseDeserializer();
            var args = this.CreateArgs(factory);
            args.Deserializer = deserializer;
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(args);

            var response = await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                this.CreateRequest());

            Assert.NotNull(response.HttpResponse);
            Assert.Equal(handler.Response, response.Body);
            Assert.Single(response.Result);
            Assert.Equal("bar", response.Result["foo"]);
            Assert.Equal(1, handler.Calls);
            Assert.Equal(1, deserializer.Count);
        }

        [Fact]
        public void NoHttpClientFactory()
        {
            var args = this.CreateArgs(null);

            Assert.Throws<ArgumentNullException>(
                () => new ErrorHandlingHttpClient<FirebaseException>(args));
        }

        [Fact]
        public void NoRequestExceptionHandler()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var args = this.CreateArgs(factory);
            args.RequestExceptionHandler = null;

            Assert.Throws<ArgumentNullException>(
                () => new ErrorHandlingHttpClient<FirebaseException>(args));
        }

        [Fact]
        public void NoDeserializeExceptionHandler()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var args = this.CreateArgs(factory);
            args.DeserializeExceptionHandler = null;

            Assert.Throws<ArgumentNullException>(
                () => new ErrorHandlingHttpClient<FirebaseException>(args));
        }

        [Fact]
        public void NoErrorResponseHandler()
        {
            var handler = new MockMessageHandler()
            {
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var args = this.CreateArgs(factory);
            args.ErrorResponseHandler = null;

            Assert.Throws<ArgumentNullException>(
                () => new ErrorHandlingHttpClient<FirebaseException>(args));
        }

        [Fact]
        public async Task RetryOnErrorResponse()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var args = this.CreateArgs(factory);
            args.RetryOptions = this.RetryOptionsWithoutBackOff();
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(args);

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                    this.CreateRequest()));

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal("Example error message: {}", exception.Message);
            Assert.Null(exception.InnerException);
            Assert.NotNull(exception.HttpResponse);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public async Task RetryOnNetworkError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Low-level network error"),
            };
            var factory = new MockHttpClientFactory(handler);
            var args = this.CreateArgs(factory);
            args.RetryOptions = this.RetryOptionsWithoutBackOff();
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(args);

            var exception = await Assert.ThrowsAsync<FirebaseException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                    this.CreateRequest()));

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal("Network error", exception.Message);
            Assert.Same(handler.Exception, exception.InnerException);
            Assert.Null(exception.HttpResponse);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public async Task Dispose()
        {
            var handler = new MockMessageHandler()
            {
                Response = @"{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = new ErrorHandlingHttpClient<FirebaseException>(
                this.CreateArgs(factory));

            httpClient.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(
                async () => await httpClient.SendAndDeserializeAsync<Dictionary<string, string>>(
                this.CreateRequest()));
        }

        private ErrorHandlingHttpClientArgs<FirebaseException> CreateArgs(
            HttpClientFactory factory, GoogleCredential credential = null)
        {
            return new ErrorHandlingHttpClientArgs<FirebaseException>()
            {
                HttpClientFactory = factory,
                Credential = credential,
                ErrorResponseHandler = new TestHttpErrorResponseHandler(),
                RequestExceptionHandler = new TestRequestExceptionHandler(),
                DeserializeExceptionHandler = new TestDeserializeExceptionHandler(),
            };
        }

        private HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://firebase.google.com"),
            };
        }

        private RetryOptions RetryOptionsWithoutBackOff()
        {
            var copy = RetryOptions.Default;
            copy.BackOffFactor = 0;
            return copy;
        }

        private class TestHttpErrorResponseHandler : IHttpErrorResponseHandler<FirebaseException>
        {
            public FirebaseException HandleHttpErrorResponse(HttpResponseMessage response, string body)
            {
                return new FirebaseException(
                    ErrorCode.Internal, $"Example error message: {body}", response: response);
            }
        }

        private class TestDeserializeExceptionHandler : IDeserializeExceptionHandler<FirebaseException>
        {
            public FirebaseException HandleDeserializeException(
                Exception exception, ResponseInfo responseInfo)
            {
                return new FirebaseException(
                    ErrorCode.Unknown, "Response parse error", exception, responseInfo.HttpResponse);
            }
        }

        private class TestRequestExceptionHandler : IHttpRequestExceptionHandler<FirebaseException>
        {
            public FirebaseException HandleHttpRequestException(HttpRequestException exception)
            {
                return new FirebaseException(ErrorCode.Unknown, "Network error", exception);
            }
        }

        private class TestResponseDeserializer : IHttpResponseDeserializer
        {
            internal int Count { get; private set; }

            public T Deserialize<T>(string body)
            {
                this.Count++;
                return NewtonsoftJsonSerializer.Instance.Deserialize<T>(body);
            }
        }
    }
}
