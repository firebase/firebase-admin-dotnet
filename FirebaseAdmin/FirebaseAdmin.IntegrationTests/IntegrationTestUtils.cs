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
using Google.Apis.Auth.OAuth2;

namespace FirebaseAdmin.IntegrationTests
{
    internal static class IntegrationTestUtils
    {
        private const string ServiceAccountFile = "./resources/integration_cert.json";
        private const string ApiKeyFile = "./resources/integration_apikey.txt";

        private static readonly Lazy<FirebaseApp> DefaultFirebaseApp = new Lazy<FirebaseApp>(
            () =>
            {
                var options = new AppOptions()
                {
                    Credential = GoogleCredential.FromFile(ServiceAccountFile),
                };
                return FirebaseApp.Create(options);
            }, true);

        public static FirebaseApp EnsureDefaultApp()
        {
            return DefaultFirebaseApp.Value;
        }

        public static string GetApiKey()
        {
            return System.IO.File.ReadAllText(ApiKeyFile).Trim();
        }
    }
}
