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

using FirebaseAdmin.Auth;

namespace FirebaseAdmin.IntegrationTests.Auth
{
    /// <summary>
    /// A fixture API that facilitates running the same set of integration tests on different
    /// implementations of <c>AbstractFirebaseAuth</c>.
    /// </summary>
    public abstract class AbstractAuthFixture<T>
    where T : AbstractFirebaseAuth
    {
        public abstract T Auth { get; }

        public abstract TemporaryUserBuilder UserBuilder { get; }

        public abstract string TenantId { get; }

        public abstract T AuthFromApp(FirebaseApp app);
    }
}
