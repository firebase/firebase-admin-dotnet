// Copyright 2019, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Text.RegularExpressions;

namespace FirebaseAdmin.Messaging.Util
{
    /// <summary>
    /// Checker for analytics label.
    /// </summary>
    internal static class AnalyticsLabelChecker
    {
        private static string pattern = "^[a-zA-Z0-9-_.~%]{0,50}$";

        /// <summary>
        /// Checks anytytics labels an throw if not valid.
        /// </summary>
        /// <exception cref="ArgumentException">If analytics label does not match pattern.</exception>
        /// <param name="analyticsLabel">Analytics label.</param>
        public static void ValidateAnalyticsLabel(string analyticsLabel)
        {
            if (analyticsLabel == null)
            {
                return;
            }

            if (analyticsLabel == string.Empty)
            {
                throw new ArgumentException("Analytics label must have format matching'^[a-zA-Z0-9-_.~%]{1,50}$");
            }

            if (!Regex.IsMatch(analyticsLabel, pattern))
            {
                throw new ArgumentException("Analytics label must have format matching'^[a-zA-Z0-9-_.~%]{1,50}$");
            }
        }
    }
}
