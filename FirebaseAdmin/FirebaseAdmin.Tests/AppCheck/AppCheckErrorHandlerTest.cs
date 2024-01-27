using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using FirebaseAdmin.Util;
using Xunit;

namespace FirebaseAdmin.AppCheck.Tests
{
    public class AppCheckErrorHandlerTest
    {
        public static readonly IEnumerable<object[]> AppCheckErrorCodes =
            new List<object[]>()
            {
                new object[]
                {
                    "ABORTED",
                    ErrorCode.Aborted,
                    AppCheckErrorCode.Aborted,
                },
                new object[]
                {
                    "INVALID_ARGUMENT",
                    ErrorCode.InvalidArgument,
                    AppCheckErrorCode.InvalidArgument,
                },
                new object[]
                {
                    "INVALID_CREDENTIAL",
                    ErrorCode.InvalidArgument,
                    AppCheckErrorCode.InvalidCredential,
                },
                new object[]
                {
                    "PERMISSION_DENIED",
                    ErrorCode.PermissionDenied,
                    AppCheckErrorCode.PermissionDenied,
                },
                new object[]
                {
                    "UNAUTHENTICATED",
                    ErrorCode.Unauthenticated,
                    AppCheckErrorCode.Unauthenticated,
                },
                new object[]
                {
                    "NOT_FOUND",
                    ErrorCode.NotFound,
                    AppCheckErrorCode.NotFound,
                },
                new object[]
                {
                    "UNKNOWN",
                    ErrorCode.Unknown,
                    AppCheckErrorCode.UnknownError,
                },
            };

        [Theory]
        [MemberData(nameof(AppCheckErrorCodes))]
        public void KnownErrorCode(
            string code, ErrorCode expectedCode, AppCheckErrorCode expectedAppCheckCode)
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""{code}"",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(expectedCode, error.ErrorCode);
            Assert.Equal(expectedAppCheckCode, error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
            Assert.EndsWith($" ({code}).", error.Message);
        }

        [Theory]
        [MemberData(nameof(AppCheckErrorCodes))]
        public void KnownErrorCodeWithDetails(
            string code, ErrorCode expectedCode, AppCheckErrorCode expectedAppCheckCode)
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""{code}: Some details."",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(expectedCode, error.ErrorCode);
            Assert.Equal(expectedAppCheckCode, error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
            Assert.EndsWith($" ({code}).: Some details.", error.Message);
        }

        [Fact]
        public void UnknownErrorCode()
        {
            var json = $@"{{
                ""error"": {{
                    ""message"": ""SOMETHING_UNUSUAL"",
                }}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Internal, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{json}",
                error.Message);
            Assert.Null(error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void UnspecifiedErrorCode()
        {
            var json = $@"{{
                ""error"": {{}}
            }}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Internal, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 500 (InternalServerError){Environment.NewLine}{json}",
                error.Message);
            Assert.Null(error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void NoDetails()
        {
            var json = @"{}";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, json);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{{}}",
                error.Message);
            Assert.Null(error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void NonJson()
        {
            var text = "plain text";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };

            var error = AppCheckErrorHandler.Instance.HandleHttpErrorResponse(resp, text);

            Assert.Equal(ErrorCode.Unavailable, error.ErrorCode);
            Assert.Equal(
                $"Unexpected HTTP response with status: 503 (ServiceUnavailable){Environment.NewLine}{text}",
                error.Message);
            Assert.Null(error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Null(error.InnerException);
        }

        [Fact]
        public void DeserializeException()
        {
            var text = "plain text";
            var resp = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.ServiceUnavailable,
                Content = new StringContent(text, Encoding.UTF8, "text/plain"),
            };
            var inner = new Exception("Deserialization error");

            var error = AppCheckErrorHandler.Instance.HandleDeserializeException(
                inner, new ResponseInfo(resp, text));

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                $"Error while parsing AppCheck service response. Deserialization error: {text}",
                error.Message);
            Assert.Equal(AppCheckErrorCode.UnknownError, error.AppCheckErrorCode);
            Assert.Same(resp, error.HttpResponse);
            Assert.Same(inner, error.InnerException);
        }

        [Fact]
        public void HttpRequestException()
        {
            var exception = new HttpRequestException("network error");

            var error = AppCheckErrorHandler.Instance.HandleHttpRequestException(exception);

            Assert.Equal(ErrorCode.Unknown, error.ErrorCode);
            Assert.Equal(
                "Unknown error while making a remote service call: network error", error.Message);
            Assert.Null(error.AppCheckErrorCode);
            Assert.Null(error.HttpResponse);
            Assert.Same(exception, error.InnerException);
        }
    }
}
