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

namespace FirebaseAdmin.Auth.Hash.Tests
{
  public class UserImportOptionsTest
  {
    [Fact]
    public void TestUserImportOptionsBasic()
    {
      var options = new UserImportOptions()
      {
        Hash = new Md5 { Rounds = 5 },
      };

      var expectedResult = new Dictionary<string, object>
        {
          { "rounds", "5" },
          { "hashAlgorithm", "MD5" },
        };

      Assert.Equal(expectedResult, options.GetHashProperties());
    }

    [Fact]
    public void TestUserImportOptionsNoHash()
    {
      var options = new UserImportOptions();
      Assert.Throws<ArgumentException>(() => options.GetHashProperties());
    }
  }
}
