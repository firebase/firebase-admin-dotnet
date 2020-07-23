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
using Xunit.Abstractions;
using Xunit.Sdk;

namespace FirebaseAdmin.IntegrationTests
{
    /// <summary>
    /// Use this test case orderer to order the execution of test cases by their
    /// <see cref="TestRankAttribute"/>. Test cases with the same rank are ordered by their name.
    /// Based on the example provided in the
    /// <a href="https://docs.microsoft.com/en-us/dotnet/core/testing/order-unit-tests?pivots=xunit">
    /// Microsoft documentation</a>.
    /// </summary>
    public class TestRankOrderer : ITestCaseOrderer
    {
        public IEnumerable<TTestCase> OrderTestCases<TTestCase>(IEnumerable<TTestCase> testCases)
        where TTestCase : ITestCase
        {
            string testRankAttribute = typeof(TestRankAttribute).AssemblyQualifiedName;
            var rankedMethods = new SortedDictionary<int, List<ITestCase>>();
            foreach (var testCase in testCases)
            {
                int rank = testCase.TestMethod.Method.GetCustomAttributes(testRankAttribute)
                    .FirstOrDefault()
                    ?.GetNamedArgument<int>(nameof(TestRankAttribute.Rank)) ?? 0;
                GetOrCreate(rankedMethods, rank).Add(testCase);
            }

            var orderedTests = rankedMethods.Keys.SelectMany((rank) =>
                rankedMethods[rank].OrderBy((testCase) => testCase.TestMethod.Method.Name));
            foreach (TTestCase testCase in orderedTests)
            {
                yield return testCase;
            }
        }

        private static List<ITestCase> GetOrCreate(
            IDictionary<int, List<ITestCase>> dictionary, int key)
        {
            return dictionary.TryGetValue(key, out List<ITestCase> result)
                ? result
                : (dictionary[key] = new List<ITestCase>());
        }
    }
}
