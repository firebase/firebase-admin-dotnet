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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents a page of <see cref="PaginatedList{T}"/> instances. Provides methods for iterating
    /// over the users in the current page, and calling up subsequent pages of users.Instances of
    /// this class are thread-safe and immutable.
    /// </summary>
    /// <typeparam name="T">the paginated type.</typeparam>
    public class PaginatedList<T>
    {
        private readonly ISource<T> datasource;

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedList{T}"/> class.
        /// </summary>
        /// <param name="pageSize">the page-size.</param>
        /// <param name="nextToken">the next token.</param>
        /// <param name="datasource">the datasource.</param>
        /// <param name="items">the items.</param>
        public PaginatedList(int pageSize, string nextToken, ISource<T> datasource, List<T> items = default)
        {
            this.datasource = datasource;
            this.PageSize = pageSize;
            this.NextToken = nextToken;
            this.Items = items;
        }

        /// <summary>
        /// Gets the items.
        /// </summary>
        public List<T> Items { get; private set; }

        /// <summary>
        /// Gets the pagesize.
        /// </summary>
        public int PageSize { get; private set; }

        /// <summary>
        /// Gets the next token.
        /// </summary>
        public string NextToken { get; private set; }

        /// <summary>
        /// Creates a new paginated list of the given type and loads the first page.
        /// </summary>
        /// <param name="pageSize">the page-size.</param>
        /// <param name="datasource">the datasource.</param>
        /// <returns>The initiated first page.</returns>
        public static async Task<PaginatedList<T>> CreateAndLoad(int pageSize, ISource<T> datasource)
        {
            return await new PaginatedList<T>(pageSize, string.Empty, datasource).GetNextPage();
        }

        /// <summary>
        /// Returns the next page of the given type.
        /// </summary>
        /// <returns>A new <see cref="PaginatedList{T}"/> instance, or null if there are no more pages.</returns>
        public async Task<PaginatedList<T>> GetNextPage()
        {
            var fetchedData = await this.datasource.Fetch(this.PageSize, this.NextToken);
            return string.IsNullOrEmpty(fetchedData.Item1) ? null : new PaginatedList<T>(this.PageSize, fetchedData.Item1, this.datasource, fetchedData.Item2.ToList());
        }
    }
}