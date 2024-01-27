using System.Collections.Generic;
using Newtonsoft.Json;

namespace FirebaseAdmin.AppCheck
{
    /// <summary>
    /// Interface representing a decoded Firebase App Check token, returned from the {@link AppCheck.verifyToken} method..
    /// </summary>
    public class AppCheckDecodedToken
    {
        internal AppCheckDecodedToken(Args args)
        {
            this.AppId = args.AppId;
            this.Issuer = args.Issuer;
            this.Subject = args.Subject;
            this.Audience = args.Audience;
            this.ExpirationTimeSeconds = (int)args.ExpirationTimeSeconds;
            this.IssuedAtTimeSeconds = (int)args.IssuedAtTimeSeconds;
        }

        /// <summary>
        /// Gets or sets the issuer identifier for the issuer of the response.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Gets or sets the Firebase App ID corresponding to the app the token belonged to.
        /// As a convenience, this value is copied over to the {@link AppCheckDecodedToken.app_id | app_id} property.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets the audience for which this token is intended.
        /// This value is a JSON array of two strings, the first is the project number of your
        /// Firebase project, and the second is the project ID of the same project.
        /// </summary>
        public string[] Audience { get; set; }

        /// <summary>
        /// Gets or sets the App Check token's c time, in seconds since the Unix epoch. That is, the
        /// time at which this App Check token expires and should no longer be considered valid.
        /// </summary>
        public int ExpirationTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the App Check token's issued-at time, in seconds since the Unix epoch. That is, the
        /// time at which this App Check token was issued and should start to be considered valid.
        /// </summary>
        public int IssuedAtTimeSeconds { get; set; }

        /// <summary>
        /// Gets or sets the App ID corresponding to the App the App Check token belonged to.
        /// This value is not actually one of the JWT token claims. It is added as a
        /// convenience, and is set as the value of the {@link AppCheckDecodedToken.sub | sub} property.
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// Gets or sets key .
        /// </summary>
        public Dictionary<string, string> Key { get; set; }
        ////[key: string]: any;

        internal sealed class Args
        {
            public string AppId { get; internal set; }

            [JsonProperty("app_id")]
            internal string Issuer { get; set; }

            [JsonProperty("sub")]
            internal string Subject { get; set; }

            [JsonProperty("aud")]
            internal string[] Audience { get; set; }

            [JsonProperty("exp")]
            internal long ExpirationTimeSeconds { get; set; }

            [JsonProperty("iat")]
            internal long IssuedAtTimeSeconds { get; set; }

            [JsonIgnore]
            internal IReadOnlyDictionary<string, object> Claims { get; set; }
        }
    }
}
