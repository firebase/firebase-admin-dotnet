using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Jwt.Tests;
using FirebaseAdmin.Check;
using Google.Apis.Auth.OAuth2;
using Moq;
using Xunit;

namespace FirebaseAdmin.Tests.AppCheck
{
    public class AppCheckTokenVerifierTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private static readonly GoogleCredential MockCredential =
            GoogleCredential.FromAccessToken("test-token");

        [Fact]
        public void ProjectIdFromOptions()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = MockCredential,
                ProjectId = "explicit-project-id",
            });
            var verifier = AppCheckTokenVerify.Create(app);
            Assert.Equal("explicit-project-id", verifier.ProjectId);
        }

        [Fact]
        public void ProjectIdFromServiceAccount()
        {
            var app = FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromFile("./resources/service_account.json"),
            });
            var verifier = AppCheckTokenVerify.Create(app);
            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidProjectId(string projectId)
        {
            var args = FullyPopulatedArgs();
            args.ProjectId = projectId;

            Assert.Throws<ArgumentException>(() => new AppCheckTokenVerify(args));
        }

        [Fact]
        public void NullKeySource()
        {
            var args = FullyPopulatedArgs();
            args.PublicKeySource = null;

            Assert.Throws<ArgumentNullException>(() => new AppCheckTokenVerify(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidShortName(string shortName)
        {
            var args = FullyPopulatedArgs();
            args.ShortName = shortName;

            Assert.Throws<ArgumentException>(() => new AppCheckTokenVerify(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidIssuer(string issuer)
        {
            var args = FullyPopulatedArgs();
            args.Issuer = issuer;

            Assert.Throws<ArgumentException>(() => new AppCheckTokenVerify(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidOperation(string operation)
        {
            var args = FullyPopulatedArgs();
            args.Operation = operation;

            Assert.Throws<ArgumentException>(() => new AppCheckTokenVerify(args));
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public void InvalidUrl(string url)
        {
            var args = FullyPopulatedArgs();
            args.Url = url;

            Assert.Throws<ArgumentException>(() => new AppCheckTokenVerify(args));
        }

        [Fact]
        public void ProjectId()
        {
            var args = FullyPopulatedArgs();

            var verifier = new AppCheckTokenVerify(args);

            Assert.Equal("test-project", verifier.ProjectId);
        }

        [Fact]
        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        private static FirebaseTokenVerifierArgs FullyPopulatedArgs()
        {
            return new FirebaseTokenVerifierArgs
            {
                ProjectId = "test-project",
                ShortName = "short name",
                Operation = "VerifyToken()",
                Url = "https://firebase.google.com",
                Issuer = "https://firebase.google.com/",
                PublicKeySource = JwtTestUtils.DefaultKeySource,
            };
        }
    }
}
