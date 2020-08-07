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
using Google.Apis.Util;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Exposes Firebase Auth operations that are available in both tenant-aware and tenant-unaware
    /// contexts.
    /// </summary>
    public abstract class AbstractFirebaseAuth : IFirebaseService
    {
        private readonly object authLock = new object();
        private bool deleted;

        internal AbstractFirebaseAuth(Args args)
        {
            args.ThrowIfNull(nameof(args));
        }

        /// <summary>
        /// Deletes this <see cref="FirebaseAuth"/> service instance.
        /// </summary>
        void IFirebaseService.Delete()
        {
            lock (this.authLock)
            {
                this.deleted = true;
                this.Cleanup();
            }
        }

        internal virtual void Cleanup() { }

        internal TResult IfNotDeleted<TResult>(Func<TResult> func)
        {
            lock (this.authLock)
            {
                if (this.deleted)
                {
                    throw new InvalidOperationException("Cannot invoke after deleting the app.");
                }

                return func();
            }
        }

        internal class Args { }
    }
}
