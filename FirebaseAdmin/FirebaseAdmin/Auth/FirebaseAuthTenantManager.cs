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
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// The tenant aware <see cref="FirebaseAuth"/> class. You can
    /// get an instance of this class via <c>FirebaseAuth.DefaultInstance.TenantManager.AuthForTenant(tenantId)</c>.
    /// </summary>
    public sealed class FirebaseAuthTenantManager : IFirebaseService
    {
        private readonly FirebaseApp app;

        internal FirebaseAuthTenantManager(FirebaseApp app)
        {
            this.app = app.ThrowIfNull(nameof(app));
        }

        /// <summary>
        /// Returns the auth instance for the specified app tenant.
        /// </summary>
        /// <returns>The <see cref="FirebaseTenantAwareAuth"/> instance associated with the specified
        /// app tenant.</returns>
        /// <exception cref="System.ArgumentException">If the tenantId argument is invalid.</exception>
        /// <param name="tenantId">The tenant ID to manage.</param>
        public FirebaseTenantAwareAuth AuthForTenant(string tenantId)
        {
            if (tenantId != null && !System.Text.RegularExpressions.Regex.IsMatch(tenantId, "^[a-zA-Z0-9-]+$"))
            {
                throw new ArgumentException("The tenant ID must be a valid non-empty string.", "tenantId");
            }

            return this.app.GetOrInit<FirebaseTenantAwareAuth>(nameof(FirebaseTenantAwareAuth) + tenantId, () =>
            {
                return new FirebaseTenantAwareAuth(BaseAuth.FirebaseAuthArgs.Create(this.app, tenantId));
            });
        }

        /// <summary>
        /// Deletes this service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
        }
    }
}
