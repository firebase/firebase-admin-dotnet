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

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// Simplifies getting and setting  environment variables used in the project.
    /// This class also serves as a dictionary, of sorts, for environment variables relevant to the project.
    /// </summary>
    internal static class EnvironmentVariable
    {
        /// <summary>
        /// Gets or sets environment variable for connecting to the Firebase Authentication Emulator.
        /// </summary>
        /// <value>string in the form &lt;host&gt;:&lt;port&gt;, e.g. localhost:9099. Beware: No validation is done.</value>
        internal static string FirebaseAuthEmulatorHost
        {
            get => Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST") ?? string.Empty;
            set => Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", value);
        }
    }
}
