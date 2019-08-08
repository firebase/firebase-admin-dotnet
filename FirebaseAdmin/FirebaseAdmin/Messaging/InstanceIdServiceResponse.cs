using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Response from an operation that subscribes or unsubscribes registration tokens to a topic.
    /// See <see cref="FirebaseMessaging.SubscribeToTopicAsync(IReadOnlyList{string}, string)"/> and
    /// <see cref="FirebaseMessaging.UnsubscribeFromTopicAsync(IReadOnlyList{string}, string)"/>.
    /// </summary>
    internal class InstanceIdServiceResponse
    {
        /// <summary>
        /// Gets the errors returned by the operation.
        /// </summary>
        [JsonProperty("results")]
        public List<InstanceIdServiceResponseElement> Results { get; private set; }

        /// <summary>
        /// Gets the number of errors returned by the operation.
        /// </summary>
        public int ErrorCount => Results?.Count(results => results.HasError) ?? 0;

        /// <summary>
        /// Gets the number of results returned by the operation.
        /// </summary>
        public int ResultCount => Results?.Count() ?? 0;

        /// <summary>
        /// An instance Id response error.
        /// </summary>
        internal class InstanceIdServiceResponseElement
        {
            /// <summary>
            /// Gets a value indicating the error in this element of the response array. If this is empty this indicates success.
            /// </summary>
            [JsonProperty("error")]
            public string Error { get; private set; }

            /// <summary>
            /// Gets a value indicating whether this response element in the response array is an error, as an empty element indicates success.
            /// </summary>
            public bool HasError => !string.IsNullOrEmpty(Error);
        }
    }
}
