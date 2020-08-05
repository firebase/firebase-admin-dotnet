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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Xunit;

namespace FirebaseAdmin.Auth.Jwt.Tests
{
    public class IAMSignerTest
    {
        [Fact]
        public async Task Signer()
        {
            var bytes = Encoding.UTF8.GetBytes("signature");
            var handler = new MockMessageHandler()
            {
                Response = "discovered-service-account",
            };
            var factory = new MockHttpClientFactory(handler);
            var signer = new IAMSigner(factory, GoogleCredential.FromAccessToken("token"));
            Assert.Equal("discovered-service-account", await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);

            // should only fetch account once
            Assert.Equal("discovered-service-account", await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);

            handler.Response = new IAMSigner.SignBlobResponse()
            {
                Signature = Convert.ToBase64String(bytes),
            };
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            byte[] signature = await signer.SignDataAsync(data);
            Assert.Equal(bytes, signature);
            var req = NewtonsoftJsonSerializer.Instance.Deserialize<IAMSigner.SignBlobRequest>(
                handler.LastRequestBody);
            Assert.Equal(Convert.ToBase64String(data), req.BytesToSign);
            Assert.Equal(2, handler.Calls);
            Assert.Equal("Bearer token", handler.LastRequestHeaders.Authorization?.ToString());
        }

        [Fact]
        public async Task AccountDiscoveryError()
        {
            var bytes = Encoding.UTF8.GetBytes("signature");
            var handler = new MockMessageHandler()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = new IAMSigner(factory, GoogleCredential.FromAccessToken("token"));
            var errorMessage = "Failed to determine service account ID. Make sure to initialize the SDK "
                + "with service account credentials or specify a service account "
                + "ID with iam.serviceAccounts.signBlob permission. Please refer to "
                + "https://firebase.google.com/docs/auth/admin/create-custom-tokens for "
                + "more details on creating custom tokens.";

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);
            Assert.Equal(errorMessage, ex.Message);
            Assert.IsType<HttpRequestException>(ex.InnerException);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);
            Assert.Equal(errorMessage, ex.Message);
            Assert.IsType<HttpRequestException>(ex.InnerException);
        }
    }

    public class FixedAccountIAMSignerTest
    {
        [Fact]
        public async Task Signer()
        {
            var bytes = Encoding.UTF8.GetBytes("signature");
            var handler = new MockMessageHandler()
                {
                    Response = new IAMSigner.SignBlobResponse()
                        {
                                Signature = Convert.ToBase64String(bytes),
                        },
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            byte[] signature = await signer.SignDataAsync(data);
            Assert.Equal(bytes, signature);
            var req = NewtonsoftJsonSerializer.Instance.Deserialize<IAMSigner.SignBlobRequest>(
                handler.LastRequestBody);
            Assert.Equal(Convert.ToBase64String(data), req.BytesToSign);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task WelformedSignError()
        {
            var handler = new MockMessageHandler()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Response = @"{""error"": {""message"": ""test reason""}}",
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await signer.SignDataAsync(data));

            Assert.Equal(ErrorCode.Internal, ex.ErrorCode);
            Assert.Equal("test reason", ex.Message);
            Assert.Null(ex.AuthErrorCode);
            Assert.NotNull(ex.HttpResponse);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task WelformedSignErrorWithCode()
        {
            var handler = new MockMessageHandler()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Response = @"{""error"": {""message"": ""test reason"", ""status"": ""UNAVAILABLE""}}",
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await signer.SignDataAsync(data));

            Assert.Equal(ErrorCode.Unavailable, ex.ErrorCode);
            Assert.Equal("test reason", ex.Message);
            Assert.Null(ex.AuthErrorCode);
            Assert.NotNull(ex.HttpResponse);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task UnexpectedSignError()
        {
            var handler = new MockMessageHandler()
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Response = "not json",
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await signer.SignDataAsync(data));

            Assert.Equal(ErrorCode.Internal, ex.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}not json",
                ex.Message);
            Assert.Null(ex.AuthErrorCode);
            Assert.NotNull(ex.HttpResponse);
            Assert.Null(ex.InnerException);
        }

        [Fact]
        public async Task Unavailable()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = @"{""error"": {""message"": ""test reason""}}",
            };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await signer.SignDataAsync(data));

            Assert.Equal(ErrorCode.Unavailable, ex.ErrorCode);
            Assert.Equal("test reason", ex.Message);
            Assert.Null(ex.AuthErrorCode);
            Assert.NotNull(ex.HttpResponse);
            Assert.Null(ex.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        [Fact]
        public async Task TransportError()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new HttpRequestException("Transport error"),
            };
            var factory = new MockHttpClientFactory(handler);
            var signer = this.CreateFixedAccountIAMSigner(factory);
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await signer.SignDataAsync(data));

            Assert.Equal(ErrorCode.Unknown, ex.ErrorCode);
            Assert.Null(ex.AuthErrorCode);
            Assert.Null(ex.HttpResponse);
            Assert.NotNull(ex.InnerException);
            Assert.Equal(5, handler.Calls);
        }

        private FixedAccountIAMSigner CreateFixedAccountIAMSigner(HttpClientFactory factory)
        {
            return new FixedAccountIAMSigner(new FixedAccountIAMSigner.Args()
            {
                ClientFactory = factory,
                Credential = GoogleCredential.FromAccessToken("token"),
                KeyId = "test-service-account",
                RetryOptions = RetryOptions.NoBackOff,
            });
        }
    }
}
