// Copyright 2018, Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseAuthTest
    {
        private const string VerifyCustomTokenUrl =
            "https://www.googleapis.com/identitytoolkit/v3/relyingparty/verifyCustomToken";

        public FirebaseAuthTest()
        {
            IntegrationTestUtils.EnsureDefaultApp();
        }

        [Fact]
        public async Task CreateCustomToken()
        {
            var customToken = await FirebaseAuth.DefaultInstance
                .CreateCustomTokenAsync("testuser");
            var idToken = await SignInWithCustomTokenAsync(customToken);
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            Assert.Equal("testuser", decoded.Uid);
        }

        [Fact]
        public async Task CreateCustomTokenWithClaims()
        {
            var developerClaims = new Dictionary<string, object>()
            {
                { "admin", true },
                { "package", "gold" },
                { "magicNumber", 42L },
            };
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(
                "testuser", developerClaims);
            var idToken = await SignInWithCustomTokenAsync(customToken);
            Assert.False(string.IsNullOrEmpty(idToken));
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
            Assert.Equal("testuser", decoded.Uid);
            foreach (var entry in developerClaims)
            {
                object value;
                Assert.True(decoded.Claims.TryGetValue(entry.Key, out value));
                Assert.Equal(entry.Value, value);
            }
        }

        [Fact]
        public async Task CreateCustomTokenWithoutServiceAccount()
        {
            var googleCred = FirebaseApp.DefaultInstance.Options.Credential;
            var serviceAcct = (ServiceAccountCredential)googleCred.UnderlyingCredential;
            var token = await ((ITokenAccess)googleCred).GetAccessTokenForRequestAsync();
            var app = FirebaseApp.Create(
                new AppOptions()
                {
                    Credential = GoogleCredential.FromAccessToken(token),
                    ServiceAccountId = serviceAcct.Id,
                }, "IAMSignApp");
            try
            {
                var customToken = await FirebaseAuth.GetAuth(app).CreateCustomTokenAsync(
                    "testuser");
                var idToken = await SignInWithCustomTokenAsync(customToken);
                var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
                Assert.Equal("testuser", decoded.Uid);
            }
            finally
            {
                app.Delete();
            }
        }

        [Fact]
        public async Task SetCustomUserClaims()
        {
            var customClaims = new Dictionary<string, object>()
            {
                { "admin", true },
            };

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("testuser", customClaims);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithEmptyClaims()
        {
            var customClaims = new Dictionary<string, object>();

            await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("testuser", customClaims);
        }

        [Fact]
        public async Task SetCustomUserClaimsWithWrongUid()
        {
            var customClaims = new Dictionary<string, object>();

            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.SetCustomUserClaimsAsync("mock-uid", customClaims));
        }

        [Fact]
        public async Task UserLifecycle()
        {
            var rand = new Random();
            var uid = $"user{rand.Next()}";
            var customToken = await FirebaseAuth.DefaultInstance.CreateCustomTokenAsync(uid);
            var idToken = await SignInWithCustomTokenAsync(customToken);

            var user = await FirebaseAuth.DefaultInstance.GetUserAsync(uid);
            Assert.Equal(uid, user.Uid);

            await FirebaseAuth.DefaultInstance.DeleteUserAsync(uid);
            await Assert.ThrowsAsync<FirebaseException>(
                async () => await FirebaseAuth.DefaultInstance.GetUserAsync(uid));
        }

        private static async Task<string> SignInWithCustomTokenAsync(string customToken)
        {
            var rb = new Google.Apis.Requests.RequestBuilder()
            {
                Method = Google.Apis.Http.HttpConsts.Post,
                BaseUri = new Uri(VerifyCustomTokenUrl),
            };
            rb.AddParameter(RequestParameterType.Query, "key", IntegrationTestUtils.GetApiKey());
            var request = rb.CreateRequest();
            var jsonSerializer = Google.Apis.Json.NewtonsoftJsonSerializer.Instance;
            var payload = jsonSerializer.Serialize(new SignInRequest
            {
                CustomToken = customToken,
                ReturnSecureToken = true,
            });
            request.Content = new StringContent(payload, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync();
                var parsed = jsonSerializer.Deserialize<SignInResponse>(json);
                return parsed.IdToken;
            }
        }
    }

    internal class SignInRequest
    {
        [Newtonsoft.Json.JsonProperty("token")]
        public string CustomToken { get; set; }

        [Newtonsoft.Json.JsonProperty("returnSecureToken")]
        public bool ReturnSecureToken { get; set; }
    }

    internal class SignInResponse
    {
        [Newtonsoft.Json.JsonProperty("idToken")]
        public string IdToken { get; set; }
    }
}
