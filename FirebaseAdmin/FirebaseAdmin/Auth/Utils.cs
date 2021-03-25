// Copyright 2021, Google Inc. All rights reserved.
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
using Google.Apis.Auth.OAuth2;

namespace FirebaseAdmin.Auth
{
    internal enum IdToolkitVersion
    {
        V1,
        V2,
    }

    internal class Utils
    {
        /// <summary>
        /// <para>Gets the correct identity toolkit api host.</para>
        /// <para>It does this by checking if FIREBASE_AUTH_EMULATOR_HOST exists
        /// and then prepends the url with that host if it does. Otherwise it returns the regular identity host.</para>
        /// <para>Example:<br/>
        /// If FIREBASE_AUTH_EMULATOR_HOST environment variable is set to localhost:9099 the host is resolved to http://localhost:9099/identitytoolkit.googleapis.com&#x2026;.<br/>
        /// If FIREBASE_AUTH_EMULATOR_HOST environment variable is not set the host resolves to https://identitytoolkit.googleapis.com&#x2026;.</para>
        /// </summary>
        /// <param name="projectId">The project ID to connect to.</param>
        /// <param name="version">The version of the API to connect to.</param>
        /// <param name="tenantId">The tenant id.</param>
        /// <returns>Resolved identity toolkit host.</returns>
        internal static string GetIdToolkitHost(string projectId, IdToolkitVersion version = IdToolkitVersion.V2, string tenantId = "")
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Must provide a project ID to resolve");
            }

            const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/{0}/projects/{1}{2}";
            const string IdToolkitEmulatorUrl = "http://{0}/identitytoolkit.googleapis.com/{1}/projects/{2}{3}";

            var tenantIdPath = string.Empty;
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                tenantIdPath = $"/tenants/{tenantId}";
            }

            var versionAsString = version.ToString().ToLower();

            if (IsEmulatorHostSet())
            {
                return string.Format(
                    IdToolkitEmulatorUrl,
                    GetEmulatorHost(),
                    versionAsString,
                    projectId,
                    tenantIdPath);
            }

            return string.Format(IdToolkitUrl, versionAsString, projectId, tenantIdPath);
        }

        internal static GoogleCredential ResolveCredentials(GoogleCredential original)
        {
            if (IsEmulatorHostSet())
            {
                return GoogleCredential.FromAccessToken("owner");
            }

            return original;
        }

        private static bool IsEmulatorHostSet()
            => !string.IsNullOrWhiteSpace(GetEmulatorHost());

        private static string GetEmulatorHost()
            => Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");
    }
}
