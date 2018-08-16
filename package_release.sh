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

set -e

if [[ -z "${FrameworkPathOverride}" ]]; then
    echo "[INFO] FrameworkPathOverride not set. Using default."
    FrameworkPathOverride="/usr/lib/mono/4.5/"
fi

rm -rf FirebaseAdmin/FirebaseAdmin/bin/Release
dotnet pack -c Release FirebaseAdmin/FirebaseAdmin /p:OS="Windows_NT" /p:FrameworkPathOverride=${FrameworkPathOverride}
