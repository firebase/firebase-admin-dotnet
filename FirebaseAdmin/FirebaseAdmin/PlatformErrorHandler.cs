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
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin
{
    internal class PlatformErrorHandler
    {
        private static readonly IReadOnlyDictionary<string, ErrorCode> PlatformErrorCodes =
            new Dictionary<string, ErrorCode>()
            {
                { "INVALID_ARGUMENT", ErrorCode.InvalidArgument },
                { "INTERNAL", ErrorCode.Internal },
                { "PERMISSION_DENIED", ErrorCode.PermissionDenied },
                { "UNAUTHENTICATED", ErrorCode.Unauthenticated },
                { "UNAVAILABLE", ErrorCode.Unavailable },
            };

        private static readonly IReadOnlyDictionary<HttpStatusCode, ErrorCode> HttpErrorCodes =
            new Dictionary<HttpStatusCode, ErrorCode>()
            {
                { HttpStatusCode.BadRequest, ErrorCode.InvalidArgument },
                { HttpStatusCode.InternalServerError, ErrorCode.Internal },
                { HttpStatusCode.Forbidden, ErrorCode.PermissionDenied },
                { HttpStatusCode.Unauthorized, ErrorCode.Unauthenticated },
                { HttpStatusCode.ServiceUnavailable, ErrorCode.Unavailable },
            };

        internal void ThrowIfError(HttpResponseMessage response, string json)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var parsedResponse = NewtonsoftJsonSerializer.Instance.Deserialize<PlatformErrorResponse>(json);
            var status = parsedResponse.Error?.Status ?? string.Empty;

            ErrorCode code;
            if (!PlatformErrorCodes.TryGetValue(status, out code))
            {
                if (!HttpErrorCodes.TryGetValue(response.StatusCode, out code))
                {
                    code = ErrorCode.Unknown;
                }
            }

            var message = parsedResponse.Error?.Message;
            if (string.IsNullOrEmpty(message))
            {
                message = "Unexpected HTTP response with status: "
                    + $"{(int)response.StatusCode} ({response.StatusCode})"
                    + $"{Environment.NewLine}{json}";
            }

            var args = new FirebaseExceptionArgs()
            {
                Code = code,
                Message = message,
                HttpResponse = response,
                ResponseBody = json,
            };
            throw this.CreateException(args);
        }

        protected virtual FirebaseException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseException(args.Code, args.Message, response: args.HttpResponse);
        }

        internal class PlatformErrorResponse
        {
            [JsonProperty("error")]
            public PlatformError Error { get; set; }
        }

        internal class PlatformError
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        internal class FirebaseExceptionArgs
        {
            internal ErrorCode Code { get; set; }

            internal string Message { get; set; }

            internal HttpResponseMessage HttpResponse { get; set; }

            internal string ResponseBody { get; set; }
        }
    }
}