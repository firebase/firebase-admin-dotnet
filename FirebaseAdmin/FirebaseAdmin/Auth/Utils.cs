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
    internal class Utils
    {
        private const string IdToolkitUrl = "identitytoolkit.googleapis.com/{0}/projects/{1}{2}";

        internal static string EmulatorHostFromEnvironment =>
            Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");

        internal static bool IsEmulatorModeFromEnvironment =>
            !string.IsNullOrWhiteSpace(EmulatorHostFromEnvironment);

        internal static string BuildAuthUrl(string projectId, UrlOptions options = null)
        {
            var version = !string.IsNullOrEmpty(options?.ApiVersion) ? options.ApiVersion : "v1";
            var tenant = !string.IsNullOrEmpty(options?.TenantId) ?
                $"/tenants/{options.TenantId}" : string.Empty;
            var url = string.Format(IdToolkitUrl, version, projectId, tenant);
            if (!string.IsNullOrWhiteSpace(options?.EmulatorHost))
            {
                return $"http://{options.EmulatorHost}/{url}";
            }

            return $"https://{url}";
        }

        internal static GoogleCredential ResolveCredentials(
            string emulatorHost, GoogleCredential original)
        {
            if (!string.IsNullOrWhiteSpace(emulatorHost))
            {
                return GoogleCredential.FromAccessToken("owner");
            }

            return original;
        }

        internal class UrlOptions
        {
            internal string TenantId { get; set; }

            internal string ApiVersion { get; set; }

            internal string EmulatorHost { get; set; }
        }
    }
}
