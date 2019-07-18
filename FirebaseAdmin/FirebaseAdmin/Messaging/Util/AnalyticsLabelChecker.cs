using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace FirebaseAdmin.Messaging.Util
{
    /// <summary>
    /// Checker for analytics label.
    /// </summary>
    public static class AnalyticsLabelChecker
    {
        private static ImmutableHashSet<char> alowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-~%".ToCharArray().ToImmutableHashSet();

        /// <summary>
        /// Checks anytytics labels an throw if not valid.
        /// </summary>
        /// <exception cref="ArgumentException">If analytics label does not match pattern.</exception>
        /// <param name="analyticsLabel">Analytics label.</param>
        public static void CheckAnalyticsLabelOrThrow(string analyticsLabel)
        {
            if (analyticsLabel.Length > 50)
            {
                throw new ArgumentException("Analytics label must have format matching'^[a-zA-Z0-9-_.~%]{1,50}$");
            }

            foreach (var character in analyticsLabel)
            {
                if (!alowedChars.Contains(character))
                {
                    throw new ArgumentException("Analytics label must have format matching'^[a-zA-Z0-9-_.~%]{1,50}$");
                }
            }
        }
    }
}