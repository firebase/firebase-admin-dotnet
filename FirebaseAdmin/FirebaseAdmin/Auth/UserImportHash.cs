using System;
using System.Collections.Generic;
using System.Text;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a hash algorithm and the related configuration parameters used to hash user
    /// passwords.An instance of this class must be specified if importing any users with password hashes.
    /// </summary>
    public abstract class UserImportHash
    {
        private readonly string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserImportHash"/> class.
        /// </summary>
        /// <param name="name">The name of the hashing algorithm.</param>
        protected UserImportHash(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Builds the configuration properties of the related algorithm used to hash user passwords.
        /// </summary>
        /// <returns>A dictionary containing the hash configurations.</returns>
        public IReadOnlyDictionary<string, object> Properties()
        {
            var properties = new Dictionary<string, object>
            {
                { "hashAlgorithm", this.name },
            };
            foreach (var entry in this.Options())
            {
                properties.Add(entry.Key, entry.Value);
            }

            return properties;
        }

        /// <summary>
        /// Creates a dictionary containing additional options related to the hash configurations.
        /// </summary>
        /// <returns>A dictionary with additional options related to the hash configurations.</returns>
        protected abstract IReadOnlyDictionary<string, object> Options();
    }
}
