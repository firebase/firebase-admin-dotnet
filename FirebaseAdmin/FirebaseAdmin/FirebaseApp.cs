// Copyright 2018, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Logging;

[assembly: InternalsVisibleToAttribute("FirebaseAdmin.Tests,PublicKey=" +
"002400000480000094000000060200000024000052534131000400000100010081328559eaab41" +
"055b84af73469863499d81625dcbba8d8decb298b69e0f783a0958cf471fd4f76327b85a7d4b02" +
"3003684e85e61cf15f13150008c81f0b75a252673028e530ea95d0c581378da8c6846526ab9597" +
"4c6d0bc66d2462b51af69968a0e25114bde8811e0d6ee1dc22d4a59eee6a8bba4712cba839652f" +
"badddb9c")]
namespace FirebaseAdmin
{
    internal delegate TResult ServiceFactory<out TResult>()
        where TResult : IFirebaseService;

    /// <summary>
    /// This is the entry point to the Firebase Admin SDK. It holds configuration and state common
    /// to all APIs exposed from the SDK.
    /// <para>Use one of the provided <c>Create()</c> methods to obtain a new instance.
    /// See <a href="https://firebase.google.com/docs/admin/setup#initialize_the_sdk">
    /// Initialize the SDK</a> for code samples and detailed documentation.</para>
    /// </summary>
    public sealed class FirebaseApp
    {
        internal static readonly IReadOnlyList<string> DefaultScopes = ImmutableList.Create(
            "https://www.googleapis.com/auth/firebase", // RTDB.
            "https://www.googleapis.com/auth/userinfo.email", // RTDB
            "https://www.googleapis.com/auth/identitytoolkit", // User management
            "https://www.googleapis.com/auth/devstorage.full_control", // Cloud Storage
            "https://www.googleapis.com/auth/cloud-platform", // Cloud Firestore
            "https://www.googleapis.com/auth/datastore");

        private const string DefaultAppName = "[DEFAULT]";

        private static readonly Dictionary<string, FirebaseApp> Apps = new Dictionary<string, FirebaseApp>();

        private static readonly ILogger Logger = ApplicationContext.Logger.ForType<FirebaseApp>();

        // Guards the mutable state local to an app instance.
        private readonly object appLock = new object();
        private readonly AppOptions options;

        // A collection of stateful services initialized using this app instance (e.g.
        // FirebaseAuth). Services are tracked here so they can be cleaned up when the app is
        // deleted.
        private readonly Dictionary<string, IFirebaseService> services = new Dictionary<string, IFirebaseService>();
        private bool deleted = false;

        private FirebaseApp(AppOptions options, string name)
        {
            this.options = new AppOptions(options);
            if (this.options.Credential == null)
            {
                throw new ArgumentNullException("Credential must be set");
            }

            if (this.options.Credential.IsCreateScopedRequired)
            {
                this.options.Credential = this.options.Credential.CreateScoped(DefaultScopes);
            }

            if (this.options.HttpClientFactory == null)
            {
                throw new ArgumentNullException("HttpClientFactory must be set");
            }

            this.Name = name;
        }

        /// <summary>
        /// Gets the default app instance. This property is <c>null</c> if the default app instance
        /// doesn't yet exist.
        /// </summary>
        public static FirebaseApp DefaultInstance
        {
            get
            {
                return GetInstance(DefaultAppName);
            }
        }

        /// <summary>
        /// Gets a copy of the <see cref="AppOptions"/> this app was created with.
        /// </summary>
        public AppOptions Options
        {
            get
            {
                return new AppOptions(this.options);
            }
        }

        /// <summary>
        /// Gets the name of this app.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Returns the app instance identified by the given name.
        /// </summary>
        /// <returns>The <see cref="FirebaseApp"/> instance with the specified name or null if it
        /// doesn't exist.</returns>
        /// <exception cref="System.ArgumentException">If the name argument is null or empty.</exception>
        /// <param name="name">Name of the app to retrieve.</param>
        public static FirebaseApp GetInstance(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("App name to lookup must not be null or empty");
            }

            lock (Apps)
            {
                FirebaseApp app;
                if (Apps.TryGetValue(name, out app))
                {
                    return app;
                }
            }

            return null;
        }

        /// <summary>
        /// Creates the default app instance with Google Application Default Credentials.
        /// </summary>
        /// <returns>The newly created <see cref="FirebaseApp"/> instance.</returns>
        /// <exception cref="System.ArgumentException">If the default app instance already
        /// exists.</exception>
        public static FirebaseApp Create()
        {
            return Create(DefaultAppName);
        }

        /// <summary>
        /// Creates an app with the specified name, and Google Application Default Credentials.
        /// </summary>
        /// <returns>The newly created <see cref="FirebaseApp"/> instance.</returns>
        /// <exception cref="System.ArgumentException">If the default app instance already
        /// exists.</exception>
        /// <param name="name">Name of the app.</param>
        public static FirebaseApp Create(string name)
        {
            return Create(null, name);
        }

