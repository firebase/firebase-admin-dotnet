using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Interface representing an App Check token.
    /// </summary>
    public class AppCheckService
    {
        private const long OneMinuteInMillis = 60 * 1000; // 60,000
        private const long OneDayInMillis = 24 * 60 * OneMinuteInMillis; // 1,440,000

        /// <summary>
        /// Interface representing an App Check token.
        /// </summary>
        /// <param name="options"> IDictionary string, object .</param>
        /// <returns>IDictionary string object .</returns>
        public static Dictionary<string, object> ValidateTokenOptions(AppCheckTokenOptions options)
        {
            if (options == null)
            {
                throw new FirebaseAppCheckError(
                    "invalid-argument",
                    "AppCheckTokenOptions must be a non-null object.");
            }

            if (options.TtlMillis > 0)
            {
                long ttlMillis = options.TtlMillis;
                if (ttlMillis < (OneMinuteInMillis * 30) || ttlMillis > (OneDayInMillis * 7))
                {
                    throw new FirebaseAppCheckError(
                        "invalid-argument",
                        "ttlMillis must be a duration in milliseconds between 30 minutes and 7 days (inclusive).");
                }

                return new Dictionary<string, object> { { "ttl", TransformMillisecondsToSecondsString(ttlMillis) } };
            }

            return new Dictionary<string, object>();
        }

        private static string TransformMillisecondsToSecondsString(long milliseconds)
        {
            return (milliseconds / 1000).ToString();
        }
    }
}
