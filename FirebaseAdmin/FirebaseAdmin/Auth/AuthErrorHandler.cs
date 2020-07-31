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
using System.Net.Http;
using FirebaseAdmin.Util;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Parses error responses received from the Auth service, and creates instances of
    /// <see cref="FirebaseAuthException"/>.
    /// </summary>
    internal sealed class AuthErrorHandler
    : HttpErrorHandler<FirebaseAuthException>,
        IHttpRequestExceptionHandler<FirebaseAuthException>,
        IDeserializeExceptionHandler<FirebaseAuthException>
    {
        internal static readonly AuthErrorHandler Instance = new AuthErrorHandler();

        private static readonly IReadOnlyDictionary<string, ErrorInfo> CodeToErrorInfo =
            new Dictionary<string, ErrorInfo>()
            {
                {
                    "CONFIGURATION_NOT_FOUND",
                    new ErrorInfo(
                        ErrorCode.NotFound,
                        AuthErrorCode.ConfigurationNotFound,
                        "No identity provider configuration found for the given identifier")
                },
                {
                    "DUPLICATE_EMAIL",
                    new ErrorInfo(
                        ErrorCode.AlreadyExists,
                        AuthErrorCode.EmailAlreadyExists,
                        "The user with the provided email already exists")
                },
                {
                    "DUPLICATE_LOCAL_ID",
                    new ErrorInfo(
                        ErrorCode.AlreadyExists,
                        AuthErrorCode.UidAlreadyExists,
                        "The user with the provided uid already exists")
                },
                {
                    "EMAIL_EXISTS",
                    new ErrorInfo(
                        ErrorCode.AlreadyExists,
                        AuthErrorCode.EmailAlreadyExists,
                        "The user with the provided email already exists")
                },
                {
                    "INVALID_DYNAMIC_LINK_DOMAIN",
                    new ErrorInfo(
                        ErrorCode.InvalidArgument,
                        AuthErrorCode.InvalidDynamicLinkDomain,
                        "Dynamic link domain specified in ActionCodeSettings is not authorized")
                },
                {
                    "PHONE_NUMBER_EXISTS",
                    new ErrorInfo(
                        ErrorCode.AlreadyExists,
                        AuthErrorCode.PhoneNumberAlreadyExists,
                        "The user with the provided phone number already exists")
                },
                {
                    "TENANT_NOT_FOUND",
                    new ErrorInfo(
                        ErrorCode.NotFound,
                        AuthErrorCode.TenantNotFound,
                        "No tenant found for the given identifier")
                },
                {
                    "USER_NOT_FOUND",
                    new ErrorInfo(
                        ErrorCode.NotFound,
                        AuthErrorCode.UserNotFound,
                        "No user record found for the given identifier")
                },
            };

        private AuthErrorHandler() { }

        public FirebaseAuthException HandleHttpRequestException(
            HttpRequestException exception)
        {
            var temp = exception.ToFirebaseException();
            return new FirebaseAuthException(
                temp.ErrorCode,
                temp.Message,
                inner: temp.InnerException,
                response: temp.HttpResponse);
        }

        public FirebaseAuthException HandleDeserializeException(
            Exception exception, ResponseInfo responseInfo)
        {
            return new FirebaseAuthException(
                ErrorCode.Unknown,
                $"Error while parsing Auth service response. {exception.Message}: {responseInfo.Body}",
                AuthErrorCode.UnexpectedResponse,
                inner: exception,
                response: responseInfo.HttpResponse);
        }

        protected sealed override FirebaseExceptionArgs CreateExceptionArgs(
            HttpResponseMessage response, string body)
        {
            var authError = this.ParseAuthError(body);

            ErrorInfo info;
            CodeToErrorInfo.TryGetValue(authError.Code, out info);

            var defaults = base.CreateExceptionArgs(response, body);
            return new FirebaseAuthExceptionArgs()
            {
                Code = info?.ErrorCode ?? defaults.Code,
                Message = info?.GetMessage(authError) ?? defaults.Message,
                HttpResponse = response,
                ResponseBody = body,
                AuthErrorCode = info?.AuthErrorCode,
            };
        }

        protected override FirebaseAuthException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseAuthException(
                args.Code,
                args.Message,
                (args as FirebaseAuthExceptionArgs).AuthErrorCode,
                response: args.HttpResponse);
        }

        private AuthError ParseAuthError(string body)
        {
            try
            {
                var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<AuthErrorResponse>(body);
                return parsed.Error ?? new AuthError();
            }
            catch
            {
                // Ignore any error that may occur while parsing the error response. The server
                // may have responded with a non-json body.
                return new AuthError();
            }
        }

        /// <summary>
        /// Describes a class of errors that can be raised by the Firebase Auth backend API.
        /// </summary>
        private sealed class ErrorInfo
        {
            private readonly string message;

            internal ErrorInfo(ErrorCode code, AuthErrorCode authCode, string message)
            {
                this.ErrorCode = code;
                this.AuthErrorCode = authCode;
                this.message = message;
            }

            internal ErrorCode ErrorCode { get; private set; }

            internal AuthErrorCode AuthErrorCode { get; private set; }

            internal string GetMessage(AuthError authError)
            {
                var message = $"{this.message} ({authError.Code})";
                if (!string.IsNullOrEmpty(authError.Detail))
                {
                    return $"{message}: {authError.Detail}";
                }

                return $"{message}.";
            }
        }

        private sealed class FirebaseAuthExceptionArgs : FirebaseExceptionArgs
        {
            internal AuthErrorCode? AuthErrorCode { get; set; }
        }

        private sealed class AuthError
        {
            [JsonProperty("message")]
            internal string Message { get; set; }

            /// <summary>
            /// Gets the Firebase Auth error code extracted from the response. Returns empty string
            /// if the error code cannot be determined.
            /// </summary>
            internal string Code
            {
                get
                {
                    var separator = this.GetSeparator();
                    if (separator != -1)
                    {
                        return this.Message.Substring(0, separator);
                    }

                    return this.Message ?? string.Empty;
                }
            }

            /// <summary>
            /// Gets the error detail sent by the Firebase Auth API. May be null.
            /// </summary>
            internal string Detail
            {
                get
                {
                    var separator = this.GetSeparator();
                    if (separator != -1)
                    {
                        return this.Message.Substring(separator + 1).Trim();
                    }

                    return null;
                }
            }

            private int GetSeparator()
            {
                return this.Message?.IndexOf(':') ?? -1;
            }
        }

        private sealed class AuthErrorResponse
        {
            [JsonProperty("error")]
            internal AuthError Error { get; set; }
        }
    }
}
