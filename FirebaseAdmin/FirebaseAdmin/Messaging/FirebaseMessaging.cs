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
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Http;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Something.
    /// </summary>
    public sealed class FirebaseMessaging: IFirebaseService
    {
        private readonly FirebaseMessagingClient _messagingClient;

        private FirebaseMessaging(FirebaseApp app)
        {
            _messagingClient = new FirebaseMessagingClient(
                new HttpClientFactory(), app.Options.Credential, app.GetProjectId());
        }

        /// <summary>
        /// Something.
        /// </summary>
        public async Task<string> SendAsync(Message message)
        {
            return await SendAsync(message, false);
        }

        /// <summary>
        /// Something.
        /// </summary>
        public async Task<string> SendAsync(Message message, CancellationToken cancellationToken)
        {
            return await SendAsync(message, false, cancellationToken);
        }

        /// <summary>
        /// Something.
        /// </summary>
        public async Task<string> SendAsync(Message message, bool dryRun)
        {
            return await SendAsync(message, dryRun, default(CancellationToken));
        }

        /// <summary>
        /// Something.
        /// </summary>
        public async Task<string> SendAsync(Message message, bool dryRun, CancellationToken cancellationToken)
        {
            return await _messagingClient.SendAsync(message, dryRun, cancellationToken).ConfigureAwait(false);
        }

        void IFirebaseService.Delete()
        {
            _messagingClient.Dispose();   
        }

        /// <summary>
        /// Something.
        /// </summary>
        public static FirebaseMessaging DefaultInstance
        {
            get
            {
                var app = FirebaseApp.DefaultInstance;
                if (app == null)
                {
                    return null;
                }
                return GetMessaging(app);
            }
        }

        /// <summary>
        /// Something.
        /// </summary>
        public static FirebaseMessaging GetMessaging(FirebaseApp app)
        {
            if (app == null)
            {
                throw new ArgumentNullException("App argument must not be null.");
            }
            return app.GetOrInit<FirebaseMessaging>(typeof(FirebaseMessaging).Name, () => 
            {
                return new FirebaseMessaging(app);
            });
        }
    }
}