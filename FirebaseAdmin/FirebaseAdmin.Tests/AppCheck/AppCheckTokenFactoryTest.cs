using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FirebaseAdmin.AppCheck;
using FirebaseAdmin.Auth.Jwt;
using FirebaseAdmin.Auth.Jwt.Tests;
using FirebaseAdmin.Tests;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;
using Xunit;

#pragma warning disable SYSLIB0027

namespace FirebaseAdmin.AppCheck.Tests
{
    public class AppCheckTokenFactoryTest
    {
        public static readonly IEnumerable<object[]> InvalidStrings = new List<object[]>
        {
            new object[] { null },
            new object[] { string.Empty },
        };

        private const int ThirtyMinInMs = 1800000;
        private const int SevenDaysInMs = 604800000;
        private static readonly MockClock Clock = new MockClock();
        private static readonly MockSigner Signer = new MockSigner();
        private readonly string appId = "test-app-id";

        public string Private { get; private set; }

        public string Public { get; private set; }

        [Fact]
        public async Task CreateCustomToken()
        {
            var factory = CreateTokenFactory();

            var token = await factory.CreateCustomTokenAsync("user1");

            MockCustomTokenVerifier.WithTenant().Verify(token);
        }

        [Theory]
        [MemberData(nameof(InvalidStrings))]
        public async Task InvalidAppId(string appId)
        {
            var factory = CreateTokenFactory();
            await Assert.ThrowsAsync<FirebaseAppCheckException>(() => factory.CreateCustomTokenAsync(appId)).ConfigureAwait(false);
        }

        [Fact]
        public async Task RejectedOption()
        {
            int[] ttls = new int[] { -100, -1, 0, 10, 1799999, 604800001, 1209600000 };
            foreach (var ttl in ttls)
            {
                var option = new AppCheckTokenOptions(ttl);

                var factory = CreateTokenFactory();

                await Assert.ThrowsAsync<FirebaseAppCheckException>(() =>
                    factory.CreateCustomTokenAsync("user1", option)).ConfigureAwait(false);
            }
        }

        [Fact]
        public async Task FullFilledOption()
        {
            int[] ttls = new int[] { -100, -1, 0, 10, 1799999, 604800001, 1209600000 };
            foreach (var ttl in ttls)
            {
                var option = new AppCheckTokenOptions(ttl);

                var factory = CreateTokenFactory();

                await Assert.ThrowsAsync<FirebaseAppCheckException>(() =>
                    factory.CreateCustomTokenAsync("user1", option)).ConfigureAwait(false);
            }
        }

        [Fact]
        public void Dispose()
        {
            FirebaseApp.DeleteAll();
        }

        [Fact]
        public async Task DecodedPayload()
        {
            List<(int Milliseconds, string ExpectedResult)> ttls = new List<(int, string)>
            {
                (ThirtyMinInMs, "1800s"),
                (ThirtyMinInMs + 1, "1800.001000000s"),
                (SevenDaysInMs / 2, "302400s"),
                (SevenDaysInMs - 1, "604799.999000000s"),
                (SevenDaysInMs, "604800s"),
            };

            foreach (var value in ttls)
            {
                var factory = CreateTokenFactory();

                var option = new AppCheckTokenOptions(value.Milliseconds);

                string token = await factory.CreateCustomTokenAsync(this.appId, option).ConfigureAwait(false);
                string[] segments = token.Split(".");
                Assert.Equal(3, segments.Length);
                var payload = JwtUtils.Decode<AppCheckTokenFactory.CustomTokenPayload>(segments[1]);
                Assert.Equal(value.ExpectedResult, payload.Ttl);
            }
        }

        [Fact]
        public async Task CorrectHeader()
        {
            var factory = CreateTokenFactory();

            var option = new AppCheckTokenOptions(ThirtyMinInMs + 3000);

            string token = await factory.CreateCustomTokenAsync(this.appId, option).ConfigureAwait(false);
            string[] segments = token.Split(".");
            Assert.Equal(3, segments.Length);
            var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
            Assert.Equal("RS256", header.Algorithm);
            Assert.Equal("JWT", header.Type);
        }

