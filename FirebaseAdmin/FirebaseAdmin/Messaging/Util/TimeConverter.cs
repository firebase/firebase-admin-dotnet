using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Messaging.Util
{
    /// <summary>
    /// Converter from long milliseconds to string and vice versa.
    /// </summary>
    internal static class TimeConverter
    {
        /// <summary>
        /// Converts long milliseconds to the FCM string representation.
        /// </summary>
        /// <param name="longMillis">Milliseconds as a long.</param>
        /// <returns>An FCM string representation of the long milliseconds.</returns>
        public static string LongMillisToString(long longMillis)
        {
            var seconds = Math.Floor(Convert.ToDouble(longMillis / 1000));
            var subsecondNanos = Convert.ToDecimal((longMillis - (seconds * 1000L)) * 1E6);

            if (subsecondNanos > 0)
            {
                return string.Format("{0:0}.{1:000000000}s", seconds, subsecondNanos);
            }
            else
            {
                return string.Format("{0}s", seconds);
            }
        }

        /// <summary>
        /// Converts an FCM representation of time into milliseconds of type long.
        /// </summary>
        /// <param name="timingString">An FCM representation of time.</param>
        /// <returns>The string time as a long.</returns>
        public static long StringToLongMillis(string timingString)
        {
            return Convert.ToInt64(Convert.ToDouble(timingString.TrimEnd('s')) * 1000);
        }
    }
}
