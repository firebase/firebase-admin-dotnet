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

namespace FirebaseAdmin
{
    internal class HttpErrorHandler
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

        internal void ThrowIfError(HttpResponseMessage response, string json)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var info = this.ExtractErrorInfo(response, json);
            var args = new FirebaseExceptionArgs()
            {
                Code = info.Code,
                Message = info.Message,
                HttpResponse = response,
                ResponseBody = json,
            };
            throw this.CreateException(args);
        }

        protected virtual ErrorInfo ExtractErrorInfo(HttpResponseMessage response, string json)
        {
            ErrorCode code;
            if (!HttpErrorCodes.TryGetValue(response.StatusCode, out code))
            {
                code = ErrorCode.Unknown;
            }

            var message = "Unexpected HTTP response with status: "
                + $"{(int)response.StatusCode} ({response.StatusCode})"
                + $"{Environment.NewLine}{json}";
            return new ErrorInfo()
            {
                Code = code,
                Message = message,
            };
        }

        protected virtual FirebaseException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseException(args.Code, args.Message, response: args.HttpResponse);
        }

        internal sealed class ErrorInfo
        {
            internal ErrorCode Code { get; set; }

            internal string Message { get; set; }
        }

        internal sealed class FirebaseExceptionArgs
        {
            internal ErrorCode Code { get; set; }

            internal string Message { get; set; }

            internal HttpResponseMessage HttpResponse { get; set; }

            internal string ResponseBody { get; set; }
        }
    }
}