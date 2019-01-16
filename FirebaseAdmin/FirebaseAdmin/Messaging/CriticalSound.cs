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
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    public sealed class CriticalSound
    {
        [JsonIgnore]
        public bool Critical { get; set; }

        [JsonProperty("critical")]
        private int? CriticalInt
        {
            get
            {
                if (Critical)
                {
                    return 1;
                }
                return null;
            }
            set
            {
                Critical = (value == 1);
            }
        }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("volume")]
        public double Volume { get; set; }

        internal CriticalSound CopyAndValidate()
        {
            if (Volume < 0 || Volume > 1)
            {
                throw new ArgumentException("Volume must be in the interval [0, 1]");
            }
            return new CriticalSound()
            {
                Critical = this.Critical,
                Name = this.Name,
                Volume = this.Volume,
            }
        }
    }
}