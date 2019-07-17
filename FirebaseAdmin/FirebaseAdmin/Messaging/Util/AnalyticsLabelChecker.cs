using System;
using System.Text.RegularExpressions;

namespace FirebaseAdmin.Messaging.Util
{
    /// <summary>
    /// Checker for analytics label.
    /// </summary>
    public static class AnalyticsLabelChecker
    {
        private static string pattern = "^[a-zA-Z0-9-_.~%]{0,50}$";

        /// <summary>
        /// Checks anytytics labels an throw if not valid.
        /// </summary>
        /// <exception cref="ArgumentException">If analytics label does not match pattern.</exception>
        /// <param name="analyticsLabel">Analytics label.</param>
        public static void CheckAnalyticsLabelOrThrow(string analyticsLabel)
        {
            if (!Regex.IsMatch(analyticsLabel, pattern))
            {
                throw new ArgumentException("Analytics label must have format matching'^[a-zA-Z0-9-_.~%]{1,50}$");
            }
        }
    }
}