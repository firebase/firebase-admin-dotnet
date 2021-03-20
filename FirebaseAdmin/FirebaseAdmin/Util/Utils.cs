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
    internal enum IdToolkitVersion
    {
        V1,
        V2,
    }

    internal class Utils
    {
        /// <summary>
        /// Resolves to the correct identity toolkit api host.
        /// It does this by checking if <see cref="EnvironmentVariable.FirebaseAuthEmulatorHost" /> exists
        /// and then prepends the url with that host if it does. Otherwise it returns the regular identity host.
        /// Example:
        /// If <see cref="EnvironmentVariable.FirebaseAuthEmulatorHost" /> is set to localhost:9099 the host is resolved to http://localhost:9099/identitytoolkit.googleapis.com...
        /// If <see cref="EnvironmentVariable.FirebaseAuthEmulatorHost" /> is not set the host resolves to https://identitytoolkit.googleapis.com...
        /// </summary>
        /// <param name="projectId">The project ID to connect to.</param>
        /// <param name="version">The version of the API to connect to.</param>
        /// <returns>Resolved identity toolkit host.</returns>
        internal static string ResolveIdToolkitHost(string projectId, IdToolkitVersion version = IdToolkitVersion.V2)
        {
            const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/{0}/projects/{1}";
            const string IdToolkitEmulatorUrl = "http://{0}/identitytoolkit.googleapis.com/{1}/projects/{2}";

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Must provide a project ID to resolve");
            }

            var emulatorHostEnvVar = EnvironmentVariable.FirebaseAuthEmulatorHost;
            var useEmulatorHost = !string.IsNullOrWhiteSpace(emulatorHostEnvVar);
            var versionAsString = version.ToString().ToLower();
            return useEmulatorHost
                ? string.Format(IdToolkitEmulatorUrl, emulatorHostEnvVar, versionAsString, projectId)
                : string.Format(IdToolkitUrl, versionAsString, projectId);
        }
    }
}
