[![Build Status](https://github.com/firebase/firebase-admin-dotnet/workflows/Continuous%20Integration/badge.svg)](https://github.com/firebase/firebase-admin-dotnet/actions)

# Firebase Admin .NET SDK

## Table of Contents

 * [Overview](#overview)
 * [Installation](#installation)
 * [Contributing](#contributing)
 * [Supported Frameworks](#supported-frameworks)
 * [Documentation](#documentation)
 * [License and Terms](#license-and-terms)

## Overview

[Firebase](https://firebase.google.com) provides the tools and infrastructure
you need to develop apps, grow your user base, and earn money. The Firebase
Admin .NET SDK enables access to Firebase services from privileged environments
(such as servers or cloud) in .NET. Currently this SDK provides Firebase custom
authentication support.

For more information, please visit the
[Firebase Admin SDK setup guide](https://firebase.google.com/docs/admin/setup/).

## Installation

The Firebase Admin .NET SDK is available on Nuget as `FirebaseAdmin`. Use the
following command to install it with the Nuget package manager:

```
$ Install-Package FirebaseAdmin -Version $VERSION
```

Or use the `dotnet` command-line utility as follows:

```
$ dotnet add package FirebaseAdmin --version $VERSION
```

## Contributing

Development of the Admin .NET SDK is mostly driven by our open source community.
We welcome and encourage contributions from the developer community to further
improve and expand this project. If you would like a new feature or an API
added to this SDK, please file an issue and provide a pull request.
You can use other Firebase Admin SDKs as a reference on how certain features
should be implemented:

 * [Node.js](https://github.com/firebase/firebase-admin-node)
 * [Java](https://github.com/firebase/firebase-admin-java)
 * [Python](https://github.com/firebase/firebase-admin-python)
 * [Go](https://github.com/firebase/firebase-admin-go)

Please refer to the [CONTRIBUTING page](./CONTRIBUTING.md) for more information
about how you can contribute to this project. In addition to the pull requests,
We also welcome bug reports, feature requests, and code review feedback.

## Supported Frameworks

Admin .NET SDK supports the following frameworks:

* .NET Framework 4.6.2+
* .NET Standard 2.0
* .NET 6.0+

This is consistent with the frameworks supported by other .NET libraries
associated with Google Cloud Platform.

## Documentation

* [Setup Guide](https://firebase.google.com/docs/admin/setup/)
* [Authentication Guide](https://firebase.google.com/docs/auth/admin/)
* [API Reference](https://firebase.google.com/docs/reference/admin/dotnet/)
* [Release Notes](https://firebase.google.com/support/release-notes/admin/dotnet)

## License and Terms

Firebase Admin .NET SDK is licensed under the
[Apache License, version 2.0](http://www.apache.org/licenses/LICENSE-2.0).

Your use of Firebase is governed by the
[Terms of Service for Firebase Services](https://firebase.google.com/terms/).
