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
using Google.Apis.Http;

namespace FirebaseAdmin.Util
{
    internal sealed class RetryHttpClientFactoryDecorator : IHttpClientFactory
    {
        private static readonly Func<FirebaseBackOffHandler> DefaultHandlerFunc =
            () => new FirebaseBackOffHandler();

        private readonly IHttpClientFactory factory;
        private readonly Func<FirebaseBackOffHandler> handler;

        public RetryHttpClientFactoryDecorator(
            IHttpClientFactory factory, Func<FirebaseBackOffHandler> handler = null)
        {
            this.factory = factory;
            this.handler = handler ?? DefaultHandlerFunc;
        }

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            args.Initializers.Add(new ExponentialBackOffInitializer(
                ExponentialBackOffPolicy.Exception | ExponentialBackOffPolicy.UnsuccessfulResponse503,
                this.handler));
            var client = this.factory.CreateHttpClient(args);
            client.MessageHandler.NumTries = FirebaseBackOffHandler.MaxRetries + 1;
            return client;
        }
    }
}