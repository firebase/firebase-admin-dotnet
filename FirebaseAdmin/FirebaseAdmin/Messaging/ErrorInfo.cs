using System.Collections.Generic;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// A topic management error.
    /// </summary>
    public sealed class ErrorInfo
    {
        // Server error codes as defined in https://developers.google.com/instance-id/reference/server
        // TODO: Should we handle other error codes here (e.g. PERMISSION_DENIED)?
        private static IReadOnlyDictionary<string, string> errorCodes;
        private readonly string unknownError = "unknown-error";

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorInfo"/> class.
        /// </summary>
        /// <param name="index">Index of the error in the error codes.</param>
        /// <param name="reason">Reason for the error.</param>
        public ErrorInfo(int index, string reason)
        {
            errorCodes = new Dictionary<string, string>
                {
                    { "INVALID_ARGUMENT", "invalid-argument" },
                    { "NOT_FOUND", "registration-token-not-registered" },
                    { "INTERNAL", "internal-error" },
                    { "TOO_MANY_TOPICS", "too-many-topics" },
                };

            this.Index = index;
            this.Reason = errorCodes.ContainsKey(reason)
              ? errorCodes[reason] : this.unknownError;
        }

        /// <summary>
        /// Gets the registration token to which this error is related to.
        /// </summary>
        /// <returns>An index into the original registration token list.</returns>
        public int Index { get; private set; }

        /// <summary>
        /// Gets the nature of the error.
        /// </summary>
        /// <returns>A non-null, non-empty error message.</returns>
        public string Reason { get; private set; }
    }
}
