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
using Google.Cloud.Storage.V1;

namespace FirebaseAdmin.Cloud
{
    /// <summary>
    /// StorageClientHelper provides access to Google Cloud Storage APIs. You
    /// can get an instance of this class via <c>StorageClientHelper.GetStorageClient()</c>.
    /// </summary>
    public sealed class StorageClientHelper : IFirebaseService
    {
        private StorageClient storageClient;

        private StorageClientHelper(FirebaseApp app)
        {
            this.storageClient = StorageClient.Create(app.Options.Credential);
        }

        /// <summary>
        /// Gets the StorageClient instance associated with the default Firebase app. Return value is
        /// <c>null</c> if the default app doesn't yet exist.
        /// </summary>
        /// <returns>The <see cref="StorageClient"/> instance associated with the specified
        /// app.</returns>
        public static StorageClient GetStorageClient()
        {
            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                return null;
            }

            return GetStorageClient(app);
        }

        /// <summary>
        /// Returns the StorageClient instance for the specified app.
        /// </summary>
        /// <returns>The <see cref="StorageClient"/> instance associated with the specified
        /// app.</returns>
        /// <exception cref="System.ArgumentNullException">If the app argument is null.</exception>
        /// <param name="app">An app instance.</param>
        public static StorageClient GetStorageClient(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }

            return app.GetOrInit<StorageClientHelper>(typeof(StorageClientHelper).Name, () =>
            {
                return new StorageClientHelper(app);
            }).storageClient;
        }

        /// <summary>
        /// Deletes this <see cref="StorageClientHelper"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            this.storageClient.Dispose();
        }
    }
}
