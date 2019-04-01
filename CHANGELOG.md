# Unreleased

- [added] `AppOptions` now supports setting an `HttpClientFactory`, which
  is useful when deploying the SDK behinh a proxy server.

# v1.3.0

- [added] Implemented the `SendAllAsync()` and `SendMulticastAsync()` APIs in
  the `FirebaseMessaging` class.

# v1.2.1

- [fixed] The `VerifyIdTokenAsync()` function now tolerates a clock skew of up
  to 5 minutes when comparing JWT timestamps.

# v1.2.0

- [added] Implemented the `FirebaseMessaging` API for sending notifications
  with FCM.

# v1.1.0

- [added] Implemented the `SetCustomUserClaimsAsync()` API in the
  `FirebaseAuth` class.

# v1.0.0

- [added] Initial release of the Admin .NET SDK. See
  [Add the Firebase Admin SDK to your Server](/docs/admin/setup/) to get
  started.
- [added] You can configure the SDK to use service account credentials, user
  credentials (refresh tokens), or Google Cloud application default credentials
  to access your Firebase project.

### Authentication

- [added] The initial release includes the `CreateCustomTokenAsync()`,
  and `VerifyIdTokenAsync()` methods for minting custom
  authentication tokens and verifying Firebase ID tokens.
