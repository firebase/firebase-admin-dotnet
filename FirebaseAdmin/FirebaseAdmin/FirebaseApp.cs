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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Google;
using Google.Apis.Logging;
using Google.Apis.Auth.OAuth2;

[assembly: InternalsVisibleToAttribute("FirebaseAdmin.Tests")]
namespace FirebaseAdmin 
{
    internal delegate TResult ServiceFactory<out TResult>() where TResult: IFirebaseService;

    /// <summary>
    /// This is the entry point to the Firebase Admin SDK. It holds configuration and state common
    /// to all APIs exposed from the SDK.
    /// <para>Use one of the provided <c>Create()</c> methods to obtain a new instance.
    /// See <a href="https://firebase.google.com/docs/admin/setup#initialize_the_sdk">
    /// Initialize the SDK</a> for code samples and detailed documentation.</para>
    /// </summary>
    public sealed class FirebaseApp
    {
        private const string DefaultAppName = "[DEFAULT]";

        internal static readonly IReadOnlyList<string> DefaultScopes = ImmutableList.Create(
            // Enables access to Firebase Realtime Database.
            "https://www.googleapis.com/auth/firebase",

            // Enables access to the email address associated with a project.
            "https://www.googleapis.com/auth/userinfo.email",

            // Enables access to Google Identity Toolkit (for user management APIs).
            "https://www.googleapis.com/auth/identitytoolkit",

            // Enables access to Google Cloud Storage.
            "https://www.googleapis.com/auth/devstorage.full_control",

            // Enables access to Google Cloud Firestore
            "https://www.googleapis.com/auth/cloud-platform",
            "https://www.googleapis.com/auth/datastore"
        );

        private static readonly Dictionary<string, FirebaseApp> Apps = new Dictionary<string, FirebaseApp>();

        private static readonly ILogger Logger = ApplicationContext.Logger.ForType<FirebaseApp>();

        // Guards the mutable state local to an app instance.
        private readonly Object _lock = new Object();
        private bool _deleted = false;
        private readonly AppOptions _options;

        /// <summary>
        /// A copy of the <see cref="AppOptions"/> this app was created with.
        /// </summary>
        public AppOptions Options
        {
            get
            {
                return new AppOptions(_options);
            }
        }

        /// <summary>
        /// Name of this app.
        /// </summary>
        public string Name { get; }

        // A collection of stateful services initialized using this app instance (e.g.
        // FirebaseAuth). Services are tracked here so they can be cleaned up when the app is
        // deleted.
        private readonly Dictionary<string, IFirebaseService> _services = new Dictionary<string, IFirebaseService>();

        private FirebaseApp(AppOptions options, string name)
        {
            _options = new AppOptions(options);
            if (_options.Credential == null)
            {
                throw new ArgumentNullException("Credential must be set");
            }
            if (_options.Credential.IsCreateScopedRequired)
            {
                _options.Credential = _options.Credential.CreateScoped(DefaultScopes);
            }
            Name = name;
        }

        /// <summary>
        /// Deletes this app instance and cleans up any state associated with it. Once an app has
        /// been deleted, accessing any services related to it will result in an exception.
        /// If the app is already deleted, this method is a no-op.
        /// </summary>
        public void Delete()
        {
            // Clean up local state
            lock (_lock)
            {
                _deleted = true;
                foreach (var entry in _services)
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
                _services.Clear();
            }
            // Clean up global state
            lock (Apps)
            {
                Apps.Remove(Name);
            }
        }

        internal T GetOrInit<T>(string id, ServiceFactory<T> initializer) where T : class, IFirebaseService
        {
            lock (_lock)
            {
                if (_deleted)
                {
                    throw new InvalidOperationException("Cannot use an app after it has been deleted");
                }
                IFirebaseService service;
                if (!_services.TryGetValue(id, out service))
                {
                    service = initializer();
                    _services.Add(id, service);  
                }
                return (T) service;
            }
        }

        internal string GetProjectId()
        {
            if (!string.IsNullOrEmpty(Options.ProjectId))
            {
                return Options.ProjectId;
            }
            var projectId = Options.Credential.ToServiceAccountCredential()?.ProjectId;
            if (!String.IsNullOrEmpty(projectId))
            {
                return projectId;
            }
            foreach (var variableName in new [] {"GOOGLE_CLOUD_PROJECT", "GCLOUD_PROJECT"})
            {
                projectId = Environment.GetEnvironmentVariable(variableName);
                if (!String.IsNullOrEmpty(projectId))
                {
                    return projectId;
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

        private static AppOptions GetOptionsFromEnvironment()
        {
            return new AppOptions()
            {
                Credential = GoogleCredential.GetApplicationDefault(),
            };
        }

        /// <summary>
        /// The default app instance. This property is <c>null</c> if the default app instance
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
    }
}
