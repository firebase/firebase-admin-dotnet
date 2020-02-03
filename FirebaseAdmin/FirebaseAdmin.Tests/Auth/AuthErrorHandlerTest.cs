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
using FirebaseAdmin.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class AuthErrorHandlerTest
    {
        public static readonly IEnumerable<object[]> AuthErrorCodes =
            new List<object[]>()
            {
                new object[]
                {
                    "DUPLICATE_EMAIL",
                    ErrorCode.AlreadyExists,
                    AuthErrorCode.EmailAlreadyExists,
                },
                new object[]
                {
                    "DUPLICATE_LOCAL_ID",
                    ErrorCode.AlreadyExists,
                    AuthErrorCode.UidAlreadyExists,
                },
                new object[]
                {
                    "EMAIL_EXISTS",
                    ErrorCode.AlreadyExists,
                    AuthErrorCode.EmailAlreadyExists,
                },
                new object[]
                {
                    "PHONE_NUMBER_EXISTS",
                    ErrorCode.AlreadyExists,
                    AuthErrorCode.PhoneNumberAlreadyExists,
                },
                new object[]
                {
                    "USER_NOT_FOUND",
                    ErrorCode.NotFound,
                    AuthErrorCode.UserNotFound,
                },
            };

        [Theory]
        [MemberData(nameof(AuthErrorCodes))]
        public void KnownErrorCode(
            string code, ErrorCode expectedCode, AuthErrorCode expectedAuthCode)
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""{code}"",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(expectedCode, error.ErrorCode);
            Assert.Equal(expectedAuthCode, error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
            Assert.EndsWith($" ({code}).", error.Message);
        }

        [Theory]
        [MemberData(nameof(AuthErrorCodes))]
        public void KnownErrorCodeWithDetails(
            string code, ErrorCode expectedCode, AuthErrorCode expectedAuthCode)
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""{code}: Some details."",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(expectedCode, error.ErrorCode);
            Assert.Equal(expectedAuthCode, error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
            Assert.EndsWith($" ({code}): Some details.", error.Message);
        }

        [Fact]
        public void UnknownErrorCode()
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""SOMETHING_UNUSUAL"",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Internal, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{json}",
                error.Message);
            Assert.Null(error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void UnspecifiedErrorCode()
        {
            var json = $@"{{
                ""error"": {{}}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Internal, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{json}",
                error.Message);
            Assert.Null(error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void NoDetails()
        {
            var json = @"{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{{}}",
                error.Message);
            Assert.Null(error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void NonJson()
        {
            var text = "plain text";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };

            var error = AuthErrorHandler.Instance.HandleHttpErrorResponse(resp, text);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{text}",
                error.Message);
            Assert.Null(error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void DeserializeException()
        {
            var text = "plain text";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };
            var inner = new Exception("Deserialization error");

            var error = AuthErrorHandler.Instance.HandleDeserializeException(
                inner, new ResponseInfo(resp, text));

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                $"Error while parsing Auth service response. Deserialization error: {text}",
                error.Message);
            Assert.Equal(AuthErrorCode.UnexpectedResponse, error.AuthErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void HttpRequestException()
        {
            var exception = new HttpRequestException("network error");

            var error = AuthErrorHandler.Instance.HandleHttpRequestException(exception);

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: network error", error.Message);
            Assert.Null(error.AuthErrorCode);
            Assert.Null(error.HttpResponse);
            Assert.Same(exception, error.InnerException);
        }
    }
}
