using System;

namespace FirebaseAdmin
{ /// <summary>
  /// Represents a cryptographic key.
  /// </summary>
    public class Key
    {
        /// <summary>
        /// Gets or sets the key type.
        /// </summary>
        public string Kty { get; set; }

        /// <summary>
        /// Gets or sets the intended use of the key.
        /// </summary>
        public string Use { get; set; }

        /// <summary>
        /// Gets or sets the algorithm associated with the key.
        /// </summary>
        public string Alg { get; set; }

        /// <summary>
        /// Gets or sets the key ID.
        /// </summary>
        public string Kid { get; set; }

        /// <summary>
        /// Gets or sets the modulus for the RSA public key.
        /// </summary>
        public string N { get; set; }

        /// <summary>
        /// Gets or sets the exponent for the RSA public key.
        /// </summary>
        public string E { get; set; }
    }
}
