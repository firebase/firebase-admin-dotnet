// Copyright 2026, Google Inc. All rights reserved.
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
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Util;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckErrorHandlerTest
    {
        [Fact]
        public void PlatformError()
        {
            var json = @"{
                ""error"": {
                    ""status"": ""PERMISSION_DENIED"",
                    ""message"": ""Test error message""
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.Forbidden,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.PermissionDenied, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal(AppCheckErrorCode.ServiceError, error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void DeserializeException()
        {
            var text = "plain text";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };
            var inner = new Exception("Deserialization error");

            var error = AppCheckErrorHandler.Instance.HandleDeserializeException(
                inner, new ResponseInfo(resp, text));

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                $"Error parsing response from App Check service. Deserialization error: {text}",
                error.Message);
            Assert.Equal(AppCheckErrorCode.ServiceError, error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void HttpRequestException()
        {
            var exception = new HttpRequestException("network error");

            var error = AppCheckErrorHandler.Instance.HandleHttpRequestException(exception);

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: network error", error.Message);
            Assert.Equal(AppCheckErrorCode.ServiceError, error.AppCheckErrorCode);
            Assert.Null(error.HttpResponse);
            Assert.Same(exception, error.InnerException);
        }
    }
}
