using System.Collections.Generic;
using System.Linq;
using Google.Api.Gax;

namespace FirebaseAdmin.Auth
{
    /// <summary>
    /// Represents <see cref="PagedEnumerable{DownloadAccountResponse, UserRecord}"/> instances. Provides methods for iterating
    /// over the users in the current page, and calling up subsequent pages of users. Instances of
    /// this class are thread-safe and immutable.
    /// </summary>
    internal class UserList : PagedEnumerable<DownloadAccountResponse, UserRecord>
    {
        private readonly ISource<UserRecord, DownloadAccountResponse> dataSource;

        private string nextToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserList"/> class.
        /// </summary>
        /// <param name="dataSource">the data-source from which the users are loaded.</param>
        public UserList(ISource<UserRecord, DownloadAccountResponse> dataSource)
        {
            this.dataSource = dataSource;
        }

        /// <inheritdoc />
        public override IEnumerable<DownloadAccountResponse> AsRawResponses()
        {
            for (DownloadAccountResponse userPages = null; (userPages = this.dataSource.FetchRaw(FirebaseUserManager.MaxListUsersResults, userPages != null ? userPages.NextPageToken : string.Empty).Result) != null;)
            {
                yield return userPages;
            }
        }

        /// <inheritdoc />
        public override Page<UserRecord> ReadPage(int pageSize)
        {
            var fetchedData = this.dataSource.Fetch(pageSize, this.nextToken).Result;
            this.nextToken = fetchedData.Item1;

            return string.IsNullOrEmpty(this.nextToken) ? null : new Page<UserRecord>(fetchedData.Item2, fetchedData.Item1);
        }

        /// <inheritdoc />
        public override IEnumerator<UserRecord> GetEnumerator()
        {
            var users = new List<UserRecord>();

            for (Page<UserRecord> userPages; (userPages = this.ReadPage(FirebaseUserManager.MaxListUsersResults)) != null;)
            {
                users.AddRange(userPages.AsEnumerable());
            }

            return users.GetEnumerator();
        }
    }
}