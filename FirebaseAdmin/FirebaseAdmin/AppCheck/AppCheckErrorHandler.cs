// Copyright 2026, Google Inc. All rights reserved.
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
using FirebaseAdmin.Util;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Parses error responses received from the App Check service (the JWKS and token
    /// verification endpoints), and creates instances of <see cref="FirebaseAppCheckException"/>.
    /// </summary>
    internal sealed class AppCheckErrorHandler
    : PlatformErrorHandler<FirebaseAppCheckException>,
        IHttpRequestExceptionHandler<FirebaseAppCheckException>,
        IDeserializeExceptionHandler<FirebaseAppCheckException>
    {
        internal static readonly AppCheckErrorHandler Instance = new AppCheckErrorHandler();

        private AppCheckErrorHandler() { }

        public FirebaseAppCheckException HandleHttpRequestException(
            HttpRequestException exception)
        {
            var temp = exception.ToFirebaseException();
            return new FirebaseAppCheckException(
                temp.ErrorCode,
                temp.Message,
                AppCheckErrorCode.ServiceError,
                inner: temp.InnerException,
                response: temp.HttpResponse);
        }

        public FirebaseAppCheckException HandleDeserializeException(
            Exception exception, ResponseInfo responseInfo)
        {
            return new FirebaseAppCheckException(
                ErrorCode.Unknown,
                $"Error parsing response from App Check service. {exception.Message}: {responseInfo.Body}",
                AppCheckErrorCode.ServiceError,
                inner: exception,
                response: responseInfo.HttpResponse);
        }

        protected override FirebaseAppCheckException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseAppCheckException(
                args.Code,
                args.Message,
                AppCheckErrorCode.ServiceError,
                response: args.HttpResponse);
        }
    }
}
