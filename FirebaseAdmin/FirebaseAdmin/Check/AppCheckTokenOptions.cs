using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Interface representing App Check token options.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="AppCheckTokenOptions"/> class.
    /// </remarks>
    /// <param name="v">ttlMillis.</param>
    public class AppCheckTokenOptions(int v)
    {
        /// <summary>
        /// Gets or sets the length of time, in milliseconds, for which the App Check token will
        /// be valid. This value must be between 30 minutes and 7 days, inclusive.
        /// </summary>
        public int TtlMillis { get; set; } = v;
    }
}
