# Unreleased

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