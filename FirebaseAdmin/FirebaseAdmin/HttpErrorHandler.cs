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
using FirebaseAdmin.Util;

namespace FirebaseAdmin
{
    /// <summary>
    /// Base class for handling HTTP error responses.
    /// </summary>
    internal abstract class HttpErrorHandler<T> : IHttpErrorResponseHandler<T>
    where T : FirebaseException
    {
        private static readonly IReadOnlyDictionary<HttpStatusCode, ErrorCode> HttpErrorCodes =
            new Dictionary<HttpStatusCode, ErrorCode>()
            {
                { HttpStatusCode.BadRequest, ErrorCode.InvalidArgument },
                { HttpStatusCode.Unauthorized, ErrorCode.Unauthenticated },
                { HttpStatusCode.Forbidden, ErrorCode.PermissionDenied },
                { HttpStatusCode.NotFound, ErrorCode.NotFound },
                { HttpStatusCode.Conflict, ErrorCode.Conflict },
                { (HttpStatusCode)429, ErrorCode.ResourceExhausted },
                { HttpStatusCode.InternalServerError, ErrorCode.Internal },
                { HttpStatusCode.ServiceUnavailable, ErrorCode.Unavailable },
            };

        public T HandleHttpErrorResponse(HttpResponseMessage response, string body)
        {
            var args = this.CreateExceptionArgs(response, body);
            return this.CreateException(args);
        }

        /// <summary>
        /// Parses the given HTTP response to extract an error code and message from it.
        /// </summary>
        /// <param name="response">HTTP response message to process.</param>
        /// <param name="body">The response body read from the message.</param>
        /// <returns>A <see cref="FirebaseExceptionArgs"/> object containing error code and message.
        /// Both the code and the message should not be null or empty.</returns>
        protected virtual FirebaseExceptionArgs CreateExceptionArgs(
            HttpResponseMessage response, string body)
        {
            ErrorCode code;
            if (!HttpErrorCodes.TryGetValue(response.StatusCode, out code))
            {
                code = ErrorCode.Unknown;
            }

            var message = "Unexpected HTTP response with status: "
                + $"{(int)response.StatusCode} ({response.StatusCode})"
                + $"{Environment.NewLine}{body}";
            return new FirebaseExceptionArgs()
            {
                Code = code,
                Message = message,
                HttpResponse = response,
                ResponseBody = body,
            };
        }

        /// <summary>
        /// Creates a new <see cref="FirebaseException"/> from the specified arguments.
        /// </summary>
        /// <param name="args">A <see cref="FirebaseExceptionArgs"/> instance containing error
        /// code, message and other details.</param>
        /// <returns>A <see cref="FirebaseException"/> object.</returns>
        protected abstract T CreateException(FirebaseExceptionArgs args);

        internal class FirebaseExceptionArgs
        {
            internal ErrorCode Code { get; set; }

            internal string Message { get; set; }

            internal HttpResponseMessage HttpResponse { get; set; }

            internal string ResponseBody { get; set; }
        }
    }
}
