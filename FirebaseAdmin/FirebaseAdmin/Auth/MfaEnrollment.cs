using Newtonsoft.Json;
using System;
using System.Text.RegularExpressions;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A specification for enrolling a user for mfa or updating their enrollment.
    /// </summary>
    public sealed class MfaEnrollment
    {
        private Optional<string> displayName;
        private Optional<string> phoneInfo;
        private Optional<object> totpInfo;

        /// <summary>
        /// Gets or sets the Mfa enrollments ID.
        /// </summary>
        public string MfaEnrollmentId { get; set; }

        /// <summary>
        /// Gets or sets the Mfa enrollments display name.
        /// </summary>
        public string DisplayName
        {
            get => this.displayName?.Value;
            set => this.displayName = this.Wrap(value);
        }

        /// <summary>
        /// Gets or sets the when the user enrolled this second factor.
        /// </summary>
        public string EnrolledAt { get; set; }

        /// <summary>
        /// Gets or sets the phone info of the mfa enrollment.
        /// </summary>
        public string PhoneInfo
        {
            get => this.phoneInfo?.Value;
            set => this.phoneInfo = this.Wrap(value);
        }

        /// <summary>
        /// Gets or sets Totp info of the mfa enrollment. NOTE: this is only here so that the totp field can be deleted, no values can really be assigned to totp via the admin sdk.
        /// </summary>
        public object TotpInfo
        {
            get => this.totpInfo?.Value;
            set => this.totpInfo = this.Wrap(value);
        }

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

        internal static string CheckPhoneNumber(string phoneNumber, bool required = false)
        {
            if (phoneNumber == null)
            {
                if (required)
                {
                    throw new ArgumentNullException(nameof(phoneNumber));
                }
            }
            else if (phoneNumber == string.Empty)
            {
                throw new ArgumentException("Phone number must not be empty.");
            }
            else if (!phoneNumber.StartsWith("+"))
            {
                throw new ArgumentException(
                    "Phone number must be a valid, E.164 compliant identifier starting with a '+' sign.");
            }

            return phoneNumber;
        }


        internal CreateUserRequest ToCreateUserRequest()
        {
            return new CreateUserRequest(this);
        }

        internal UpdateUserRequest ToUpdateUserRequest()
        {
            return new UpdateUserRequest(this);
        }

        private Optional<T> Wrap<T>(T value)
        {
            return new Optional<T>(value);
        }

        internal sealed class CreateUserRequest
        {
            internal CreateUserRequest(MfaEnrollment enrollment)
            {
                if (enrollment.PhoneInfo == null)
                {
                    throw new ArgumentException("Cannot enroll user to invalid phone number!");
                }

                this.DisplayName = enrollment.DisplayName;

                this.PhoneInfo = enrollment.PhoneInfo;
            }

            [JsonProperty("displayName")]
            public string DisplayName { get; set; }

            [JsonProperty("phoneInfo")]
            public string PhoneInfo { get; set; }
        }

        internal sealed class UpdateUserRequest
        {
            internal UpdateUserRequest(MfaEnrollment enrollment)
            {
                if (enrollment.MfaEnrollmentId != "phone")
                {
                    throw new ArgumentException("Unsupported second factor: " + enrollment.MfaEnrollmentId);
                }

                this.DisplayName = enrollment.DisplayName;
                this.EnrolledAt = enrollment.EnrolledAt;
                this.PhoneInfo = CheckPhoneNumber(enrollment.PhoneInfo, true);
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

        /// <summary>
        /// Wraps a nullable value. Used to differentiate between parameters that have not been set, and
        /// the parameters that have been explicitly set to null.
        /// </summary>
        private class Optional<T>
        {
            internal Optional(T value)
            {
                this.Value = value;
            }

            internal T Value { get; private set; }
        }
    }
}
