using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Enumerator to encapsulate the different possible MfaFactorIds.
    /// </summary>
    public enum MfaFactorIdType
    {
        /// <summary>
        /// Value for the phone factor id.
        /// </summary>
        [StringValue("phone")]
        Phone = 1,

        /// <summary>
        /// Value for the phone factor id.
        /// </summary>
        [StringValue("totp")]
        Totp = 2,
    }
}
