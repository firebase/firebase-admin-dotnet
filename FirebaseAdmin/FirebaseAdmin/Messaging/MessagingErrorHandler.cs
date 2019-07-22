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
using System.Linq;
using System.Net.Http;
using FirebaseAdmin.Util;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Parses error responses received from the FCM service, and creates instances of
    /// <see cref="FirebaseMessagingException"/>.
    /// </summary>
    internal sealed class MessagingErrorHandler
    : PlatformErrorHandler<FirebaseMessagingException>,
        IHttpRequestExceptionHandler<FirebaseMessagingException>,
        IDeserializeExceptionHandler<FirebaseMessagingException>
    {
        internal static readonly MessagingErrorHandler Instance = new MessagingErrorHandler();

        private static readonly string MessagingErrorType =
            "type.googleapis.com/google.firebase.fcm.v1.FcmError";

        private static readonly IReadOnlyDictionary<string, MessagingErrorCode> MessagingErrorCodes =
            new Dictionary<string, MessagingErrorCode>()
            {
                { "APNS_AUTH_ERROR", MessagingErrorCode.ThirdPartyAuthError },
                { "INTERNAL", MessagingErrorCode.Internal },
                { "INVALID_ARGUMENT", MessagingErrorCode.InvalidArgument },
                { "QUOTA_EXCEEDED", MessagingErrorCode.QuotaExceeded },
                { "SENDER_ID_MISMATCH", MessagingErrorCode.SenderIdMismatch },
                { "THIRD_PARTY_AUTH_ERROR", MessagingErrorCode.ThirdPartyAuthError },
                { "UNAVAILABLE", MessagingErrorCode.Unavailable },
                { "UNREGISTERED", MessagingErrorCode.Unregistered },
            };

        private MessagingErrorHandler() { }

        public FirebaseMessagingException HandleHttpRequestException(
            HttpRequestException exception)
        {
            var temp = exception.ToFirebaseException();
            return new FirebaseMessagingException(
                temp.ErrorCode,
                temp.Message,
                inner: temp.InnerException,
                response: temp.HttpResponse);
        }

        public FirebaseMessagingException HandleDeserializeException(
            Exception exception, ResponseInfo responseInfo)
        {
            return new FirebaseMessagingException(
                ErrorCode.Unknown,
                $"Error parsing response from FCM. {exception.Message}: {responseInfo.Body}",
                inner: exception,
                response: responseInfo.HttpResponse);
        }

        protected override FirebaseMessagingException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseMessagingException(
                args.Code,
                args.Message,
                this.GetMessagingErrorCode(args.ResponseBody),
                response: args.HttpResponse);
        }

        private MessagingErrorCode? GetMessagingErrorCode(string body)
        {
            try
            {
                var fcmError = NewtonsoftJsonSerializer.Instance.Deserialize<MessagingErrorResponse>(
                    body);
                return fcmError.Error?.GetMessagingErrorCode();
            }
            catch
            {
                return null;
            }
        }

        private sealed class MessagingErrorResponse
        {
            [JsonProperty("error")]
            public MessagingError Error { get; set; }
        }

        private sealed class MessagingError : PlatformError
        {
            [JsonProperty("details")]
            internal IList<MessagingErrorDetail> Details { get; set; }

            internal MessagingErrorCode? GetMessagingErrorCode()
            {
                var filtered = this.Details?
                    .Where(detail => detail.Type == MessagingErrorType)
                    .FirstOrDefault();
                var code = filtered?.ErrorCode ?? string.Empty;
                if (MessagingErrorCodes.ContainsKey(code))
                {
                    return MessagingErrorCodes[code];
                }

                return null;
            }
        }

        private sealed class MessagingErrorDetail
        {
            [JsonProperty("@type")]
            internal string Type { get; set; }

            [JsonProperty("errorCode")]
            internal string ErrorCode { get; set; }
        }
    }
}
