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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Http;
using Google.Apis.Json;
using Google.Apis.Util;

namespace FirebaseAdmin
{
    /// <summary>
    /// A collection of extension methods for internal use in the SDK.
    /// </summary>
    internal static class Extensions
    {
        private static readonly IReadOnlyList<SocketError> NetworkAvailabilityErrors =
            new List<SocketError>()
            {
                SocketError.HostDown,
                SocketError.HostNotFound,
                SocketError.HostUnreachable,
                SocketError.NetworkDown,
                SocketError.NetworkUnreachable,
            };

        /// <summary>
        /// Extracts and returns the underlying <see cref="ServiceAccountCredential"/> from a
        /// <see cref="GoogleCredential"/>. Returns null if the <c>GoogleCredential</c> is not
        /// based on a service account.
        /// </summary>
        /// <returns>A service account credential if available, or null.</returns>
        /// <param name="credential">The Google credential from which to extract service account
        /// credentials.</param>
        public static ServiceAccountCredential ToServiceAccountCredential(
            this GoogleCredential credential)
        {
            if (credential.UnderlyingCredential is GoogleCredential)
            {
                return ((GoogleCredential)credential.UnderlyingCredential)
                    .ToServiceAccountCredential();
            }

            return credential.UnderlyingCredential as ServiceAccountCredential;
        }

        /// <summary>
        /// Creates a default (unauthenticated) <see cref="ConfigurableHttpClient"/> from the
        /// factory.
        /// </summary>
        /// <returns>An HTTP client that can be used to make unauthenticated requests.</returns>
        /// <param name="clientFactory">The <see cref="HttpClientFactory"/> used to create
        /// the HTTP client.</param>
        public static ConfigurableHttpClient CreateDefaultHttpClient(
            this HttpClientFactory clientFactory)
        {
            return clientFactory.CreateHttpClient(new CreateHttpClientArgs());
        }

        /// <summary>
        /// Makes a JSON POST request using the given parameters.
        /// </summary>
        /// <returns>An <see cref="HttpRequestMessage"/> representing the response to the
        /// POST request.</returns>
        /// <typeparam name="T">Type of the object that will be serialized into JSON.</typeparam>
        /// <param name="client">The <see cref="HttpClient"/> used to make the request.</param>
        /// <param name="requestUri">URI for the outgoing request.</param>
        /// <param name="body">The object that will be serialized as the JSON body.</param>
        /// <param name="cancellationToken">A cancellation token to monitor the asynchronous
        /// operation.</param>
        public static async Task<HttpResponseMessage> PostJsonAsync<T>(
            this HttpClient client, string requestUri, T body, CancellationToken cancellationToken)
        {
            var content = NewtonsoftJsonSerializer.Instance.CreateJsonHttpContent(body);
            return await client.PostAsync(requestUri, content, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Serializes the <paramref name="body"/> into JSON, and wraps the result in an instance
        /// of <see cref="HttpContent"/>, which can be included in an outgoing HTTP request.
        /// </summary>
        /// <returns>An instance of <see cref="HttpContent"/> containing the JSON representation
        /// of <paramref name="body"/>.</returns>
        /// <param name="serializer">The JSON serializer to serialize the given object.</param>
        /// <param name="body">The object that will be serialized into JSON.</param>
        public static HttpContent CreateJsonHttpContent(
            this NewtonsoftJsonSerializer serializer, object body)
        {
            var payload = serializer.Serialize(body);
            return new StringContent(payload, Encoding.UTF8, "application/json");
        }

        /// <summary>
        /// Returns a Unix-styled timestamp (seconds from epoch) from the <see cref="IClock"/>.
        /// </summary>
        /// <returns>Number of seconds since epoch.</returns>
        /// <param name="clock">The <see cref="IClock"/> used to generate the timestamp.</param>
        public static long UnixTimestamp(this IClock clock)
        {
            var timeSinceEpoch = clock.UtcNow.Subtract(new DateTime(1970, 1, 1));
            return (long)timeSinceEpoch.TotalSeconds;
        }

        /// <summary>
        /// Disposes a lazy-initialized object if the object has already been created.
        /// </summary>
        /// <param name="lazy">The lazy initializer containing a disposable object.</param>
        /// <typeparam name="T">Type of the object that needs to be disposed.</typeparam>
        public static void DisposeIfCreated<T>(this Lazy<T> lazy)
        where T : IDisposable
        {
            if (lazy.IsValueCreated)
            {
                lazy.Value.Dispose();
            }
        }

        /// <summary>
        /// Creates a shallow copy of a collection of key-value pairs.
        /// </summary>
        public static IReadOnlyDictionary<TKey, TValue> Copy<TKey, TValue>(
            this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            var copy = new Dictionary<TKey, TValue>();
            foreach (var entry in source)
            {
                copy[entry.Key] = entry.Value;
            }

            return copy;
        }

        public static FirebaseException ToFirebaseException(this HttpRequestException exception)
        {
            ErrorCode code = ErrorCode.Unknown;
            string message = "Unknown error while making a remote service call";
            if (exception.InnerException is SocketException)
            {
                var socketException = (SocketException)exception.InnerException;
                if (socketException.SocketErrorCode == SocketError.TimedOut)
                {
                    code = ErrorCode.DeadlineExceeded;
                    message = "Timed out while making an API call";
                }
                else if (NetworkAvailabilityErrors.Contains(socketException.SocketErrorCode))
                {
                    code = ErrorCode.Unavailable;
                    message = "Failed to establish a connection";
                }
            }

            return new FirebaseException(code, $"{message}: {exception.Message}", exception);
        }
    }
}
