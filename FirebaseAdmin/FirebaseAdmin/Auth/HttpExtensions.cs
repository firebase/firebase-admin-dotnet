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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;
using Google.Apis.Json;

namespace FirebaseAdmin.Auth
{
    internal static class HttpExtensions
    {
        internal static async Task<ResponseInfo> SendAndReadAsync(
            this ConfigurableHttpClient httpClient,
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await httpClient.SendAsync(request, cancellationToken)
                    .ConfigureAwait(false);
                var json = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait(false);
                AuthErrorHandler.Instance.ThrowIfError(response, json);

                return new ResponseInfo()
                {
                    HttpResponse = response,
                    Body = json,
                };
            }
            catch (HttpRequestException e)
            {
                throw e.ToFirebaseAuthException();
            }
        }

        internal static FirebaseAuthException ToFirebaseAuthException(
            this HttpRequestException exception,
            string prefix = "",
            AuthErrorCode? errorCode = null)
        {
            var temp = exception.ToFirebaseException();
            return new FirebaseAuthException(
                temp.ErrorCode,
                $"{prefix}{temp.Message}",
                errorCode,
                inner: temp.InnerException,
                response: temp.HttpResponse);
        }

        internal class ResponseInfo
        {
            internal HttpResponseMessage HttpResponse { get; set; }

            internal string Body { get; set; }

            internal ParsedResponseInfo<TResult> SafeDeserialize<TResult>()
            {
                try
                {
                    var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<TResult>(this.Body);
                    return new ParsedResponseInfo<TResult>()
                    {
                        Result = parsed,
                        HttpResponse = this.HttpResponse,
                        Body = this.Body,
                    };
                }
                catch (Exception e)
                {
                    throw new FirebaseAuthException(
                        ErrorCode.Unknown,
                        "Error while parsing Auth service response.",
                        AuthErrorCode.UnexpectedResponse,
                        e,
                        this.HttpResponse);
                }
            }
        }

        internal sealed class ParsedResponseInfo<T> : ResponseInfo
        {
            internal T Result { get; set; }
        }
    }
}