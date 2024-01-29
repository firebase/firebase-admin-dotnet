using System;
using System.Net.Http;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Exception type raised by Firebase AppCheck APIs.
    /// </summary>
    public sealed class FirebaseAppCheckException : FirebaseException
    {
        internal FirebaseAppCheckException(
            ErrorCode code,
            string message,
            AppCheckErrorCode? fcmCode = null,
            Exception inner = null,
            HttpResponseMessage response = null)
        : base(code, message, inner, response)
        {
            this.AppCheckErrorCode = fcmCode;
        }

        /// <summary>
        /// Gets the Firease AppCheck error code associated with this exception. May be null.
        /// </summary>
        public AppCheckErrorCode? AppCheckErrorCode { get; private set; }
    }
}
