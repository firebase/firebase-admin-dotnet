using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    /// <summary>
    /// Represents FCM options.
    /// </summary>
    public class FcmOptions
    {
        /// <summary>
        /// Gets or sets analytics label.
        /// </summary>
        [JsonProperty("analytics_label")]
        public string AnalyticsLabel { get; set; }
    }
}