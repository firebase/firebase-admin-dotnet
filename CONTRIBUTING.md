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
[Open a new pull request](https://github.com/firebase/firebase-admin-dotnet/pull/new) and fill
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

Start by installing [.NET](https://dotnet.microsoft.com/en-us/download) 6 or higher or
[.NET Framework](https://dotnet.microsoft.com/en-us/download/dotnet-framework) 4.6.2 or
higher. This installs the `dotnet` command-line utility into the system.

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


Integration tests are executed against a real life Firebase project. If you do not already
have one suitable for running the tests against, you can create a new project in the
[Firebase Console](https://console.firebase.google.com) following the setup guide below.
If you already have a Firebase project, you'll need to obtain credentials to communicate and
authorize access to your Firebase project:


1. Service account certificate: This allows access to your Firebase project through a service account
which is required for all integration tests. This can be downloaded as a JSON file from the 
**Settings > Service Accounts** tab of the Firebase console when you click the
**Generate new private key** button. Copy the file into the repo so it's available at 
`FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_cert.json`.
   > **Note:** Service accounts should be carefully managed and their keys should never be stored in publicly accessible source code or repositories.


2. Web API key: This allows for Auth sign-in needed for some Authentication and Tenant Management
integration tests. This is displayed in the **Settings > General** tab of the Firebase console
after enabling Authentication as described in the steps below. Copy it and save to a new text
file at `FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_apikey.txt`.


Set up your Firebase project as follows:


1. Enable Authentication:
   1. Go to the Firebase Console, and select **Authentication** from the **Build** menu.
   2. Click on **Get Started**.
   3. Select **Sign-in method > Add new provider > Email/Password** then enable both the
   **Email/Password** and **Email link (passwordless sign-in)** options.


2. Enable Firestore:
   1. Go to the Firebase Console, and select **Firestore Database** from the **Build** menu.
   2. Click on the **Create database** button. You can choose to set up Firestore either in
   the production mode or in the test mode.


3. Enable Realtime Database:
   1. Go to the Firebase Console, and select **Realtime Database** from the **Build** menu.
   2. Click on the **Create Database** button. You can choose to set up the Realtime Database
   either in the locked mode or in the test mode.

   > **Note:** Integration tests are not run against the default Realtime Database reference and are
   instead run against a database created at `https://{PROJECT_ID}.firebaseio.com`.
   This second Realtime Database reference is created in the following steps.

   3. In the **Data** tab click on the kebab menu (3 dots) and select **Create Database**.
   4. Enter your Project ID (Found in the **General** tab in **Account Settings**) as the
   **Realtime Database reference**. Again, you can choose to set up the Realtime Database
   either in the locked mode or in the test mode.


4. Enable Storage:
   1. Go to the Firebase Console, and select **Storage** from the **Build** menu.
   2. Click on the **Get started** button. You can choose to set up Cloud Storage
   either in the production mode or in the test mode.


5. Enable the IAM API:
   1. Go to the [Google Cloud console](https://console.cloud.google.com)
   and make sure your Firebase project is selected.
   2. Select **APIs & Services** from the main menu, and click the
   **ENABLE APIS AND SERVICES** button.
   3. Search for and enable **Identity and Access Management (IAM) API** by Google Enterprise API.


6. Enable Tenant Management:
   1. Go to
   [Google Cloud console | Identity Platform](https://console.cloud.google.com/customer-identity/)
   and if it is not already enabled, click **Enable**.
   2. Then
   [enable multi-tenancy](https://cloud.google.com/identity-platform/docs/multi-tenancy-quickstart#enabling_multi-tenancy)
   for your project.


7. Ensure your service account has the **Firebase Authentication Admin** role. This is required
to ensure that exported user records contain the password hashes of the user accounts:
   1. Go to [Google Cloud console | IAM & admin](https://console.cloud.google.com/iam-admin).
   2. Find your service account in the list. If not added click the pencil icon to edit its
   permissions.
   3. Click **ADD ANOTHER ROLE** and choose **Firebase Authentication Admin**.
   4. Click **SAVE**.


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
