using System;
using FirebaseAdmin.Auth.Users;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Class to contain readonly user mfa information.
    /// </summary>
    public class MfaEnrollment
    {
        internal MfaEnrollment(GetAccountInfoResponse.MfaEnrollment enrollment)
        {
            if (enrollment == null)
            {
                throw new ArgumentNullException("enrollment cannot be null!");
            }

            if (enrollment.MfaEnrollmentId == string.Empty)
            {
                throw new ArgumentException("Enrollment cannot be an empty string!");
            }

            this.MfaEnrollmentId = enrollment.MfaEnrollmentId;
            this.DisplayName = enrollment.DisplayName;
            this.EnrolledAt = enrollment.EnrolledAt;
            this.PhoneInfo = enrollment.PhoneInfo;
            this.UnobfuscatedPhoneInfo = enrollment.UnobfuscatedPhoneInfo;

            if (enrollment.PhoneInfo != null && enrollment.TotpInfo != null)
            {
                throw new ArgumentException("Cannot have multiple conflicting info fields!");
            }

            if (enrollment.PhoneInfo != null)
            {
                this.MfaFactorId = MfaFactorIdType.Phone;
            }
            else if (enrollment.TotpInfo != null)
            {
                this.MfaFactorId = MfaFactorIdType.Totp;
            }
            else
            {
                throw new ArgumentException("Must have atleast one valid multifactor factor type!");
            }
        }

        /// <summary>
        /// Gets the Mfa enrollments factor id.
        /// </summary>
        public MfaFactorIdType MfaFactorId { get; }

        /// <summary>
        /// Gets the Mfa enrollments ID.
        /// </summary>
        public string MfaEnrollmentId { get; }

        /// <summary>
        /// Gets the Mfa enrollments display name.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the when the user enrolled this second factor.
        /// </summary>
        public DateTime EnrolledAt { get; }

        /// <summary>
        /// Gets the phone info of the mfa enrollment.
        /// </summary>
        public string PhoneInfo { get; }

        /// <summary>
        /// Gets unobfuscated phone info of the mfa enrollment.
        /// </summary>
        public string UnobfuscatedPhoneInfo { get; }
    }
}
