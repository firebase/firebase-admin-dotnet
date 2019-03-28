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
using System.Threading.Tasks;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Xunit;

namespace FirebaseAdmin.Tests
{
    public class FirebaseAppTest : IDisposable
    {
        private static readonly AppOptions TestOptions = new AppOptions()
        {
            Credential = GoogleCredential.FromAccessToken("token"),
        };

        [Fact]
        public void GetNonExistingDefaultInstance()
        {
            Assert.Null(FirebaseApp.DefaultInstance);
        }

        [Fact]
        public void GetNonExistingInstance()
        {
            Assert.Null(FirebaseApp.GetInstance("non.existing"));
        }

        [Fact]
        public void DefaultInstance()
        {
            var app = FirebaseApp.Create(TestOptions);
            Assert.Equal("[DEFAULT]", app.Name);
            Assert.NotNull(app.Options);
            Assert.Same(app, FirebaseApp.DefaultInstance);
            Assert.Throws<ArgumentException>(() => FirebaseApp.Create(TestOptions));
            app.Delete();
            Assert.Null(FirebaseApp.DefaultInstance);
        }

        [Fact]
        public void CreateNamedInstance()
        {
            const string name = "MyApp";
            var app = FirebaseApp.Create(TestOptions, name);
            Assert.Equal(name, app.Name);
            Assert.NotNull(app.Options);
            Assert.Same(app, FirebaseApp.GetInstance(name));
            Assert.Throws<ArgumentException>(() => FirebaseApp.Create(TestOptions, name));
            app.Delete();
            Assert.Null(FirebaseApp.GetInstance(name));
        }

        [Fact]
        public void CreateWithInvalidName()
        {
            Assert.Throws<ArgumentException>(() => FirebaseApp.Create(name: null));
            Assert.Throws<ArgumentException>(() => FirebaseApp.Create(name: string.Empty));
        }

        [Fact]
        public void GetInstanceWithInvalidName()
        {
            Assert.Throws<ArgumentException>(() => FirebaseApp.GetInstance(null));
            Assert.Throws<ArgumentException>(() => FirebaseApp.GetInstance(string.Empty));
        }

        [Fact]
        public void NoCredential()
        {
            Assert.Throws<ArgumentNullException>(() => FirebaseApp.Create(new AppOptions()));
        }

        [Fact]
        public void CreateAppOptions()
        {
            var credential = GoogleCredential.FromAccessToken("token");
            var options = new AppOptions()
            {
                Credential = credential,
                ProjectId = "test-project",
                ServiceAccountId = "test@service.account",
            };
            var app = FirebaseApp.Create(options);
            Assert.Equal("[DEFAULT]", app.Name);

            var copy = app.Options;
            Assert.NotSame(options, copy);
            Assert.Same(credential, copy.Credential);
            Assert.Equal("test-project", copy.ProjectId);
            Assert.Equal("test@service.account", copy.ServiceAccountId);
        }

        [Fact]
        public void ServiceAccountCredentialScoping()
        {
            var credential = GoogleCredential.FromFile("./resources/service_account.json");
            var options = new AppOptions()
            {
                Credential = credential,
            };
            var app = FirebaseApp.Create(options);
            Assert.Equal("[DEFAULT]", app.Name);

            var copy = app.Options;
            Assert.NotSame(options, copy);
            Assert.NotSame(credential, copy.Credential);
            Assert.IsType<ServiceAccountCredential>(copy.Credential.UnderlyingCredential);
            var credentialScopes = (copy.Credential.UnderlyingCredential
                as ServiceAccountCredential).Scopes;
            foreach (var scope in FirebaseApp.DefaultScopes)
            {
                Assert.Contains(scope, credentialScopes);
            }
        }

        [Fact]
        public void ApplicationDefaultCredentials()
        {
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS", "./resources/service_account.json");
            try
            {
                var app = FirebaseApp.Create();
                Assert.NotNull(app.Options.Credential.ToServiceAccountCredential());
            }
            finally
            {
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", string.Empty);
            }
        }

        [Fact]
        public void GetProjectIdFromOptions()
        {
            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromAccessToken("token"),
                ProjectId = "explicit-project",
            };
            var app = FirebaseApp.Create(options);
            Assert.Equal("explicit-project", app.GetProjectId());
        }

        [Fact]
        public void GetProjectIdFromServiceAccount()
        {
            var options = new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            };
            var app = FirebaseApp.Create(options);
            Assert.Equal("test-project", app.GetProjectId());
        }

        [Fact]
        public void GetProjectIdFromEnvironment()
        {
            foreach (var name in new string[] { "GOOGLE_CLOUD_PROJECT", "GCLOUD_PROJECT" })
            {
                Environment.SetEnvironmentVariable(name, "env-project");
                try
                {
                    var app = FirebaseApp.Create(TestOptions);
                    Assert.Equal("env-project", app.GetProjectId());
                    app.Delete();
                }
                finally
                {
                    Environment.SetEnvironmentVariable(name, string.Empty);
                }
            }
        }

        [Fact]
        public void GetOrInitService()
        {
            ServiceFactory<MockService> factory = () =>
            {
                return new MockService();
            };
            var app = FirebaseApp.Create(TestOptions);
            var service1 = app.GetOrInit("MockService", factory);
            var service2 = app.GetOrInit("MockService", factory);
            Assert.Same(service1, service2);
            Assert.Throws<InvalidCastException>(() =>
            {
                app.GetOrInit("MockService", () => { return new OtherMockService(); });
            });

            Assert.False(service1.Deleted);
            app.Delete();
            Assert.True(service1.Deleted);
            Assert.Throws<InvalidOperationException>(() =>
            {
                app.GetOrInit("MockService", factory);
            });
        }

        [Fact]
        public void GetSdkVersion()
        {
            var version = FirebaseApp.GetSdkVersion();

            var segments = version.Split(".");
            Assert.Equal(3, segments.Length);
            int result;
            Assert.All(segments, (segment) => int.TryParse(segment, out result));
        }

        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }
    }

    internal class MockService : IFirebaseService
    {
        public bool Deleted { get; private set; }

        public void Delete()
        {
            this.Deleted = true;
        }
    }

    internal class OtherMockService : IFirebaseService
    {
        public void Delete() { }
    }
}
