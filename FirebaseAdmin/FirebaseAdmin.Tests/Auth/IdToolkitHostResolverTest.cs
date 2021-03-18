using System;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class IdToolkitHostResolverTest : IDisposable
    {
        private string mockProjectId = "test_project1234";
        private string customHost = "localhost:9099";

        [Fact]
        public void ResolvesToCorrectVersion()
        {
            var expectedV1Host = $"https://identitytoolkit.googleapis.com/v1/projects/{this.mockProjectId}";
            var expectedV2Host = $"https://identitytoolkit.googleapis.com/v2/projects/{this.mockProjectId}";

            var v1Resolver = new IdToolkitHostResolver(this.mockProjectId, IdToolkitVersion.V1);
            var v2Resolver = new IdToolkitHostResolver(this.mockProjectId, IdToolkitVersion.V2);

            Assert.Equal(expectedV1Host, v1Resolver.Resolve());
            Assert.Equal(expectedV2Host, v2Resolver.Resolve());
        }

        [Fact]
        public void ResolvesToEmulatorHost()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", this.customHost);

            var expectedHost = $"http://{this.customHost}/identitytoolkit.googleapis.com/v2/projects/{this.mockProjectId}";
            var resolver = new IdToolkitHostResolver(this.mockProjectId, IdToolkitVersion.V2);

            var resolvedHost = resolver.Resolve();

            Assert.Equal(expectedHost, resolvedHost);
        }

        [Fact]
        public void FailsOnNoProjectId()
        {
            Assert.Throws<ArgumentException>(() => new IdToolkitHostResolver(string.Empty, IdToolkitVersion.V2));
        }

        [Fact]
        public void ResolvesToFirebaseHost()
        {
            var expectedHost = $"https://identitytoolkit.googleapis.com/v2/projects/{this.mockProjectId}";
            var resolver = new IdToolkitHostResolver(this.mockProjectId, IdToolkitVersion.V2);
            var resolvedHost = resolver.Resolve();
            Assert.Equal(expectedHost, resolvedHost);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", string.Empty);
        }
    }
}