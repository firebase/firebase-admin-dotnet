using FirebaseAdmin.Auth;
using System.Threading.Tasks;
using Xunit;

namespace FirebaseAdmin.IntegrationTests
{
    public class FirebaseUserManagementTests
    {
        public FirebaseUserManagementTests()
        {
            IntegrationTestUtils.EnsureDefaultApp();
        }

        [Fact]
        public async Task DeleteUser()
        {
            var uid = "003HlDDEIHdtxlMveFAacPKq9PY2";

            await FirebaseAuth.DefaultInstance
                .DeleteUserAsync(uid);
        }
    }
}
