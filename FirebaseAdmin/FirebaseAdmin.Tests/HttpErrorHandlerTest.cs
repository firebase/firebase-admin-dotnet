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
using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace FirebaseAdmin.Tests
{
    public class HttpErrorHandlerTest
    {
        public static readonly IEnumerable<object[]> HttpErrorCodes =
            new List<object[]>()
            {
                new object[] { HttpStatusCode.BadRequest, ErrorCode.InvalidArgument },
                new object[] { HttpStatusCode.Unauthorized, ErrorCode.Unauthenticated },
                new object[] { HttpStatusCode.Forbidden, ErrorCode.PermissionDenied },
                new object[] { HttpStatusCode.NotFound, ErrorCode.NotFound },
                new object[] { HttpStatusCode.Conflict, ErrorCode.Conflict },
                new object[] { (HttpStatusCode)429, ErrorCode.ResourceExhausted },
                new object[] { HttpStatusCode.InternalServerError, ErrorCode.Internal },
                new object[] { HttpStatusCode.ServiceUnavailable, ErrorCode.Unavailable },
            };

        private static readonly TestHttpErrorHandler ErrorHandler = new TestHttpErrorHandler();

        [Theory]
        [MemberData(nameof(HttpErrorCodes))]
        public void KnownHttpStatusCode(HttpStatusCode statusCode, ErrorCode expected)
        {
            var json = "{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(expected, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: {(int)statusCode} ({statusCode}){Environment.NewLine}{json}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Theory]
        [MemberData(nameof(HttpErrorCodes))]
        public void NonJsonResponse(HttpStatusCode statusCode, ErrorCode expected)
        {
            var text = "not json";
            var resp = new HttpResponseMessage()
            {
                StatusCode = statusCode,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, text);

            Assert.Equal(expected, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: {(int)statusCode} ({statusCode}){Environment.NewLine}{text}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void UnknownHttpStatusCode()
        {
            var json = "{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.MethodNotAllowed,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = ErrorHandler.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 405 (MethodNotAllowed){Environment.NewLine}{json}",
                error.Message);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        private class TestHttpErrorHandler : HttpErrorHandler<FirebaseException>
        {
            protected override FirebaseException CreateException(FirebaseExceptionArgs args)
            {
                return new FirebaseException(args.Code, args.Message, response: args.HttpResponse);
            }
        }
  }
}
