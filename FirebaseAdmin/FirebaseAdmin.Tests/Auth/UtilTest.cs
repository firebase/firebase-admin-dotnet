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
using Xunit;
using static FirebaseAdmin.Auth.Utils;

namespace FirebaseAdmin.Auth.Tests
{
    public class UtilTest : IDisposable
    {
        private const string MockProjectId = "test_project1234";
        private const string CustomHost = "localhost:9099";

        [Fact]
        public void ResolvesToCorrectVersion()
        {
            var expectedV1Url = $"https://identitytoolkit.googleapis.com/v1/projects/{MockProjectId}";
            var expectedV2Url = $"https://identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";

            Assert.Equal(expectedV1Url, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V1));
            Assert.Equal(expectedV2Url, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        [Fact]
        public void ResolvesToEmulatorHost()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", CustomHost);

            var expectedUrl = $"http://{CustomHost}/identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";

            Assert.Equal(expectedUrl, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        [Fact]
        public void FailsOnNoProjectId()
        {
            Assert.Throws<ArgumentException>(() => Utils.GetIdToolkitHost(string.Empty, IdToolkitVersion.V2));
        }

        [Fact]
        public void ResolvesToFirebaseHost()
        {
            var expectedUrl = $"https://identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";
            Assert.Equal(expectedUrl, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(EnvironmentVariable.FirebaseAuthEmulatorHostName, string.Empty);
        }
    }
}