        private static AppCheckTokenFactory CreateTokenFactory()
        {
            var args = new AppCheckTokenFactory.Args
            {
                Signer = Signer,
                Clock = Clock,
            };
            return new AppCheckTokenFactory(args);
        }

        private abstract class AppCheckCustomTokenVerifier
        {
            private readonly string issuer;

            internal AppCheckCustomTokenVerifier(string issuer)
            {
                this.issuer = issuer;
            }

            internal static AppCheckCustomTokenVerifier ForServiceAccount(
                string clientEmail, byte[] publicKey)
            {
                return new RSACustomTokenVerifier(clientEmail, publicKey);
            }

            internal void Verify(string token, IDictionary<string, object> claims = null)
            {
                string[] segments = token.Split(".");
                Assert.Equal(3, segments.Length);

                var header = JwtUtils.Decode<JsonWebSignature.Header>(segments[0]);
                this.AssertHeader(header);

                var payload = JwtUtils.Decode<AppCheckTokenFactory.CustomTokenPayload>(segments[1]);
                Assert.Equal(this.issuer, payload.Issuer);
                Assert.Equal(this.issuer, payload.Subject);
                Assert.Equal(AppCheckTokenFactory.FirebaseAppCheckAudience, payload.Audience);
                this.AssertSignature($"{segments[0]}.{segments[1]}", segments[2]);
            }

            protected virtual void AssertHeader(JsonWebSignature.Header header)
            {
                Assert.Equal("RS256", header.Algorithm);
                Assert.Equal("JWT", header.Type);
            }

            protected abstract void AssertSignature(string tokenData, string signature);

            private sealed class RSACustomTokenVerifier : AppCheckCustomTokenVerifier
            {
                private readonly RSA rsa;

                internal RSACustomTokenVerifier(string issuer, byte[] publicKey)
                : base(issuer)
                {
                    var x509cert = new X509Certificate2(publicKey);
                    this.rsa = (RSA)x509cert.PublicKey.Key;
                }

                protected override void AssertSignature(string tokenData, string signature)
                {
                    var tokenDataBytes = Encoding.UTF8.GetBytes(tokenData);
                    var signatureBytes = JwtUtils.Base64DecodeToBytes(signature);
                    var verified = this.rsa.VerifyData(
                        tokenDataBytes,
                        signatureBytes,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1);
                    Assert.True(verified);
                }
            }

            private sealed class EmulatorCustomTokenVerifier : AppCheckCustomTokenVerifier
            {
                internal EmulatorCustomTokenVerifier(string tenantId)
                : base("firebase-auth-emulator@example.com") { }

                protected override void AssertHeader(JsonWebSignature.Header header)
                {
                    Assert.Equal("none", header.Algorithm);
                    Assert.Equal("JWT", header.Type);
                }

                protected override void AssertSignature(string tokenData, string signature)
                {
                    Assert.Empty(signature);
                }
            }
        }

        private sealed class MockCustomTokenVerifier : AppCheckCustomTokenVerifier
        {
            private readonly string expectedSignature;

            private MockCustomTokenVerifier(string issuer, string signature)
            : base(issuer)
            {
                this.expectedSignature = signature;
            }

            internal static MockCustomTokenVerifier WithTenant()
            {
                return new MockCustomTokenVerifier(
                    MockSigner.KeyIdString, MockSigner.Signature);
            }

            protected override void AssertSignature(string tokenData, string signature)
            {
                Assert.Equal(this.expectedSignature, JwtUtils.Base64Decode(signature));
            }
        }

        private sealed class MockSigner : ISigner
        {
            public const string KeyIdString = "mock-key-id";
            public const string Signature = "signature";

            public string Algorithm => JwtUtils.AlgorithmRS256;

            public Task<string> GetKeyIdAsync(CancellationToken cancellationToken)
            {
                return Task.FromResult(KeyIdString);
            }

            public Task<byte[]> SignDataAsync(byte[] data, CancellationToken cancellationToken)
            {
                return Task.FromResult(Encoding.UTF8.GetBytes(Signature));
            }

            public void Dispose() { }
        }
    }
}
