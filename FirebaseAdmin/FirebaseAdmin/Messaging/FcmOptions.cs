using FirebaseAdmin.Messaging.Util;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents FCM options.
    /// </summary>
    public sealed class FcmOptions
    {
        /// <summary>
        /// Gets or sets analytics label.
        /// </summary>
        [JsonProperty("analytics_label")]
        public string AnalyticsLabel { get; set; }

        /// <summary>
        /// Copies this FCM options, and validates the content of it to ensure that it can
        /// be serialized into the JSON format expected by the FCM service.
        /// </summary>
        internal FcmOptions CopyAndValidate()
        {
            var copy = new FcmOptions()
            {
                AnalyticsLabel = this.AnalyticsLabel,
            };
            AnalyticsLabelChecker.ValidateAnalyticsLabel(copy.AnalyticsLabel);

            return copy;
        }
    }
}
