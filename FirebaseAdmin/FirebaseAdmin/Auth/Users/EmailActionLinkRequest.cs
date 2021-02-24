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

namespace FirebaseAdmin.Auth.Users
{
    internal sealed class EmailActionLinkRequest
    {
        private const string VerifyEmail = "VERIFY_EMAIL";
        private const string PasswordReset = "PASSWORD_RESET";
        private const string EmailSignIn = "EMAIL_SIGNIN";

        private EmailActionLinkRequest(string type, string email, ActionCodeSettings settings)
        {
            if (string.IsNullOrEmpty(email))
            {
                throw new ArgumentException("Email cannot be null or empty.");
            }

            if (type == EmailSignIn && settings == null)
            {
                throw new ArgumentNullException(
                    "ActionCodeSettings must not be null when generating sign in links");
            }

            this.RequestType = type;
            this.Email = email;
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

        [JsonProperty("email")]
        internal string Email { get; }

        [JsonProperty("requestType")]
        internal string RequestType { get; }

        [JsonProperty("returnOobLink")]
        internal bool ReturnOobLink { get => true; }

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

        internal static EmailActionLinkRequest EmailVerificationLinkRequest(
            string email, ActionCodeSettings settings)
        {
            return new EmailActionLinkRequest(VerifyEmail, email, settings);
        }

        internal static EmailActionLinkRequest PasswordResetLinkRequest(
            string email, ActionCodeSettings settings)
        {
            return new EmailActionLinkRequest(PasswordReset, email, settings);
        }

        internal static EmailActionLinkRequest EmailSignInLinkRequest(
            string email, ActionCodeSettings settings)
        {
            return new EmailActionLinkRequest(EmailSignIn, email, settings);
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
