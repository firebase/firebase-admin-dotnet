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
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Xunit;
using FirebaseAdmin.Tests;

namespace FirebaseAdmin.Auth.Tests
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

            handler.Response = new SignBlobResponse()
            {
                Signature = Convert.ToBase64String(bytes),
            };
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            byte[] signature = await signer.SignDataAsync(data);
            Assert.Equal(bytes, signature);
            var req = NewtonsoftJsonSerializer.Instance.Deserialize<SignBlobRequest>(
                handler.Request);
            Assert.Equal(Convert.ToBase64String(data), req.BytesToSign);
            Assert.Equal(2, handler.Calls);
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
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await signer.GetKeyIdAsync());
            Assert.Equal(1, handler.Calls);
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
                    Response = new SignBlobResponse()
                        {
                                Signature = Convert.ToBase64String(bytes),
                        },
                };
            var factory = new MockHttpClientFactory(handler);
            var signer = new FixedAccountIAMSigner(
                factory, GoogleCredential.FromAccessToken("token"), "test-service-account");
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            byte[] signature = await signer.SignDataAsync(data);
            Assert.Equal(bytes, signature);
            var req = NewtonsoftJsonSerializer.Instance.Deserialize<SignBlobRequest>(
                handler.Request);
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
            var signer = new FixedAccountIAMSigner(
                factory, GoogleCredential.FromAccessToken("token"), "test-service-account");
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseException>(
                async () => await signer.SignDataAsync(data));
            Assert.Equal("test reason", ex.Message);
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
            var signer = new FixedAccountIAMSigner(
                factory, GoogleCredential.FromAccessToken("token"), "test-service-account");
            Assert.Equal("test-service-account", await signer.GetKeyIdAsync());
            byte[] data = Encoding.UTF8.GetBytes("Hello world");
            var ex = await Assert.ThrowsAsync<FirebaseException>(
                async () => await signer.SignDataAsync(data));
            Assert.Contains("not json", ex.Message);
        }
    }
}
