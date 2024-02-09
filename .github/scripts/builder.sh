#!/bin/bash

set -e

BUILD_CONFIGURATION=Release

dotnet restore FirebaseAdmin/FirebaseAdmin.sln
dotnet build FirebaseAdmin/FirebaseAdmin.sln --configuration $BUILD_CONFIGURATION --no-restore
dotnet pack FirebaseAdmin/FirebaseAdmin.sln --configuration $BUILD_CONFIGURATION --no-restore