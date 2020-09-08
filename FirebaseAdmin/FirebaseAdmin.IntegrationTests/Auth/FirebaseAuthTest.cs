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
using FirebaseAdmin.Auth;
using Xunit;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class FirebaseAuthTest : AbstractFirebaseAuthTest<FirebaseAuth>, IClassFixture<FirebaseAuthTest.Fixture>
    {
        public FirebaseAuthTest(Fixture fixture)
        : base(fixture) { }

        [Fact]
        public async Task SessionCookie()
        {
            var customToken = await this.Auth.CreateCustomTokenAsync("testuser");
            var idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(customToken);
            var options = new SessionCookieOptions()
            {
                ExpiresIn = TimeSpan.FromHours(1),
            };

            var sessionCookie = await this.Auth.CreateSessionCookieAsync(idToken, options);

            var decoded = await this.Auth.VerifySessionCookieAsync(sessionCookie);
            Assert.Equal("testuser", decoded.Uid);

            await Task.Delay(1000);
            await this.Auth.RevokeRefreshTokensAsync("testuser");
            decoded = await this.Auth.VerifySessionCookieAsync(sessionCookie);
            Assert.Equal("testuser", decoded.Uid);

            var exception = await Assert.ThrowsAsync<FirebaseAuthException>(
                async () => await this.Auth.VerifySessionCookieAsync(
                    sessionCookie, true));
            Assert.Equal(ErrorCode.InvalidArgument, exception.ErrorCode);
            Assert.Equal(AuthErrorCode.RevokedSessionCookie, exception.AuthErrorCode);

            idToken = await AuthIntegrationUtils.SignInWithCustomTokenAsync(customToken);
            sessionCookie = await this.Auth.CreateSessionCookieAsync(idToken, options);
            decoded = await this.Auth.VerifySessionCookieAsync(sessionCookie, true);
            Assert.Equal("testuser", decoded.Uid);
        }

        public class Fixture : AbstractAuthFixture<FirebaseAuth>, IDisposable
        {
            public Fixture()
            {
                IntegrationTestUtils.EnsureDefaultApp();
                this.UserBuilder = new TemporaryUserBuilder(FirebaseAuth.DefaultInstance);
            }

            public override FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

            public override TemporaryUserBuilder UserBuilder { get; }

            public override string TenantId => null;

            public override FirebaseAuth AuthFromApp(FirebaseApp app)
            {
                return FirebaseAuth.GetAuth(app);
            }

            public void Dispose()
            {
                this.UserBuilder.Dispose();
            }
        }
    }
}
