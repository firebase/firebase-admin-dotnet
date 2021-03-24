using System;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class AuthUtilTest : IDisposable
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
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", string.Empty);
        }
    }
}