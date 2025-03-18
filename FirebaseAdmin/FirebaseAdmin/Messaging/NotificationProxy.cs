// Copyright 2025, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Proxy behavior that can be set on an <see cref="AndroidNotification"/>.
    /// </summary>
    public enum NotificationProxy
    {
        /// <summary>
        /// Try to proxy this notification.
        /// </summary>
        Allow,

        /// <summary>
        /// Do not proxy this notification..
        /// </summary>
        Deny,

        /// <summary>
        /// Only try to proxy this notification if its `AndroidMessagePriority`
        /// was lowered from HIGH to NORMAL on the device.
        /// </summary>
        IfPriorityLowered,
    }
}
