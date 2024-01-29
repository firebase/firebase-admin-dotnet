using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FirebaseAdmin.Util
{
    internal class StringUtils
    {
        public static string ReplacePlaceholders(string str, Dictionary<string, string> urlParams)
        {
            string formatted = str;
            foreach (var key in urlParams.Keys)
            {
                formatted = Regex.Replace(formatted, $"{{{key}}}", urlParams[key]);
            }

            return formatted;
        }
    }
}
