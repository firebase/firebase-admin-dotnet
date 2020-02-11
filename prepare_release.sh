# Copyright 2018 Google Inc.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

#!/bin/bash

function isNewerVersion {
    parseVersion "$1"
    ARG_MAJOR=$MAJOR_VERSION
    ARG_MINOR=$MINOR_VERSION
    ARG_PATCH=$PATCH_VERSION

    parseVersion "$2"
    if [ "$ARG_MAJOR" -ne "$MAJOR_VERSION" ]; then
        if [ "$ARG_MAJOR" -lt "$MAJOR_VERSION" ]; then return 1; else return 0; fi;
    fi
    if [ "$ARG_MINOR" -ne "$MINOR_VERSION" ]; then
        if [ "$ARG_MINOR" -lt "$MINOR_VERSION" ]; then return 1; else return 0; fi;
    fi
    if [ "$ARG_PATCH" -ne "$PATCH_VERSION" ]; then
        if [ "$ARG_PATCH" -lt "$PATCH_VERSION" ]; then return 1; else return 0; fi;
    fi
    # The build numbers are equal
    return 1
}

function parseVersion {
    if [[ ! "$1" =~ ^([0-9]*)\.([0-9]*)\.([0-9]*)$ ]]; then
        return 1
    fi
    MAJOR_VERSION=$(echo "$1" | sed -e 's/^\([0-9]*\)\.\([0-9]*\)\.\([0-9]*\)$/\1/')
    MINOR_VERSION=$(echo "$1" | sed -e 's/^\([0-9]*\)\.\([0-9]*\)\.\([0-9]*\)$/\2/')
    PATCH_VERSION=$(echo "$1" | sed -e 's/^\([0-9]*\)\.\([0-9]*\)\.\([0-9]*\)$/\3/')
    return 0
}

set -e

if [[ -z "$1" ]]; then
    echo "[ERROR] No version number provided."
    echo "[INFO] Usage: ./prepare_release.sh <VERSION_NUMBER>"
    exit 1
fi

CURRENT_DIR=$(pwd)
SLN_FILE="${CURRENT_DIR}/FirebaseAdmin/FirebaseAdmin.sln"
if [[ ! -f "${SLN_FILE}" ]]; then
    echo "[ERROR] Prepare script must be executed from the root of the project."
    exit 1
fi

#############################
#  VALIDATE VERSION NUMBER  #
#############################

VERSION="$1"
if ! parseVersion "$VERSION"; then
    echo "[ERROR] Illegal version number provided. Version number must match semver."
    exit 1
fi

PROJECT_FILE="FirebaseAdmin/FirebaseAdmin/FirebaseAdmin.csproj"
CUR_VERSION=$(grep "<Version>" ${PROJECT_FILE} | awk -F '>' '{print $2}' | awk -F '<' '{print $1}')
if [ -z "$CUR_VERSION" ]; then
    echo "[ERROR] Failed to find the current version. Check ${PROJECT_FILE} for version declaration."
    exit 1
fi
if ! parseVersion "$CUR_VERSION"; then
    echo "[ERROR] Illegal current version number. Version number must match semver."
    exit 1
fi

if ! isNewerVersion "$VERSION" "$CUR_VERSION"; then
    echo "[ERROR] Illegal version number provided. Version $VERSION <= $CUR_VERSION"
    exit 1
fi


#############################
#  VALIDATE TEST RESOURCES  #
#############################

INTEGRATION_TESTS_DIR="FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources"
if [[ ! -e "${INTEGRATION_TESTS_DIR}/integration_cert.json" ]]; then
    echo "[ERROR] integration_cert.json file is required to run integration tests."
    exit 1
fi

if [[ ! -e "${INTEGRATION_TESTS_DIR}/integration_apikey.txt" ]]; then
    echo "[ERROR] integration_apikey.txt file is required to run integration tests."
    exit 1
fi


###################
#  VALIDATE REPO  #
###################

# Ensure the checked out branch is master
CHECKED_OUT_BRANCH="$(git branch | grep "*" | awk -F ' ' '{print $2}')"
if [[ $CHECKED_OUT_BRANCH != "master" ]]; then
    read -p "[WARN] You are on the '${CHECKED_OUT_BRANCH}' branch, not 'master'. Continue? (y/N) " CONTINUE
    case $CONTINUE in
        y|Y) ;;
        *) echo "[INFO] You chose not to continue." ;
           exit 1 ;;
    esac
fi

# Ensure the branch does not have local changes
if [[ $(git status --porcelain) ]]; then
    read -p "[WARN] Local changes exist in the repo. Continue? (y/N) " CONTINUE
    case $CONTINUE in
        y|Y) ;;
        *) echo "[INFO] You chose not to continue." ;
           exit 1 ;;
    esac
fi


####################
#  UPDATE VERSION  #
####################

HOST=$(uname)
echo "[INFO] Updating FirebaseAdmin.csproj"
sed -i -e "s/<Version>$CUR_VERSION<\/Version>/<Version>$VERSION<\/Version>/" "${PROJECT_FILE}"


##################
#  LAUNCH TESTS  #
##################

dotnet clean FirebaseAdmin
echo "[INFO] Building project"
dotnet build FirebaseAdmin/FirebaseAdmin

echo "[INFO] Running unit tests"
dotnet test FirebaseAdmin/FirebaseAdmin.Tests

echo "[INFO] Running integration tests"
dotnet test FirebaseAdmin/FirebaseAdmin.IntegrationTests

echo "[INFO] This repo has been prepared for a release. Create a branch and commit the changes."
