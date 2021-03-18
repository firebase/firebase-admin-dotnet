using System;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Enum for versions available for the identity toolkit api.
    /// </summary>
    public enum IdToolkitVersion
    {
        /// <summary>
        /// Version 1 of the api.
        /// </summary>
        V1,

        /// <summary>
        /// Version 2 of the api.
        /// </summary>
        V2,
    }

    /// <summary>
    /// Resolves the IdToolkitUrl.
    /// </summary>
    public class IdToolkitHostResolver
    {
        private const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/{0}/projects/{1}";
        private const string IdToolkitEmulatorUrl = "http://{0}/identitytoolkit.googleapis.com/{1}/projects/{2}";

        private readonly string projectId;
        private readonly IdToolkitVersion version;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdToolkitHostResolver" /> class.
        /// </summary>
        /// <param name="projectId">The Firebase Project ID to use.</param>
        /// <param name="version">Which version of the identitytoolkit api to use.</param>
        public IdToolkitHostResolver(string projectId, IdToolkitVersion version)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Must provide a project ID to resolve");
            }

            this.projectId = projectId;
            this.version = version;
        }

        /// <summary>
        /// Determines whether or not FIREBASE_AUTH_EMULATOR_HOST env variable exists
        /// and then builds the appropriate identitytoolkit url.
        /// </summary>
        /// <returns>The resolved URL.</returns>
        public string Resolve()
        {
            var emulatorHostEnvVar = Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");
            var useEmulatorHost = !string.IsNullOrWhiteSpace(emulatorHostEnvVar);
            return useEmulatorHost
                ? string.Format(IdToolkitEmulatorUrl, emulatorHostEnvVar, this.version.ToString().ToLower(), this.projectId)
                : string.Format(IdToolkitUrl, this.version.ToString().ToLower(), this.projectId);
        }
    }
}
