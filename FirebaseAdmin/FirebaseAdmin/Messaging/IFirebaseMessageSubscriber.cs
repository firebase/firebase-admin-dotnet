// Copyright 2022, Google Inc. All rights reserved.
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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Service to subscribe or unsubscribe registration tokens to a topic.
    /// </summary>
    public interface IFirebaseMessageSubscriber
    {
        /// <summary>
        /// Subscribes a list of registration tokens to a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to subscribe.</param>
        /// <param name="topic">The topic name to subscribe to. /topics/ will be prepended to the topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving details about the topic subscription operations.</returns>
        Task<TopicManagementResponse> SubscribeToTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic);

        /// <summary>
        /// Unsubscribes a list of registration tokens from a topic.
        /// </summary>
        /// <param name="registrationTokens">A list of registration tokens to unsubscribe.</param>
        /// <param name="topic">The topic name to unsubscribe from. /topics/ will be prepended to the topic name provided if absent.</param>
        /// <returns>A task that completes with a <see cref="TopicManagementResponse"/>, giving details about the topic unsubscription operations.</returns>
        Task<TopicManagementResponse> UnsubscribeFromTopicAsync(
            IReadOnlyList<string> registrationTokens, string topic);
    }
}
