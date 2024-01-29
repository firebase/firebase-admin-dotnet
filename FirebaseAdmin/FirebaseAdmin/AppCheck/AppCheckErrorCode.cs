namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Error codes that can be raised by the Firebase App Check APIs.
    /// </summary>
    public enum AppCheckErrorCode
    {
        /// <summary>
        /// Process is aborted
        /// </summary>
        Aborted,

        /// <summary>
        /// Argument is not valid
        /// </summary>
        InvalidArgument,

        /// <summary>
        /// Credential is not valid
        /// </summary>
        InvalidCredential,

        /// <summary>
        /// The server internal error
        /// </summary>
        InternalError,

        /// <summary>
        /// Permission is denied
        /// </summary>
        PermissionDenied,

        /// <summary>
        /// Unauthenticated
        /// </summary>
        Unauthenticated,

        /// <summary>
        /// Resource is not found
        /// </summary>
        NotFound,

        /// <summary>
        /// App Check Token is expired
        /// </summary>
        AppCheckTokenExpired,

        /// <summary>
        /// Unknown Error
        /// </summary>
        UnknownError,
    }
}
