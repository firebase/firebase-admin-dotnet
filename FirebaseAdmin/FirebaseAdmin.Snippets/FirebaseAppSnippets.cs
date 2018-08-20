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
// [START using_namespace_decl]
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
// [END using_namespace_decl]

namespace FirebaseAdmin.Snippets
{
    class FirebaseAppSnippets
    {
        static void InitSdkWithServiceAccount()
        {
            // [START initialize_sdk_with_service_account]
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("path/to/serviceAccountKey.json"),
            });
            // [END initialize_sdk_with_service_account]
        }

        static void InitSdkWithApplicationDefault()
        {
            // [START initialize_sdk_with_application_default]
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            });
            // [END initialize_sdk_with_application_default]
        }

        static void InitSdkWithRefreshToken()
        {
            // [START initialize_sdk_with_refresh_token]
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("path/to/refreshToken.json"),
            });
            // [END initialize_sdk_with_refresh_token]
        }

        static void InitSdkWithDefaultConfig()
        {
            // [START initialize_sdk_with_default_config]
            FirebaseApp.Create();
            // [END initialize_sdk_with_default_config]
        }

        static void InitDefaultApp()
        {
            // [START access_services_default]
            // Initialize the default app
            var defaultApp = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            });
            Console.WriteLine(defaultApp.Name); // "[DEFAULT]"

            // Retrieve services by passing the defaultApp variable...
            var defaultAuth = FirebaseAuth.GetAuth(defaultApp);

            // ... or use the equivalent shorthand notation
            defaultAuth = FirebaseAuth.DefaultInstance;
            // [END access_services_default]
        }

        static void InitCustomApp()
        {
            var defaultOptions = new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            };
            var otherAppConfig = new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            };

            // [START access_services_nondefault]
            // Initialize the default app
            var defaultApp = FirebaseApp.Create(defaultOptions);

            // Initialize another app with a different config
            var otherApp = FirebaseApp.Create(otherAppConfig, "other");

            Console.WriteLine(defaultApp.Name); // "[DEFAULT]"
            Console.WriteLine(otherApp.Name); // "other"

            // Use the shorthand notation to retrieve the default app's services
            var defaultAuth = FirebaseAuth.DefaultInstance;

            // Use the otherApp variable to retrieve the other app's services
            var otherAuth = FirebaseAuth.GetAuth(otherApp);
            // [END access_services_nondefault]
        }

        static void InitWithServiceAccountId()
        {
            // [START initialize_sdk_with_service_account_id]
            FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ServiceAccountId = "my-client-id@my-project-id.iam.gserviceaccount.com",
            });
            // [END initialize_sdk_with_service_account_id]
        }
    }
}
