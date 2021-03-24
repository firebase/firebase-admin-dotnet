using System;
using FirebaseAdmin.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class UtilTest : IDisposable
    {
        private const string MockProjectId = "test_project1234";
        private const string CustomHost = "localhost:9099";

        [Fact]
        public void ResolvesToCorrectVersion()
        {
            var expectedV1Host = $"https://identitytoolkit.googleapis.com/v1/projects/{MockProjectId}";
            var expectedV2Host = $"https://identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";

            Assert.Equal(expectedV1Host, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V1));
            Assert.Equal(expectedV2Host, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        [Fact]
        public void ResolvesToEmulatorHost()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", CustomHost);

            var expectedHost = $"http://{CustomHost}/identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";

            Assert.Equal(expectedHost, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        [Fact]
        public void FailsOnNoProjectId()
        {
            Assert.Throws<ArgumentException>(() => Utils.GetIdToolkitHost(string.Empty, IdToolkitVersion.V2));
        }

        [Fact]
        public void ResolvesToFirebaseHost()
        {
            var expectedHost = $"https://identitytoolkit.googleapis.com/v2/projects/{MockProjectId}";
            Assert.Equal(expectedHost, Utils.GetIdToolkitHost(MockProjectId, IdToolkitVersion.V2));
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", string.Empty);
        }
    }
}