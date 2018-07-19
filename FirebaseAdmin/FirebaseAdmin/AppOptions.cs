// Copyright 2018, Google Inc. All rights reserved.
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
using System.Collections.Generic;
using Google.Apis.Auth.OAuth2;

namespace FirebaseAdmin
{
    /// <summary>
    /// Configurable options that can be specified when creating a <see cref="FirebaseApp"/>.
    /// See <a href="https://firebase.google.com/docs/admin/setup#initialize_the_sdk">
    /// Initialize the SDK</a> for code samples and detailed documentation.
    /// </summary>
    public sealed class AppOptions
    {
        /// <summary>
        /// <see cref="GoogleCredential"/> used to authorize an app. All service calls made by
        /// the app will be authorized using this.
        /// </summary>
        public GoogleCredential Credential { get; set; }

        /// <summary>
        /// The Google Cloud Platform project ID that should be associated with an app.
        /// </summary>
        public string ProjectId { get; set; }

        /// <summary>
        /// The unique ID of the service account that should be associated with an app.
        /// <para>This is used to <a href="https://firebase.google.com/docs/auth/admin/create-custom-tokens">
        /// create custom auth tokens</a> when service account credentials are not available. The
        /// service account ID can be found in the <c>client_email</c> field of the service account
        /// JSON.</para>
        /// </summary>
        public string ServiceAccountId { get; set; }

        /// <summary>
        /// Creates a new <see cref="AppOptions"/> instance.
        /// </summary>
        public AppOptions() {}

        internal AppOptions(AppOptions options)
        {
            Credential = options.Credential;
            ProjectId = options.ProjectId;
            ServiceAccountId = options.ServiceAccountId;
        }
    }
}
