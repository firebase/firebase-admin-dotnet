using System;
using System.Collections.Generic;
using System.Text;
using FirebaseAdmin.Auth;
using Xunit;

namespace FirebaseAdmin.Tests.Auth
{
    public class UserImportHashTest
    {
        private static readonly byte[] SIGNERKEY = Encoding.ASCII.GetBytes("key");
        private static readonly byte[] SALTSEPARATOR = Encoding.ASCII.GetBytes("separator");

        [Fact]
        public void TestBase()
        {
            var hash = new MockHash();
            Assert.Equal(
                new Dictionary<string, object>
                {
                    { "hashAlgorithm", "MOCK_HASH" },
                    { "key", "value" },
                }, hash.Properties());
        }

        private class MockHash : UserImportHash
        {
            internal MockHash()
                : base("MOCK_HASH") { }

            protected override IReadOnlyDictionary<string, object> Options()
            {
                return new Dictionary<string, object> { { "key", "value" } };
            }
        }
    }
}
