using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// A specification for updating mfa email information.
    /// </summary>
    public sealed class EmailInfo
    {
        /// <summary>
        /// Gets or sets email address of the Email Info.
        /// </summary>
        public string EmailAddress { get; set; }
    }
}
