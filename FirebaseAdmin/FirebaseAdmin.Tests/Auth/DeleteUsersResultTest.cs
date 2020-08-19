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

using System;
using System.Collections.Generic;
using Xunit;

namespace FirebaseAdmin.Auth.Tests
{
    /// <summary>
    /// Represents the result of the
    /// <see cref="AbstractFirebaseAuth.DeleteUsersAsync(IReadOnlyList{string})"/> API.
    /// </summary>
    public sealed class DeleteUsersResultTest
    {
        [Fact]
        public void NullErrors()
        {
            var result = new DeleteUsersResult(2, null);

            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void EmptyErrors()
        {
            var result = new DeleteUsersResult(2, new List<ErrorInfo>());

            Assert.Equal(2, result.SuccessCount);
            Assert.Equal(0, result.FailureCount);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Errors()
        {
            var errors = new List<ErrorInfo>()
            {
                { new ErrorInfo(1, "test") },
            };
            var result = new DeleteUsersResult(2, errors);

            Assert.Equal(1, result.SuccessCount);
            Assert.Equal(1, result.FailureCount);
            var error = Assert.Single(result.Errors);
            Assert.Equal(1, error.Index);
            Assert.Equal("test", error.Reason);
        }

        [Fact]
        public void TooManyErrors()
        {
            var errors = new List<ErrorInfo>()
            {
                { new ErrorInfo(1, "test") },
                { new ErrorInfo(2, "test") },
                { new ErrorInfo(3, "test") },
            };

            var exception = Assert.Throws<ArgumentException>(
                () => new DeleteUsersResult(2, errors));

            Assert.Equal(
                "More errors encountered (3) than users (2). Errors: test, test, test",
                exception.Message);
        }
    }
}
