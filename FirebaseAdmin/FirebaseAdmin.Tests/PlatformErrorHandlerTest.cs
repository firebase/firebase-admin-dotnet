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
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace FirebaseAdmin.Tests
{
    public class PlatformErrorHandlerTest
    {
        private static readonly TestPlatformErrorHandler ErrorHandler = new TestPlatformErrorHandler();

        [Fact]
        public void PlatformError()
        {
            var json = @"{
                ""error"": {
                    ""status"": ""UNAVAILABLE"",
                    ""message"": ""Test error message""
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void NonJsonResponse()
        {
            var text = "not json";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, text);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{text}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void PlatformErrorWithoutCode()
        {
            var json = @"{
                ""error"": {
                    ""message"": ""Test error message""
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void PlatformErrorWithoutMessage()
        {
            var json = @"{
                ""error"": {
                    ""status"": ""INVALID_ARGUMENT"",
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.InvalidArgument, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{json}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void PlatformErrorWithoutCodeOrMessage()
        {
            var json = @"{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{{}}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        private class TestPlatformErrorHandler : PlatformErrorHandler<FirebaseException>
        {
            protected override FirebaseException CreateException(FirebaseExceptionArgs args)
            {
                return new FirebaseException(args.Code, args.Message, response: args.HttpResponse);
            }
        }
    }
}
