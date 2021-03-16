using System;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public class IdToolkitHostResolverTest : IDisposable
    {
        private string mockProjectId = "test_project1234";
        private string customHost = "localhost:9099";

        [Fact]
        public void ResolvesToEmulatorHost()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", this.customHost);

            var expectedHost = $"http://{this.customHost}/identitytoolkit.googleapis.com/v2/projects/{this.mockProjectId}";
            var resolver = new IdToolkitHostResolver(this.mockProjectId);

            var resolvedHost = resolver.Resolve();

            Assert.Equal(expectedHost, resolvedHost);
        }

        [Fact]
        public void FailsOnEmptyEmulatorHost()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", string.Empty);

            var resolver = new IdToolkitHostResolver(this.mockProjectId);

            Assert.Throws<ArgumentException>(() => resolver.Resolve());
        }

        [Fact]
        public void FailsOnNoProjectId()
        {
            Assert.Throws<ArgumentException>(() => new IdToolkitHostResolver(string.Empty));
        }

        [Fact]
        public void ResolvesToFirebaseHost()
        {
            var expectedHost = $"http://identitytoolkit.googleapis.com/v2/projects/{this.mockProjectId}";
            var resolver = new IdToolkitHostResolver(this.mockProjectId);
            var resolvedHost = resolver.Resolve();
            Assert.Equal(expectedHost, resolvedHost);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST", string.Empty);
        }
    }
}