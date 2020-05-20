# Contributing | Firebase Admin .NET SDK

Thank you for contributing to the Firebase community!

 - [Have a usage question?](#question)
 - [Think you found a bug?](#issue)
 - [Have a feature request?](#feature)
 - [Want to submit a pull request?](#submit)
 - [Need to get set up locally?](#local-setup)


## <a name="question"></a>Have a usage question?

We get lots of those and we love helping you, but GitHub is not the best place for them. Issues
which just ask about usage will be closed. Here are some resources to get help:

- Go through the [guides](https://firebase.google.com/docs/admin/setup/)
- Read the full [API reference](https://firebase.google.com/docs/reference/admin/)

If the official documentation doesn't help, try asking a question on the
[Firebase Google Group](https://groups.google.com/forum/#!forum/firebase-talk/) or one of our
other [official support channels](https://firebase.google.com/support/).

**Please avoid double posting across multiple channels!**


## <a name="issue"></a>Think you found a bug?

Yeah, we're definitely not perfect!

Search through [old issues](https://github.com/firebase/firebase-admin-dotnet/issues) before
submitting a new issue as your question may have already been answered.

If your issue appears to be a bug, and hasn't been reported,
[open a new issue](https://github.com/firebase/firebase-admin-dotnet/issues/new). Please use the
provided bug report template and include a minimal repro.

If you are up to the challenge, [submit a pull request](#submit) with a fix!


## <a name="feature"></a>Have a feature request?

Great, we love hearing how we can improve our products! Share you idea through our
[feature request support channel](https://firebase.google.com/support/contact/bugs-features/).


## <a name="submit"></a>Want to submit a pull request?

Sweet, we'd love to accept your contribution! In fact this project is mainly
driven by contributions from our community.
[Open a new pull request](https://github.com/firebase/firebase-admin-dotnet/pull/new/master) and fill
out the provided template.

**If you want to implement a new feature, please open an issue with a proposal first so that we can
figure out if the feature makes sense and how it will work.**

Make sure your changes pass our tests on your local machine. We've hooked
up this repo with continuous integration to double check those things for you.

Most non-trivial changes should include some extra test coverage. If you aren't sure how to add
tests, feel free to submit regardless and ask us for some advice.

Finally, you will need to sign our
[Contributor License Agreement](https://cla.developers.google.com/about/google-individual),
and go through our code review process before we can accept your pull request.

### Contributor License Agreement

Contributions to this project must be accompanied by a Contributor License
Agreement. You (or your employer) retain the copyright to your contribution.
This simply gives us permission to use and redistribute your contributions as
part of the project. Head over to <https://cla.developers.google.com/> to see
your current agreements on file or to sign a new one.

You generally only need to submit a CLA once, so if you've already submitted one
(even if it was for a different project), you probably don't need to do it
again.

### Code reviews

All submissions, including submissions by project members, require review. We
use GitHub pull requests for this purpose. Consult
[GitHub Help](https://help.github.com/articles/about-pull-requests/) for more
information on using pull requests.


## <a name="local-setup"></a>Need to get set up locally?

### Initial Setup

This section explains how to set up a development environment on Linux or Mac. Windows users
should be able to import the solution file (`FirebaseAdmin/FirebaseAdmin.sln`) into Visual
Studio.

Start by installing [.NET Core](https://www.microsoft.com/net/download) 2.1 or higher. This
installs the `dotnet` command-line utility into the system.

Run the following commands from the command line to get your local environmentset up:

```bash
$ git clone https://github.com/firebase/firebase-admin-dotnet.git
$ cd firebase-admin-dotnet/FirebaseAdmin # Change into the FirebaseAdmin solution directory
$ dotnet restore                         # Install dependencies
```

### Running Tests

There are two test suites, housed under two separate subdirectories:

* `FirebaseAdmin/FirebaseAdmin.Tests`: Unit tests
* `FirebaseAdmin/FirebaseAdmin.IntegrationTests`: Integration tests

The unit test suite is intended to be run during development, and the integration test suite is
intended to be run before packaging up release candidates.

To run the unit test suite:

```bash
$ dotnet test FirebaseAdmin.Tests
```

To invoke code coverage tools, run the unit tests as follows:

```bash
$ dotnet test FirebaseAdmin.Tests /p:CollectCoverage=true
```

The above command calculates and displays a code coverage summary. To produce a more detailed
code coverage report, you can use a tool like
[Report Generator](https://github.com/danielpalme/ReportGenerator):

```bash
$ dotnet test FirebaseAdmin.Tests /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
$ dotnet path/to/ReportGenerator.dll -reports:./FirebaseAdmin.Tests/coverage.opencover.xml -targetdir:./reports
```

This generates a collection of HTML code coverage reports in a local subdirectory named
`reports/`.

The integration test suite requires a service account JSON key file, and an API key for a Firebase
project. Create a new project in the [Firebase console](https://console.firebase.google.com) if
you do not already have one. Use a separate, dedicated project for integration tests since the
test suite makes a large number of writes to the Firebase realtime database. Download the service
account key file from the "Settings > Service Accounts" page of the project, and copy it to
`FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_cert.json`. Also obtain the
API key for the same project from "Settings > General", and save it to
`FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_apikey.txt`.

You'll also need to grant your service account the 'Firebase Authentication Admin' role. This is
required to ensure that exported user records contain the password hashes of the user accounts:
1. Go to [Google Cloud Platform Console / IAM & admin](https://console.cloud.google.com/iam-admin).
2. Find your service account in the list, and click the 'pencil' icon to edit it's permissions.
3. Click 'ADD ANOTHER ROLE' and choose 'Firebase Authentication Admin'.
4. Click 'SAVE'.

For some of the Firebase Auth integration tests, it is required to enable the Email/Password
sign-in method:
1. Go to the [Firebase console](https://console.firebase.google.com).
2. Click on 'Authentication', and select the 'Sign-in method' tab. 
3. Enable 'Email/Password'.
4. Enable 'Email link (passwordless sign-in)'.

Finally, to run the integration test suite:

```bash
$ dotnet test FirebaseAdmin.IntegrationTests
```

### Repo Organization

Here are some highlights of the directory structure and notable source files

* `FirebaseAdmin/` - Solution directory containing all source code and tests.
  * `FirebaseAdmin.sln/` - Visual Studio solution file for the project.
  * `FirebaseAdmin/` - Source directory.
  * `FirebaseAdmin.Tests/` - Unit tests directory.
  * `FirebaseAdmin.IntegrationTests/` - Integration tests directory.
  * `FirebaseAdmin.Snippets/` - Example code snippets for the SDK.
