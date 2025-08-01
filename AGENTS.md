This document provides a detailed guide for AI agents to understand and contribute to the Firebase Admin DotNet SDK. Adhering to these guidelines is crucial for maintaining code quality, consistency, and idiomatic C# practices.

## High-Level Overview

The Firebase Admin DotNet SDK provides C# developers with access to Firebase services on privileged environments. Its design emphasizes idiomatic C#, thread-safety, and a consistent, predictable API surface.

## Directory Structure

-   **`FirebaseAdmin/FirebaseAdmin.sln`**: The main solution file for the entire project.
-   **`FirebaseAdmin/FirebaseAdmin/`**: The primary source code for the SDK.
    -   **`FirebaseAdmin/FirebaseAdmin/FirebaseApp.cs`**: The main entry point and initialization class for the SDK.
    -   **`FirebaseAdmin/FirebaseAdmin/Auth/`**: Source code for the Authentication service.
    -   **`FirebaseAdmin/FirebaseAdmin/Messaging/`**: Source code for the Firebase Cloud Messaging (FCM) service.
    -   **`FirebaseAdmin/FirebaseAdmin/Internal/`**: Internal utilities and classes not meant for public consumption. Direct use of this code in public APIs is strictly forbidden.
-   **`FirebaseAdmin/FirebaseAdmin.Tests/`**: Unit tests for the SDK.
-   **`FirebaseAdmin/FirebaseAdmin.IntegrationTests/`**: Integration tests.
-   **`FirebaseAdmin/FirebaseAdmin.Snippets/`**: Code snippets used in documentation.

## Core Design Patterns

-   **Initialization**: The SDK is initialized via the `FirebaseApp.Create()` method, which configures a singleton instance.
-   **Service Clients**: Services like `FirebaseAuth` and `FirebaseMessaging` are accessed through the `FirebaseApp.DefaultInstance`.
-   **Error Handling**: Exceptions are the primary mechanism for error handling. Custom exceptions inherit from `FirebaseException` (defined in `FirebaseAdmin/FirebaseAdmin/FirebaseException.cs`) and are defined within each service module.
-   **HTTP Communication**: All outgoing HTTP requests are managed by a centralized `HttpClient` provided via the `AppOptions.HttpClientFactory`.
-   **Asynchronous Operations**: Asynchronous operations are handled using `Task`-based asynchronous programming (`async`/`await`).

## Coding Style and Naming Conventions

-   **Formatting**: Code style is enforced using `stylecop`. Run `dotnet format` to apply the rules.
-   **Naming**:
    -   Classes and public methods use `PascalCase`.
    -   Private members are not explicitly prefixed, but it is preferred to prefix them with an underscore (`_`).
    -   Constants are `PascalCase`.

## Testing Philosophy

-   **Unit Tests**: Located in `FirebaseAdmin.Tests/`, with a file naming pattern of `*Test.cs`. `xunit` is the testing framework. Moq is the preferred library for mocking dependencies.
-   **Integration Tests**: Located in `FirebaseAdmin.IntegrationTests/`. These tests interact with live Firebase services and require a configured service account.

## Dependency Management

-   **Manager**: Dependencies are managed using NuGet.
-   **Manifest**: The `FirebaseAdmin/FirebaseAdmin/FirebaseAdmin.csproj` file lists all dependencies.
-   **Command**: To add a dependency, use `dotnet add package <PACKAGE_NAME>`.

## Critical Developer Journeys

### Journey 1: How to Add a New API Method

1.  **Public Method**: Define the new public method in the relevant service class (e.g., `FirebaseAuth.cs`).
2.  **Internal Logic**: Implement the core logic as a private method.
3.  **HTTP Client**: Use the internal `HttpClient` to make the API call.
4.  **Error Handling**: Wrap API calls in `try-catch` blocks and throw appropriate `FirebaseException` subtypes.
5.  **Testing**: Add a unit test in the corresponding `*Test.cs` file and an integration test in `FirebaseAdmin.IntegrationTests/`.
6.  **Snippet**: Add a code snippet in `FirebaseAdmin.Snippets/` to demonstrate the new feature.

### Journey 2: How to Add a New Field to an Existing API Response

1.  **Data Model**: Locate and modify the relevant data model class in the service's directory.
2.  **Testing**: Update unit and integration tests to verify the presence and correctness of the new field.

## Critical Do's and Don'ts

-   **DO**: Use the centralized `HttpClient` for all API calls.
-   **DO**: Follow the established `async`/`await` patterns for asynchronous code.
-   **DON'T**: Expose types from the `FirebaseAdmin/FirebaseAdmin/Internal/` directory in any public API.
-   **DON'T**: Introduce new third-party dependencies without a compelling reason and discussion with the maintainers.

## Commit and Pull Request Generation

After implementing and testing a change, you must create a commit and Pull Request following these rules:

**1. Commit Message Format**: The commit message is critical and is used to generate the Pull Request. It MUST follow this structure:
   - **Title**: Use the [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) specification: `type(scope): subject`.
     - `scope` should be the service package changed (e.g., `auth`, `db`, `deps`).
     - **Note on Scopes**: Some services use specific abbreviations. Use the abbreviation if one exists. Common abbreviations include:
       - `messaging` -> `fcm`
       - `dataconnect` -> `fdc`
       - `database` -> `rtdb`
       - `appcheck` -> `fac`
     - Example: `fix(auth): Resolve issue with custom token verification`
     - Example: `feat(fcm): Add support for multicast messages`
   - **Body**: The body is separated from the title by a blank line and MUST contain the following, in order:
     1. A brief explanation of the problem and the solution.
     2. A summary of the testing strategy (e.g., "Added a new unit test to verify the fix.").
     3. A **Context Sources** section that lists the `id` and repository path of every `AGENTS.md` file you used.

**2. Example Commit Message**:
   ```
   feat(fcm): Add support for multicast messages

   This change introduces a new `SendMulticast` method to the messaging client, allowing developers to send a single message to multiple tokens efficiently.

   Testing: Added unit tests in `messaging_test.cs` with a mock server and an integration test in `integration/messaging_test.cs`.

   Context Sources Used:
   - id: firebase-admin-dotnet (`/AGENTS.md`)
   - id: firebase-admin-dotnet-messaging (`/messaging/AGENTS.md`)
   ```

**3. Pull Request**: The Pull Request title and description should be populated directly from the commit message's title and body.