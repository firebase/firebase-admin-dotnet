using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// 
    /// </summary>
    public class UserRecord
    {
        /// <summary>
        /// Uid
        /// </summary>
        public string Uid { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string Email { get; set; }
        /// <summary>
        /// EmailVerified
        /// </summary>
        public bool EmailVerified { get; set; }
        /// <summary>
        /// DisplayName
        /// </summary>
        public string DisplayName { get; set; }
        /// <summary>
        /// PhotoURL
        /// </summary>
        public string PhotoURL { get; set; }
        /// <summary>
        /// PhoneNumber
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// Disabled
        /// </summary>
        public bool Disabled { get; set; }
        //public Metadata: UserMetadata;
        //public ProviderData: UserInfo[];
        /// <summary>
        /// PasswordHash
        /// </summary>
        public string PasswordHash { get; set; }
        /// <summary>
        /// PasswordSalt
        /// </summary>
        public string PasswordSalt { get; set; }
        /// <summary>
        /// CustomClaims
        /// </summary>
        public IDictionary<string, object> CustomClaims { get; set; }

        /// <summary>
        /// TokensValidAfterTime
        /// </summary>
        public string TokensValidAfterTime { get; set; }
    }
}
