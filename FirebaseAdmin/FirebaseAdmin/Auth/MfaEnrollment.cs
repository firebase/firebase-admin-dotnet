using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A specification for enrolling a user for mfa or updating their enrollment.
    /// </summary>
    public sealed class MfaEnrollment
    {
        /// <summary>
        /// Gets or sets the Mfa enrollments ID.
        /// </summary>
        public string MfaEnrollmentId { get; set; }

        /// <summary>
        /// Gets or sets the Mfa enrollments display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the when the user enrolled this second factor.
        /// </summary>
        public string EnrolledAt { get; set; }

        /// <summary>
        /// Gets or sets the phone info of the mfa enrollment.
        /// </summary>
        public string PhoneInfo { get; set; }

        /// <summary>
        /// Gets or sets the email info of the mfa enrollment.
        /// </summary>
        public EmailInfo EmailInfo { get; set; }

        /// <summary>
        /// Gets or sets unobfuscated phone info of the mfa enrollment.
        /// </summary>
        public string UnobfuscatedPhoneInfo { get; set; }

        internal static string CheckEmail(string email, bool required = false)
        {
            if (email == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(email));
                }
            }
            else if (email == string.Empty)
            {
                throw new ArgumentException("Email must not be empty");
            }
            else if (!Regex.IsMatch(email, @"^[^@]+@[^@]+$"))
            {
                throw new ArgumentException($"Invalid email address: {email}");
            }

            return email;
        }

        internal UpdateUserRequest ToUpdateUserRequest()
        {
            return new UpdateUserRequest(this);
        }

        internal sealed class UpdateUserRequest
        {
            internal UpdateUserRequest(MfaEnrollment enrollment)
            {
                if (enrollment.PhoneInfo != null && enrollment.EmailInfo != null)
                {
                    throw new ArgumentException("Cannot have two differing enrollment types in the same enrollment!");
                }

                if (enrollment.PhoneInfo == null && enrollment.EmailInfo == null)
                {
                    throw new ArgumentException("Must have atleast one enrolled authentication method!");
                }

                if (enrollment.EmailInfo != null)
                {
                    this.EmailInfo = new EmailInfo { EmailAddress = CheckEmail(enrollment.EmailInfo.EmailAddress) };
                }

                this.DisplayName = enrollment.DisplayName;
                this.EnrolledAt = enrollment.EnrolledAt;
                this.PhoneInfo = enrollment.PhoneInfo;
                this.UnobfuscatedPhoneInfo = enrollment.UnobfuscatedPhoneInfo;
            }

            [JsonProperty("mfaEnrollmentId")]
            public string MfaEnrollmentId { get; set; }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("enrolledAt")]
            public string EnrolledAt { get; set; }

            [JsonProperty("phoneInfo")]
            public string PhoneInfo { get; set; }

            [JsonProperty("emailInfo")]
            public EmailInfo EmailInfo { get; set; }

            [JsonProperty("unobfuscatedPhoneInfo")]
            public string UnobfuscatedPhoneInfo { get; set; }
        }
    }
}
