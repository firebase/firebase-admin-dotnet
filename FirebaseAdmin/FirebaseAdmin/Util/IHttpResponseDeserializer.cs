// Copyright 2019, Google Inc. All rights reserved.
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

namespace FirebaseAdmin.Util
{
    /// <summary>
    /// An interface for deserializing string payloads read from HTTP response.
    /// </summary>
    internal interface IHttpResponseDeserializer
    {
        /// <summary>
        /// Deserializes an HTTP response payload into the specified type.
        /// </summary>
        /// <returns>The deserialized object instance.</returns>
        /// <param name="body">The response payload to deserialize.</param>
        /// <typeparam name="T">Type to deserialize the response into.</typeparam>
        T Deserialize<T>(string body);
    }
}
