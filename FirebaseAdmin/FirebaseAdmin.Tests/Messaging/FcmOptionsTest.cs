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

using System;
using FirebaseAdmin.Messaging;
using FirebaseAdmin.Messaging.Util;
using Xunit;

namespace FirebaseAdmin.Tests.Messaging
{
    public class FcmOptionsTest
    {
        [Fact]
        public void FcmOptionsCopyAndValidate()
        {
            var options = new FcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void ApnsFcmOptionsCopyAndValidate()
        {
            var options = new ApnsFcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void AndroidFcmOptionsCopyAndValidate()
        {
            var options = new AndroidFcmOptions() { AnalyticsLabel = "label" };
            var result = options.CopyAndValidate();
            Assert.Equal(options.AnalyticsLabel, result.AnalyticsLabel);
        }

        [Fact]
        public void FcmOptionsCopyAndValidateNullLabel()
        {
            var options = new FcmOptions() { AnalyticsLabel = null };
            options.CopyAndValidate();
        }

        [Fact]
        public void FcmOptionsCopyAndValidateEmptyLabel()
        {
            var options = new FcmOptions() { AnalyticsLabel = string.Empty };
            Assert.Throws<ArgumentException>(() => options.CopyAndValidate());
        }

        [Fact]
        public void AnalyticsLabelTooLong()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel(new string('a', 51)));
        }

        [Fact]
        public void AnalyticsLabelEmtpty()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel(string.Empty));
        }

        [Fact]
        public void AnalyticsLabelInvalidCharacters()
        {
            Assert.Throws<ArgumentException>(() => AnalyticsLabelChecker.ValidateAnalyticsLabel("label(label)"));
        }
    }
}
