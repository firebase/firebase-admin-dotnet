// Copyright 2020, Google Inc. All rights reserved.
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
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    internal class EmailActionLinkRequest
    {
        internal EmailActionLinkRequest(
            string email, Type type, ActionCodeSettings settings)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty.");
            }

            if (type == Type.EmailSignIn && settings == null)
            {
                throw new ArgumentException(
                    "ActionCodeSettings must be specified when generating sign-in links.");
            }

            this.Email = email;
            this.ReturnOobLink = true;
            this.RequestType = TypeToString(type);
            if (settings != null)
            {
                this.Url = settings.Url;
                this.HandleCodeInApp = settings.HandleCodeInApp;
                this.DynamicLinkDomain = settings.DynamicLinkDomain;
                this.IosBundleId = settings.IosBundleId;
                this.AndroidPackageName = settings.AndroidPackageName;
                this.AndroidMinimumVersion = settings.AndroidMinimumVersion;
                this.AndroidInstallApp = settings.AndroidInstallApp;

                this.ValidateSettings();
            }
        }

        internal enum Type
        {
            VerifyEmail,
            EmailSignIn,
            PasswordReset,
        }

        [JsonProperty("email")]
        internal string Email { get; }

        [JsonProperty("requestType")]
        internal string RequestType { get; }

        [JsonProperty("returnOobLink")]
        internal bool ReturnOobLink { get; }

        [JsonProperty("continueUrl")]
        internal string Url { get; }

        [JsonProperty("canHandleCodeInApp")]
        internal bool? HandleCodeInApp { get; }

        [JsonProperty("dynamicLinkDomain")]
        internal string DynamicLinkDomain { get; }

        [JsonProperty("iOSBundleId")]
        internal string IosBundleId { get; }

        [JsonProperty("androidPackageName")]
        internal string AndroidPackageName { get; }

        [JsonProperty("androidMinimumVersion")]
        internal string AndroidMinimumVersion { get; }

        [JsonProperty("androidInstallApp")]
        internal bool? AndroidInstallApp { get; }

        private static string TypeToString(Type type)
        {
            if (type == Type.PasswordReset)
            {
                return "PASSWORD_RESET";
            }
            else if (type == Type.VerifyEmail)
            {
                return "VERIFY_EMAIL";
            }
            else
            {
                return "EMAIL_SIGNIN";
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrEmpty(this.Url))
            {
                throw new ArgumentException("Url must not be null or empty");
            }
            else if (!Uri.IsWellFormedUriString(this.Url, UriKind.Absolute))
            {
                throw new ArgumentException($"Malformed Url string: {this.Url}");
            }

            if (this.AndroidInstallApp == true || this.AndroidMinimumVersion != null)
            {
                if (string.IsNullOrEmpty(this.AndroidPackageName))
                {
                    throw new ArgumentException(
                        "AndroidPackageName is required when specifying other Android settings");
                }
            }

            if (this.DynamicLinkDomain == string.Empty)
            {
                throw new ArgumentException("DynamicLinkDomain must not be empty");
            }

            if (this.IosBundleId == string.Empty)
            {
                throw new ArgumentException("IosBundleId must not be empty");
            }

            if (this.AndroidPackageName == string.Empty)
            {
                throw new ArgumentException("AndroidPackageName must not be empty");
            }

            if (this.AndroidMinimumVersion == string.Empty)
            {
                throw new ArgumentException("AndroidMinimumVersion must not be empty");
            }
        }
    }
}