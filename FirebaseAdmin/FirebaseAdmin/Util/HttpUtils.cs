// Copyright 2020, Google Inc. All rights reserved.
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

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Google.Apis.Json;
using Newtonsoft.Json.Linq;

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// A collection of utilities that can be used when making marshaling HTTP requests.
    /// </summary>
    internal sealed class HttpUtils
    {
        internal static readonly HttpMethod Patch = new HttpMethod("PATCH");

        private HttpUtils() { }

        internal static string EncodeQueryParams(IDictionary<string, object> queryParams)
        {
            var queryString = string.Empty;
            if (queryParams != null && queryParams.Count > 0)
            {
                var list = queryParams.OrderBy(kvp => kvp.Key)
                    .Select(kvp => $"{kvp.Key}={kvp.Value}");
                queryString = "?" + string.Join("&", list);
            }

            return queryString;
        }

        internal static IList<string> CreateUpdateMask(object request)
        {
            var json = NewtonsoftJsonSerializer.Instance.Serialize(request);
            var dictionary = JObject.Parse(json);
            var mask = CreateUpdateMask(dictionary);
            mask.Sort();
            return mask;
        }

        private static List<string> CreateUpdateMask(JObject dictionary)
        {
            var mask = new List<string>();
            foreach (var entry in dictionary)
            {
                if (entry.Value.Type == JTokenType.Object)
                {
                    var childMask = CreateUpdateMask((JObject)entry.Value);
                    mask.AddRange(childMask.Select((item) => $"{entry.Key}.{item}"));
                }
                else
                {
                    mask.Add(entry.Key);
                }
            }

            return mask;
        }
    }
}
