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
    public class RetryHttpClientInitializerTest
    {
        [Fact]
        public async Task RetryDisabledByDefault()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var factory = new MockHttpClientFactory(handler);
            var httpClient = factory.CreateHttpClient(new CreateHttpClientArgs());

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task RetryOnHttp503()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
            };
            Assert.Equal(expected, waiter.WaitTimes);
        }

        [Fact]
        public async Task RetryOnUnsuccessfulResponseDisabled()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var options = RetryOptions.Default;
            options.HandleUnsuccessfulResponseFunc = null;
            var httpClient = CreateHttpClient(handler, options, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task NoRetryOnHttp500ByDefault()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task RetryOnHttp500WhenRequested()
        {
            var handler = new MockMessageHandler()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var options = RetryOptions.Default;
            options.HandleUnsuccessfulResponseFunc =
                (resp) => resp.StatusCode == HttpStatusCode.InternalServerError;
            var httpClient = CreateHttpClient(handler, options, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4),
                TimeSpan.FromSeconds(8),
            };
            Assert.Equal(expected, waiter.WaitTimes);
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
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(3),
            };
            Assert.Equal(expected, waiter.WaitTimes);
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
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter, clock);

            var response = await httpClient.SendAsync(CreateRequest());

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal(5, handler.Calls);
            Assert.Equal(4, waiter.WaitTimes.Count);
            foreach (var timespan in waiter.WaitTimes)
            {
                // Due to the date format used in HTTP headers, the milliseconds precision gets
                // lost. Therefore the actual delay is going to be a value between 3 and 4 seconds.
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
                Response = "{}",
            };
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter);

            var response = await httpClient.SendAsync(CreateRequest());

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
            var waiter = new MockWaiter();
            var httpClient = CreateHttpClient(handler, RetryOptions.Default, waiter);

            var ex = await Assert.ThrowsAsync<Exception>(
              async () => await httpClient.SendAsync(CreateRequest()));
            Assert.Equal("transport error", ex.Message);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.FromSeconds(1),
              TimeSpan.FromSeconds(2),
              TimeSpan.FromSeconds(4),
              TimeSpan.FromSeconds(8),
            };
            Assert.Equal(expected, waiter.WaitTimes);
        }

        [Fact]
        public async Task RetryOnExceptionDisabled()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new Exception("transport error"),
            };
            var waiter = new MockWaiter();
            var options = RetryOptions.Default;
            options.HandleExceptionFunc = null;
            var httpClient = CreateHttpClient(handler, options, waiter);

            var ex = await Assert.ThrowsAsync<Exception>(
              async () => await httpClient.SendAsync(CreateRequest()));
            Assert.Equal("transport error", ex.Message);
            Assert.Equal(1, handler.Calls);
        }

        [Fact]
        public async Task BackOffFactor()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new Exception("transport error"),
            };
            var waiter = new MockWaiter();
            var options = RetryOptions.Default;
            options.BackOffFactor = 1.5;
            var httpClient = CreateHttpClient(handler, options, waiter);

            var ex = await Assert.ThrowsAsync<Exception>(
              async () => await httpClient.SendAsync(CreateRequest()));
            Assert.Equal("transport error", ex.Message);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.FromSeconds(1.5),
              TimeSpan.FromSeconds(3),
              TimeSpan.FromSeconds(6),
              TimeSpan.FromSeconds(12),
            };
            Assert.Equal(expected, waiter.WaitTimes);
        }

        [Fact]
        public async Task BackOffFactorZero()
        {
            var handler = new MockMessageHandler()
            {
                Exception = new Exception("transport error"),
            };
            var waiter = new MockWaiter();
            var options = RetryOptions.Default;
            options.BackOffFactor = 0;
            var httpClient = CreateHttpClient(handler, options, waiter);

            var ex = await Assert.ThrowsAsync<Exception>(
              async () => await httpClient.SendAsync(CreateRequest()));
            Assert.Equal("transport error", ex.Message);
            Assert.Equal(5, handler.Calls);
            var expected = new List<TimeSpan>()
            {
              TimeSpan.Zero,
              TimeSpan.Zero,
              TimeSpan.Zero,
              TimeSpan.Zero,
            };
            Assert.Equal(expected, waiter.WaitTimes);
        }

        [Fact]
        public void BackOffFactorNegative()
        {
            var options = RetryOptions.Default;
            options.BackOffFactor = -1;
            Assert.Throws<ArgumentException>(() => new RetryHttpClientInitializer(options));
        }

        private static ConfigurableHttpClient CreateHttpClient(
          MockMessageHandler handler, RetryOptions options, IWaiter waiter, IClock clock = null)
        {
            var args = new CreateHttpClientArgs();
            args.Initializers.Add(new RetryHttpClientInitializer(options, clock, waiter));

            var factory = new MockHttpClientFactory(handler);
            return factory.CreateHttpClient(args);
        }

        private static HttpRequestMessage CreateRequest()
        {
            return new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://firebase.google.com"),
            };
        }

        internal sealed class MockWaiter : IWaiter
        {
            private readonly List<TimeSpan> waitTimes = new List<TimeSpan>();

            internal IReadOnlyList<TimeSpan> WaitTimes
            {
                get => this.waitTimes;
            }

            public Task Wait(TimeSpan ts, CancellationToken cancellationToken)
            {
                this.waitTimes.Add(ts);
                return Task.CompletedTask;
            }
        }
    }
}
