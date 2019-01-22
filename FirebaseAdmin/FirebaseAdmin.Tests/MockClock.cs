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
using Google.Apis.Util;

namespace FirebaseAdmin.Tests
{
    public class MockClock : IClock
    {
        private object mutex = new object();
        private DateTime utcNow;

        public MockClock()
        {
            this.Now = DateTime.Now;
        }

        public DateTime Now
        {
            get { return this.UtcNow.ToLocalTime(); }
            set { this.UtcNow = value.ToUniversalTime(); }
        }

        public DateTime UtcNow
        {
            get
            {
                lock (this.mutex)
                {
                    return this.utcNow;
                }
            }

            set
            {
                lock (this.mutex)
                {
                    this.utcNow = value;
                }
            }
        }
    }
}
