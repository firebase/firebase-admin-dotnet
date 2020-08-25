// Copyright 2020, Google Inc. All rights reserved.
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
using FirebaseAdmin.Auth;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    public class FirebaseAuthFixture : AbstractAuthFixture<FirebaseAuth>, IDisposable
    {
        private readonly TemporaryUserBuilder userBuilder;

        public FirebaseAuthFixture()
        {
            IntegrationTestUtils.EnsureDefaultApp();
            this.userBuilder = new TemporaryUserBuilder();
        }

        public override FirebaseAuth Auth => FirebaseAuth.DefaultInstance;

        public override TemporaryUserBuilder UserBuilder => this.userBuilder;

        public override string TenantId => null;

        public override FirebaseAuth AuthFromApp(FirebaseApp app)
        {
            return FirebaseAuth.GetAuth(app);
        }

        public void Dispose()
        {
            this.userBuilder.Dispose();
        }
    }
}
