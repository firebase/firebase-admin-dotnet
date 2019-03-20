using System;
using Newtonsoft.Json;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// AccessToken
    /// </summary>
    public class GoogleOAuthAccessToken
    {
        /// <summary>
        /// ExpiresIn
        /// </summary>
        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
        /// <summary>
        /// Token
        /// </summary>
        [JsonProperty("access_token")]

        public string AccessToken { get; set; }


        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsExpired()
        {
            return false;
        }

        /// <summary>
        /// TokenType
        /// </summary>
        [JsonProperty("token_type")]
        public string TokenType { get; set; }
    }
}
