using System;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Resolves the IdToolkitUrl.
    /// </summary>
    public class IdToolkitHostResolver
    {
        private const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/v2/projects/{0}";
        private const string IdToolkitEmulatorUrl = "http://{0}/identitytoolkit.googleapis.com/v2/projects/{0}";

        private string projectId;

        /// <summary>
        /// Initializes a new instance of the <see cref="IdToolkitHostResolver" /> class.
        /// </summary>
        /// <param name="projectId">The Firebase Project ID to use.</param>
        public IdToolkitHostResolver(string projectId)
        {
            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Must provide a project ID to resolve");
            }

            this.projectId = projectId;
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
                ? string.Format(IdToolkitEmulatorUrl, emulatorHostEnvVar, this.projectId)
                : string.Format(IdToolkitUrl, this.projectId);
        }
    }
}
