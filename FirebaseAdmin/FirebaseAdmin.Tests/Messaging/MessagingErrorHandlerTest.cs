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

namespace FirebaseAdmin.Messaging.Tests
{
    public class MessagingErrorHandlerTest
    {
        public static readonly IEnumerable<object[]> MessagingErrorCodes =
            new List<object[]>()
            {
                new object[] { "APNS_AUTH_ERROR", MessagingErrorCode.ThirdPartyAuthError },
                new object[] { "INTERNAL", MessagingErrorCode.Internal },
                new object[] { "INVALID_ARGUMENT", MessagingErrorCode.InvalidArgument },
                new object[] { "QUOTA_EXCEEDED", MessagingErrorCode.QuotaExceeded },
                new object[] { "SENDER_ID_MISMATCH", MessagingErrorCode.SenderIdMismatch },
                new object[] { "THIRD_PARTY_AUTH_ERROR", MessagingErrorCode.ThirdPartyAuthError },
                new object[] { "UNAVAILABLE", MessagingErrorCode.Unavailable },
                new object[] { "UNREGISTERED", MessagingErrorCode.Unregistered },
            };

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

            var error = MessagingErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Null(error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Theory]
        [MemberData(nameof(MessagingErrorCodes))]
        public void KnownMessagingErrorCode(string code, MessagingErrorCode expected)
        {
            var json = $@"{{
                ""error"": {{
                    ""status"": ""PERMISSION_DENIED"",
                    ""message"": ""Test error message"",
                    ""details"": [
                        {{
                            ""@type"": ""type.googleapis.com/google.firebase.fcm.v1.FcmError"",
                            ""errorCode"": ""{code}""
                        }}
                    ]
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = MessagingErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.PermissionDenied, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal(expected, error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void UnknownMessagingErrorCode()
        {
            var json = @"{
                ""error"": {
                    ""status"": ""PERMISSION_DENIED"",
                    ""message"": ""Test error message"",
                    ""details"": [
                        {
                            ""@type"": ""type.googleapis.com/google.firebase.fcm.v1.FcmError"",
                            ""errorCode"": ""SOMETHING_UNUSUAL""
                        }
                    ]
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = MessagingErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.PermissionDenied, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Null(error.MessagingErrorCode);
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

            var error = MessagingErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{{}}",
                error.Message);
            Assert.Null(error.MessagingErrorCode);
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

            var error = MessagingErrorHandler.Instance.HandleDeserializeException(
                inner, new ResponseInfo(resp, text));

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                $"Error parsing response from FCM. Deserialization error: {text}",
                error.Message);
            Assert.Null(error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void HttpRequestException()
        {
            var exception = new HttpRequestException("network error");

            var error = MessagingErrorHandler.Instance.HandleHttpRequestException(exception);

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: network error", error.Message);
            Assert.Null(error.MessagingErrorCode);
            Assert.Null(error.HttpResponse);
            Assert.Same(exception, error.InnerException);
        }
    }
}
