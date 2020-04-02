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

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Defines the required continue/state URL with optional Android and iOS settings. Used when
    /// invoking the email action link generation APIs in <see cref="FirebaseAuth"/>.
    /// </summary>
    public sealed class ActionCodeSettings
    {
        /// <summary>
        /// Gets or sets the continue/state URL. This property has different meanings in different
        /// contexts.
        /// <list type="bullet">
        /// <item>
        /// <description>
        /// When the link is handled in the web action widgets, this is the deep link in the
        /// <c>continueUrl</c> query parameter.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// When the link is handled in the app directly, this is the <c>continueUrl</c> query
        /// parameter in the deep link of the Dynamic Link.
        /// </description>
        /// </item>
        /// </list>
        /// This property is required.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to open the link via a mobile app or a browser.
        /// The default is false. When set to true, the action code link is sent as a Universal
        /// Link or an Android App Link and is opened by the app if installed. In the false case,
        /// the code is sent to the web widget first and then redirects to the app if installed.
        /// </summary>
        public bool HandleCodeInApp { get; set; }

        /// <summary>
        /// Gets or sets the dynamic link domain to use for the current link if it is to be opened
        /// using Firebase Dynamic Links, as multiple dynamic link domains can be configured per
        /// project. This setting provides the ability to explicitly choose one. If none is provided,
        /// the oldest domain is used by default.
        /// </summary>
        public string DynamicLinkDomain { get; set; }

        /// <summary>
        /// Gets or sets the bundle ID of the iOS app where the link should be handled if the
        /// application is already installed on the device.
        /// </summary>
        public string IosBundleId { get; set; }

        /// <summary>
        /// Gets or sets the Android package name of the app where the link should be handled if
        /// the Android app is installed. Must be specified when setting other Android-specific
        /// settings.
        /// </summary>
        public string AndroidPackageName { get; set; }

        /// <summary>
        /// Gets or sets the minimum version for the Android app. If the installed app is an older
        /// version, the user is taken to the Play Store to upgrade the app.
        /// </summary>
        public string AndroidMinimumVersion { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to install the Android app if the device
        /// supports it and the app is not already installed.
        /// </summary>
        public bool AndroidInstallApp { get; set; }
    }
}
