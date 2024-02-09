#!/bin/bash

set -e

# BUILD_CONFIGURATION=Release
BUILD_CONFIGURATION=Debug

dotnet restore FirebaseAdmin/FirebaseAdmin.sln
dotnet build FirebaseAdmin/FirebaseAdmin.sln --configuration $BUILD_CONFIGURATION --no-restore
dotnet pack FirebaseAdmin/FirebaseAdmin.sln --configuration $BUILD_CONFIGURATION --no-restore

dotnet test FirebaseAdmin/FirebaseAdmin.Tests --no-build --framework netcoreapp3.1 --configuration $BUILD_CONFIGURATION
dotnet test FirebaseAdmin/FirebaseAdmin.Tests --no-build --framework net6.0 --configuration $BUILD_CONFIGURATION