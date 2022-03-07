# Copyright 2020 Google Inc.
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

mkdir -p FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources

gpg --quiet --batch --yes --decrypt --passphrase=$Env:FIREBASE_SERVICE_ACCT_KEY `
--output FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_cert.json `
.github/resources/integ-service-account.json.gpg

echo $Env:FIREBASE_API_KEY > FirebaseAdmin/FirebaseAdmin.IntegrationTests/resources/integration_apikey.txt

dotnet test FirebaseAdmin/FirebaseAdmin.IntegrationTests
