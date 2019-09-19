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
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Tests;
using Google.Apis.Http;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Util.Tests
{
    public class FirebaseBackOffHandlerTest
    {
        [Fact]
        public async Task RetryOnHttp503()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = @"{""foo"": ""bar""}",
            };
            var backOffHandler = new WaitDisabledFirebaseBackOffHandler();
            var httpClient = this.CreateAuthorizedHttpClient(handler, backOffHandler);

            var response = await httpClient.SendAsync(this.CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
            };
            Assert.Equal(expected, backOffHandler.WaitTimes);
        }

        [Fact]
        public async Task RetryAfterSeconds()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ApplyHeaders = (headers, contentHeaders) =>
                {
                    headers.RetryAfter = RetryConditionHeaderValue.Parse("3");
                },
                Response = @"{""foo"": ""bar""}",
            };
            var backOffHandler = new WaitDisabledFirebaseBackOffHandler();
            var httpClient = this.CreateAuthorizedHttpClient(handler, backOffHandler);

            var response = await httpClient.SendAsync(this.CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
            };
            Assert.Equal(expected, backOffHandler.WaitTimes);
        }

        [Fact]
        public async Task RetryAfterTimestamp()
        {
            var clock = new MockClock();
            var timestamp = clock.UtcNow.AddSeconds(4).ToString("r");
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ApplyHeaders = (headers, contentHeaders) =>
                {
                    headers.RetryAfter = RetryConditionHeaderValue.Parse(timestamp);
                },
                Response = @"{""foo"": ""bar""}",
            };
            var backOffHandler = new WaitDisabledFirebaseBackOffHandler(clock);
            var httpClient = this.CreateAuthorizedHttpClient(handler, backOffHandler);

            var response = await httpClient.SendAsync(this.CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            Assert.Equal(4, backOffHandler.WaitTimes.Count);
            foreach (var timespan in backOffHandler.WaitTimes)
            {
                Assert.True(timespan.TotalSeconds > 3.0 && timespan.TotalSeconds <= 4.0);
            }
        }

        [Fact]
        public async Task RetryAfterTooLarge()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                ApplyHeaders = (headers, contentHeaders) =>
                {
                    headers.RetryAfter = RetryConditionHeaderValue.Parse("300");
                },
                Response = @"{""foo"": ""bar""}",
            };
            var backOffHandler = new WaitDisabledFirebaseBackOffHandler();
            var httpClient = this.CreateAuthorizedHttpClient(handler, backOffHandler);

            var response = await httpClient.SendAsync(this.CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task RetryOnException()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new Exception("transport error"),
            };
            var backOffHandler = new WaitDisabledFirebaseBackOffHandler();
            var httpClient = this.CreateAuthorizedHttpClient(handler, backOffHandler);

            var ex = await Assert.ThrowsAsync<Exception>(
              async () => await httpClient.SendAsync(this.CreateRequest()));
            Assert.Equal("transport error", ex.Message);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.FromSeconds(1),
              TimeSpan.FromSeconds(2),
              TimeSpan.FromSeconds(4),
              TimeSpan.FromSeconds(8),
            };
            Assert.Equal(expected, backOffHandler.WaitTimes);
        }

        private ConfigurableHttpClient CreateAuthorizedHttpClient(
          MockMessageHandler handler, FirebaseBackOffHandler backOffHandler)
        {
            var args = new CreateHttpClientArgs();
            var factory = new RetryHttpClientFactoryDecorator(
                new MockHttpClientFactory(handler),
                () => backOffHandler);
            return factory.CreateHttpClient(args);
        }

        private HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://firebase.google.com"),
            };
        }

        private class WaitDisabledFirebaseBackOffHandler : FirebaseBackOffHandler
        {
            private readonly List<TimeSpan> waitTimes = new List<TimeSpan>();

            internal WaitDisabledFirebaseBackOffHandler(IClock clock = null)
            : base(clock: clock) { }

            internal IReadOnlyList<TimeSpan> WaitTimes
            {
                get => this.waitTimes;
            }

            protected override async Task Wait(TimeSpan ts, CancellationToken cancellationToken)
            {
                this.waitTimes.Add(ts);
                await base.Wait(TimeSpan.Zero, cancellationToken);
            }
        }
    }
}
