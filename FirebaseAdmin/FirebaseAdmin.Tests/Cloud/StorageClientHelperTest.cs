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
using Google.Cloud.Storage.V1;
using Xunit;

namespace FirebaseAdmin.Cloud.Tests
{
    public class StorageClientHelperTest : IDisposable
    {
        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void GetStorageClientWithoutApp()
        {
            Assert.Null(StorageClientHelper.GetStorageClient());
        }

        [Fact]
        public void GetDefaultStorageClient()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            StorageClient storageClient = StorageClientHelper.GetStorageClient();
            Assert.Same(storageClient, StorageClientHelper.GetStorageClient());
            app.Delete();
            Assert.Null(StorageClientHelper.GetStorageClient());
        }

        [Fact]
        public void GetStorageClient()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            StorageClient storageClient = StorageClientHelper.GetStorageClient(app);
            Assert.Same(storageClient, StorageClientHelper.GetStorageClient(app));
            app.Delete();
            Assert.Throws<InvalidOperationException>(() => StorageClientHelper.GetStorageClient(app));
        }

        [Fact]
        public void UseAfterDelete()
        {
            var app = FirebaseApp.Create(new AppOptions() { Credential = MockCredential });
            StorageClient storageClient = StorageClientHelper.GetStorageClient(app);
            app.Delete();
            Assert.Throws<ObjectDisposedException>(
                () => storageClient.GetBucket("test"));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }
}