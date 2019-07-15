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
using System.Net.Sockets;
using Xunit;

namespace FirebaseAdmin.Tests
{
    public class FirebaseExceptionTest
    {
        [Fact]
        public void ErrorCodeAndMessage()
        {
            var exception = new FirebaseException(ErrorCode.Internal, "Test error message");

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal("Test error message", exception.Message);
            Assert.Null(exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }

        [Fact]
        public void InnerException()
        {
            var inner = new Exception("Inner exception");
            var exception = new FirebaseException(ErrorCode.Internal, "Test error message", inner);

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal("Test error message", exception.Message);
            Assert.Same(inner, exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }

        [Fact]
        public void HttpResponse()
        {
            var inner = new Exception("Inner exception");
            var resp = new HttpResponseMessage();
            var exception = new FirebaseException(ErrorCode.Internal, "Test error message", inner, resp);

            Assert.Equal(ErrorCode.Internal, exception.ErrorCode);
            Assert.Equal("Test error message", exception.Message);
            Assert.Same(inner, exception.InnerException);
            Assert.Same(resp, exception.HttpResponse);
        }

        [Fact]
        public void TimeOutError()
        {
            var socketError = new SocketException((int)SocketError.TimedOut);
            var httpError = new HttpRequestException("Test error message", socketError);
            var exception = httpError.ToFirebaseException();

            Assert.Equal(ErrorCode.DeadlineExceeded, exception.ErrorCode);
            Assert.Equal(
                "Timed out while making an API call: Test error message", exception.Message);
            Assert.Same(httpError, exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }

        [Fact]
        public void NetworkUnavailableError()
        {
            var socketErrorCodes = new List<SocketError>()
            {
                SocketError.HostDown,
                SocketError.HostNotFound,
                SocketError.HostUnreachable,
                SocketError.NetworkDown,
                SocketError.NetworkUnreachable,
            };

            foreach (var code in socketErrorCodes)
            {
                var socketError = new SocketException((int)code);
                var httpError = new HttpRequestException("Test error message", socketError);
                var exception = httpError.ToFirebaseException();

                Assert.Equal(ErrorCode.Unavailable, exception.ErrorCode);
                Assert.Equal(
                    "Failed to establish a connection: Test error message", exception.Message);
                Assert.Same(httpError, exception.InnerException);
                Assert.Null(exception.HttpResponse);
            }
        }

        [Fact]
        public void UnknownLowLevelError()
        {
            var httpError = new HttpRequestException("Test error message");
            var exception = httpError.ToFirebaseException();

            Assert.Equal(ErrorCode.Unknown, exception.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: Test error message",
                exception.Message);
            Assert.Same(httpError, exception.InnerException);
            Assert.Null(exception.HttpResponse);
        }
    }
}
