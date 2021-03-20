using System;

namespace FirebaseAdmin.Util
{
    internal enum IdToolkitVersion
    {
        V1,
        V2,
    }

    internal class Utils
    {
        internal static string ResolveIdToolkitHost(string projectId, IdToolkitVersion version = IdToolkitVersion.V2)
        {
            const string IdToolkitUrl = "https://identitytoolkit.googleapis.com/{0}/projects/{1}";
            const string IdToolkitEmulatorUrl = "http://{0}/identitytoolkit.googleapis.com/{1}/projects/{2}";

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Must provide a project ID to resolve");
            }

            var emulatorHostEnvVar = Environment.GetEnvironmentVariable("FIREBASE_AUTH_EMULATOR_HOST");
            var useEmulatorHost = !string.IsNullOrWhiteSpace(emulatorHostEnvVar);
            var versionAsString = version.ToString().ToLower();
            return useEmulatorHost
                ? string.Format(IdToolkitEmulatorUrl, emulatorHostEnvVar, versionAsString, projectId)
                : string.Format(IdToolkitUrl, versionAsString, projectId);
        }
    }
}
