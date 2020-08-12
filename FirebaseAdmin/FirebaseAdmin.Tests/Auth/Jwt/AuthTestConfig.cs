using System;
using System.Linq;
using System.Net.Http;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Multitenancy;
using FirebaseAdmin.Tests;
using FirebaseAdmin.Util;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    public abstract class AuthTestConfig
    {
        private readonly Lazy<ConfigContext> config;

        public AuthTestConfig()
        {
            this.config = new Lazy<ConfigContext>(this.InitConfig);
        }

        private protected ConfigContext Config => this.config.Value;

        private protected abstract string BaseUrl { get; }

        public AbstractFirebaseAuth CreateAuth(HttpMessageHandler handler = null)
        {
            var context = new TestContext
            {
                UserManagerHandler = handler,
            };
            return this.CreateAuth(context);
        }

        public abstract AbstractFirebaseAuth CreateAuth(TestContext context = null);

        internal void AssertRequest(
            string expectedPath, MockMessageHandler.IncomingRequest request)
        {
            Assert.Equal($"{this.BaseUrl}/{expectedPath}", request.Url.PathAndQuery);
            Assert.Equal(
                FirebaseUserManager.ClientVersion,
                request.Headers.GetValues(FirebaseUserManager.ClientVersionHeader).First());
        }

        private protected abstract ConfigContext InitConfig();

        private protected void PopulateArgs(
            AbstractFirebaseAuth.Args args, TestContext context)
        {
            if (context == null)
            {
                context = new TestContext();
            }

            if (context.UserManagerHandler != null)
            {
                args.UserManager = new Lazy<FirebaseUserManager>(
                    this.CreateUserManager(context.UserManagerHandler));
            }

            if (context.IdTokenVerifier)
            {
                args.IdTokenVerifier = new Lazy<FirebaseTokenVerifier>(
                    this.CreateIdTokenVerifier());
            }
        }

        private FirebaseUserManager CreateUserManager(HttpMessageHandler handler)
        {
            var args = new FirebaseUserManager.Args
            {
                Credential = this.Config.Credential,
                Clock = this.Config.Clock,
                RetryOptions = this.Config.RetryOptions,
                ProjectId = this.Config.ProjectId,
                TenantId = this.Config.TenantId,
                ClientFactory = new MockHttpClientFactory(handler),
            };
            return new FirebaseUserManager(args);
        }

        private FirebaseTokenVerifier CreateIdTokenVerifier()
        {
            var args = FirebaseTokenVerifierArgs.ForIdTokens(
                this.Config.ProjectId,
                this.Config.KeySource,
                this.Config.Clock,
                this.Config.TenantId);
            return new FirebaseTokenVerifier(args);
        }

        public class ConfigContext
        {
            internal GoogleCredential Credential { get; set; }

            internal IClock Clock { get; set; }

            internal RetryOptions RetryOptions { get; set; }

            internal string ProjectId { get; set; }

            internal string TenantId { get; set; }

            internal IPublicKeySource KeySource { get; set; }
        }

        public class TestContext
        {
            internal HttpMessageHandler UserManagerHandler { get; set; }

            internal bool IdTokenVerifier { get; set; }
        }

        internal abstract class MyFirebaseAuthTestConfig : AuthTestConfig
        {
            private protected override string BaseUrl => $"/v1/projects/{this.Config.ProjectId}";

            public override AbstractFirebaseAuth CreateAuth(TestContext context)
            {
                var args = FirebaseAuth.Args.CreateDefault();
                this.PopulateArgs(args, context);
                return new FirebaseAuth(args);
            }
        }

        internal abstract class MyTenantAwareFirebaseAuthTestConfig : AuthTestConfig
        {
            private protected override string BaseUrl =>
                $"/v1/projects/{this.Config.ProjectId}/tenants/{this.Config.TenantId}";

            public override AbstractFirebaseAuth CreateAuth(TestContext context)
            {
                var args = TenantAwareFirebaseAuth.Args.CreateDefault(this.Config.TenantId);
                this.PopulateArgs(args, context);
                return new TenantAwareFirebaseAuth(args);
            }
        }
    }
}
