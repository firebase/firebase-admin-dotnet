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

using System.IO;
using System.Text;
using FirebaseAdmin.Cloud;
using Google.Cloud.Storage.V1;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class StorageClientHelperTest
    {
        public StorageClientHelperTest()
        {
            IntegrationTestUtils.EnsureDefaultApp();
        }

        [Fact]
        public void UseBucket()
        {
            var storageClient = StorageClientHelper.GetStorageClient();
            this.TestBucket(FirebaseApp.DefaultInstance.GetProjectId(), storageClient);
        }

        private void TestBucket(string projectId, StorageClient storageClient)
        {
            var bucketName = this.GetDefaultBucketName(projectId);

            var fileName = "FirebaseStorageTest.txt";
            var content = "FirebaseStorageTest";
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
            {
                var obj1 = storageClient.UploadObject(bucketName, fileName, "text/plain", stream);
                Assert.Equal(bucketName, obj1.Bucket);
            }

            using (var stream = new MemoryStream())
            {
                storageClient.DownloadObject(bucketName, fileName, stream);
                Assert.Equal(content, Encoding.UTF8.GetString(stream.ToArray()));
            }

            storageClient.DeleteObject(bucketName, fileName);
        }

        private string GetDefaultBucketName(string projectId)
        {
            return projectId + ".appspot.com";
        }
    }
}