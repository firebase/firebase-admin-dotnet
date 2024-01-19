using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Check
{
    /// <summary>
    /// Interface representing App Check token options.
    /// </summary>
    public class CyptoSigner
    {
        private readonly RSA Rsa;
        private readonly ServiceAccountCredential credential;

        /// <summary>
        /// Initializes a new instance of the <see cref="CyptoSigner"/> class.
        /// Interface representing App Check token options.
        /// </summary>
        public CyptoSigner(string privateKeyPem)
        {
            if (privateKeyPem is null)
            {
                throw new ArgumentNullException(nameof(privateKeyPem));
            }

            this.Rsa = RSA.Create();
            this.Rsa.ImportFromPem(privateKeyPem.ToCharArray());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CyptoSigner"/> class.
        /// Cryptographically signs a buffer of data.
        /// </summary>
        /// <param name="buffer">To sign data.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task<byte[]> Sign(byte[] buffer)
        {
            if (buffer is null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            // Sign the buffer using the private key and SHA256 hashing algorithm
            var signature = this.privateKey.SignData(buffer, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Task.FromResult(signature);
        }

        internal async Task<string> GetAccountId()
        {
            var request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("http://metadata/computeMetadata/v1/instance/service-accounts/default/email"),
            };
            request.Headers.Add("Metadata-Flavor", "Google");
            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new FirebaseAppCheckException("Error exchanging token.");
            }

            return response.Content.ToString();
        }
    }
}
