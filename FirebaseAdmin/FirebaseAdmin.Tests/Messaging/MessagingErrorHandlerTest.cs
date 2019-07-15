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

using System.Net;
using System.Net.Http;
using System.Text;
using Xunit;

namespace FirebaseAdmin.Messaging.Tests
{
    public class MessagingErrorHandlerTest
    {
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

            var handler = new MessagingErrorHandler();
            var error = Assert.Throws<FirebaseMessagingException>(() => handler.ThrowIfError(resp, json));

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Null(error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void PlatformErrorWithMessagingErrorCode()
        {
            var json = @"{
                ""error"": {
                    ""status"": ""PERMISSION_DENIED"",
                    ""message"": ""Test error message"",
                    ""details"": [
                        {
                            ""@type"": ""type.googleapis.com/google.firebase.fcm.v1.FcmError"",
                            ""errorCode"": ""UNREGISTERED""
                        }
                    ]
                }
            }";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var handler = new MessagingErrorHandler();
            var error = Assert.Throws<FirebaseMessagingException>(() => handler.ThrowIfError(resp, json));

            Assert.Equal(ErrorCode.PermissionDenied, error.ErrorCode);
            Assert.Equal("Test error message", error.Message);
            Assert.Equal(MessagingErrorCode.Unregistered, error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void PlatformErrorWithoutAnyDetails()
        {
            var json = @"{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var handler = new MessagingErrorHandler();
            var error = Assert.Throws<FirebaseMessagingException>(() => handler.ThrowIfError(resp, json));

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                "Unexpected HTTP response with status: 503 (ServiceUnavailable)\n{}",
                error.Message);
            Assert.Null(error.MessagingErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }
    }
}
