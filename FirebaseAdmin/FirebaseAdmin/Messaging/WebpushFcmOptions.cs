// Copyright 2019, Google Inc. All rights reserved.
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
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents the Webpush-specific notification options that can be included in a <see cref="Message"/>.
    /// See <see href="https://firebase.google.com/docs/reference/fcm/rest/v1/projects.messages#WebpushFcmOptions">this link</see>.
    /// </summary>
    public sealed class WebpushFcmOptions
    {
        /// <summary>
        /// Gets or sets the link to open when the user clicks on the notification. For all URL values, HTTPS is required.
        /// </summary>
        [JsonProperty("link")]
        public string Link { get; set; }
    }
}
