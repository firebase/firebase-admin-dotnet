using System.Collections.Generic;
using Google.Api.Gax.Rest;

namespace FirebaseAdmin.Auth
{
    internal class UserRecordPageManager : IPageManager<UserRecordServiceRequest, DownloadAccountResponse, UserRecord>
    {
        public void SetPageSize(UserRecordServiceRequest request, int pageSize)
        {
            request.SetPageSize(pageSize);
        }

        public void SetPageToken(UserRecordServiceRequest request, string pageToken)
        {
            request.SetPageToken(pageToken);
        }

        public IEnumerable<UserRecord> GetResources(DownloadAccountResponse response)
        {
            if (response?.Users == null)
            {
                yield break;
            }

            foreach (var user in response.Users)
            {
                yield return new UserRecord(user);
            }
        }

        public string GetNextPageToken(DownloadAccountResponse response)
        {
            return string.IsNullOrEmpty(response.NextPageToken) ? null : response.NextPageToken;
        }
    }
}