        /// <summary>
        /// Creates the default app instance with the specified options.
        /// </summary>
        /// <returns>The newly created <see cref="FirebaseApp"/> instance.</returns>
        /// <exception cref="System.ArgumentException">If the default app instance already
        /// exists.</exception>
        /// <param name="options">Options to create the app with. Must at least contain the
        /// <c>Credential</c>.</param>
        public static FirebaseApp Create(AppOptions options)
        {
            return Create(options, DefaultAppName);
        }

        /// <summary>
        /// Creates an app with the specified name and options.
        /// </summary>
        /// <returns>The newly created <see cref="FirebaseApp"/> instance.</returns>
        /// <exception cref="System.ArgumentException">If the default app instance already
        /// exists.</exception>
        /// <param name="options">Options to create the app with. Must at least contain the
        /// <c>Credential</c>.</param>
        /// <param name="name">Name of the app.</param>
        public static FirebaseApp Create(AppOptions options, string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("App name must not be null or empty");
            }

            options = options ?? GetOptionsFromEnvironment();
            lock (Apps)
            {
                if (Apps.ContainsKey(name))
                {
                    if (name == DefaultAppName)
                    {
                        throw new ArgumentException("The default FirebaseApp already exists.");
                    }
                    else
                    {
                        throw new ArgumentException($"FirebaseApp named {name} already exists.");
                    }
                }

                var app = new FirebaseApp(options, name);
                Apps.Add(name, app);
                return app;
            }
        }

        /// <summary>
        /// Deletes this app instance and cleans up any state associated with it. Once an app has
        /// been deleted, accessing any services related to it will result in an exception.
        /// If the app is already deleted, this method is a no-op.
        /// </summary>
        public void Delete()
        {
            // Clean up local state
            lock (this.appLock)
            {
                this.deleted = true;
                foreach (var entry in this.services)
                {
                    try
                    {
                        entry.Value.Delete();
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, "Error while cleaning up service {0}", entry.Key);
                    }
                }

                this.services.Clear();
            }

            // Clean up global state
            lock (Apps)
            {
                Apps.Remove(this.Name);
            }
        }

        /// <summary>
        /// Deleted all the apps created so far. Used for unit testing.
        /// </summary>
        internal static void DeleteAll()
        {
            lock (Apps)
            {
                var copy = new Dictionary<string, FirebaseApp>(Apps);
                foreach (var entry in copy)
                {
                    entry.Value.Delete();
                }

                if (Apps.Count > 0)
                {
                    throw new InvalidOperationException("Failed to delete all apps");
                }
            }
        }

        /// <summary>
        /// Returns the current version of the .NET assembly.
        /// </summary>
        /// <returns>A version string in major.minor.patch format.</returns>
        internal static string GetSdkVersion()
        {
            const int majorMinorPatch = 3;
            return typeof(FirebaseApp).GetTypeInfo().Assembly.GetName().Version.ToString(majorMinorPatch);
        }

        internal T GetOrInit<T>(string id, ServiceFactory<T> initializer)
            where T : class, IFirebaseService
        {
            lock (this.appLock)
            {
                if (this.deleted)
                {
                    throw new InvalidOperationException("Cannot use an app after it has been deleted");
                }

                IFirebaseService service;
                if (!this.services.TryGetValue(id, out service))
                {
                    service = initializer();
                    this.services.Add(id, service);
                }

                return (T)service;
            }
        }

        /// <summary>
        /// Returns the Google Cloud Platform project ID associated with this Firebase app. If a
        /// project ID is specified in <see cref="AppOptions"/>, that value is returned. If not
        /// attempts to determine a project ID from the <see cref="GoogleCredential"/> used to
        /// initialize the app. Looks up the GOOGLE_CLOUD_PROJECT environment variable when all
        /// else fails.
        /// </summary>
        /// <returns>A project ID string or null.</returns>
        internal string GetProjectId()
        {
            if (!string.IsNullOrEmpty(this.Options.ProjectId))
            {
                return this.Options.ProjectId;
            }

            var projectId = this.Options.Credential.ToServiceAccountCredential()?.ProjectId;
            if (!string.IsNullOrEmpty(projectId))
            {
                return projectId;
            }

            foreach (var variableName in new[] { "GOOGLE_CLOUD_PROJECT", "GCLOUD_PROJECT" })
            {
                projectId = Environment.GetEnvironmentVariable(variableName);
                if (!string.IsNullOrEmpty(projectId))
                {
                    return projectId;
                }
            }

            return null;
        }

        private static AppOptions GetOptionsFromEnvironment()
        {
            return new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            };
        }
    }
}
