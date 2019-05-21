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
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a source of generic data that can be queried to load a batch.
    /// </summary>
    /// <typeparam name="TResource">The resource type contained within the response.</typeparam>
    /// <typeparam name="TResponse">The API response type. Each response contains a page of resources.</typeparam>
    public interface ISource<TResource, TResponse>
    {
        /// <summary>
        /// Returns a list of requested data.
        /// </summary>
        /// <param name="pageSize">The page size. Must be greater than 0.</param>
        /// <param name="pageToken">the next-page-token.</param>
        /// <returns>a list of the requested data.</returns>
        Task<Tuple<string, IEnumerable<TResource>>> Fetch(int pageSize, string pageToken);

        /// <summary>
        /// Returns the raw API response.
        /// </summary>
        /// <param name="pageSize">The page size. Must be greater than 0.</param>
        /// <param name="pageToken">the next-page-token.</param>
        /// <returns>the raw API response.</returns>
        Task<TResponse> FetchRaw(int pageSize, string pageToken);
    }
}