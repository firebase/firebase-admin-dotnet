using System;
using System.Collections.Generic;
using System.Net.Http;
using FirebaseAdmin.Util;
using Google.Apis.Json;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Parses error responses received from the Auth service, and creates instances of
    /// <see cref="FirebaseAppCheckException"/>.
    /// </summary>
    internal sealed class AppCheckErrorHandler
    : HttpErrorHandler<FirebaseAppCheckException>,
        IHttpRequestExceptionHandler<FirebaseAppCheckException>,
        IDeserializeExceptionHandler<FirebaseAppCheckException>
    {
        internal static readonly AppCheckErrorHandler Instance = new AppCheckErrorHandler();

        private static readonly IReadOnlyDictionary<string, ErrorInfo> CodeToErrorInfo =
            new Dictionary<string, ErrorInfo>()
            {
                {
                    "ABORTED",
                    new ErrorInfo(
                        ErrorCode.Aborted,
                        AppCheckErrorCode.Aborted,
                        "App check is aborted")
                },
                {
                    "INVALID_ARGUMENT",
                    new ErrorInfo(
                        ErrorCode.InvalidArgument,
                        AppCheckErrorCode.InvalidArgument,
                        "An argument is not valid")
                },
                {
                    "INVALID_CREDENTIAL",
                    new ErrorInfo(
                        ErrorCode.InvalidArgument,
                        AppCheckErrorCode.InvalidCredential,
                        "The credential is not valid")
                },
                {
                    "PERMISSION_DENIED",
                    new ErrorInfo(
                        ErrorCode.PermissionDenied,
                        AppCheckErrorCode.PermissionDenied,
                        "The permission is denied")
                },
                {
                    "UNAUTHENTICATED",
                    new ErrorInfo(
                        ErrorCode.Unauthenticated,
                        AppCheckErrorCode.Unauthenticated,
                        "Unauthenticated")
                },
                {
                    "NOT_FOUND",
                    new ErrorInfo(
                        ErrorCode.NotFound,
                        AppCheckErrorCode.NotFound,
                        "The resource is not found")
                },
                {
                    "UNKNOWN",
                    new ErrorInfo(
                        ErrorCode.Unknown,
                        AppCheckErrorCode.UnknownError,
                        "unknown-error")
                },
            };

        private AppCheckErrorHandler() { }

        public FirebaseAppCheckException HandleHttpRequestException(
            HttpRequestException exception)
        {
            var temp = exception.ToFirebaseException();
            return new FirebaseAppCheckException(
                temp.ErrorCode,
                temp.Message,
                inner: temp.InnerException,
                response: temp.HttpResponse);
        }

        public FirebaseAppCheckException HandleDeserializeException(
            Exception exception, ResponseInfo responseInfo)
        {
            return new FirebaseAppCheckException(
                ErrorCode.Unknown,
                $"Error while parsing AppCheck service response. Deserialization error: {responseInfo.Body}",
                AppCheckErrorCode.UnknownError,
                inner: exception,
                response: responseInfo.HttpResponse);
        }

        protected sealed override FirebaseExceptionArgs CreateExceptionArgs(
            HttpResponseMessage response, string body)
        {
            var appCheckError = this.ParseAppCheckError(body);

            ErrorInfo info;
            CodeToErrorInfo.TryGetValue(appCheckError.Code, out info);

            var defaults = base.CreateExceptionArgs(response, body);
            return new FirebaseAppCheckExceptionArgs()
            {
                Code = info?.ErrorCode ?? defaults.Code,
                Message = info?.GetMessage(appCheckError) ?? defaults.Message,
                HttpResponse = response,
                ResponseBody = body,
                AppCheckErrorCode = info?.AppCheckErrorCode,
            };
        }

        protected override FirebaseAppCheckException CreateException(FirebaseExceptionArgs args)
        {
            return new FirebaseAppCheckException(
                args.Code,
                args.Message,
                (args as FirebaseAppCheckExceptionArgs).AppCheckErrorCode,
                response: args.HttpResponse);
        }

        private AppCheckError ParseAppCheckError(string body)
        {
            try
            {
                var parsed = NewtonsoftJsonSerializer.Instance.Deserialize<AppCheckErrorResponse>(body);
                return parsed.Error ?? new AppCheckError();
            }
            catch
            {
                // Ignore any error that may occur while parsing the error response. The server
                // may have responded with a non-json body.
                return new AppCheckError();
            }
        }

        /// <summary>
        /// Describes a class of errors that can be raised by the Firebase Auth backend API.
        /// </summary>
        private sealed class ErrorInfo
        {
            private readonly string message;

            internal ErrorInfo(ErrorCode code, AppCheckErrorCode appCheckErrorCode, string message)
            {
                this.ErrorCode = code;
                this.AppCheckErrorCode = appCheckErrorCode;
                this.message = message;
            }

            internal ErrorCode ErrorCode { get; private set; }

            internal AppCheckErrorCode AppCheckErrorCode { get; private set; }

            internal string GetMessage(AppCheckError appCheckError)
            {
                var message = $"{this.message} ({appCheckError.Code}).";
                if (!string.IsNullOrEmpty(appCheckError.Detail))
                {
                    return $"{message}: {appCheckError.Detail}";
                }

                return $"{message}";
            }
        }

        private sealed class FirebaseAppCheckExceptionArgs : FirebaseExceptionArgs
        {
            internal AppCheckErrorCode? AppCheckErrorCode { get; set; }
        }

        private sealed class AppCheckError
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

        private sealed class AppCheckErrorResponse
        {
            [JsonProperty("error")]
            internal AppCheckError Error { get; set; }
        }
    }
}
