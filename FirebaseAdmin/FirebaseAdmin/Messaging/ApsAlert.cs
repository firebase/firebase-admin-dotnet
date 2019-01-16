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
using System.Linq;
using Newtonsoft.Json;

namespace FirebaseAdmin.Messaging
{
    public sealed class ApsAlert
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("subtitle")]
        public string Subtitle { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("loc-key")]
        public string LocKey { get; set; }

        [JsonProperty("loc-args")]
        public IEnumerable<string> LocArgs { get; set; }

        [JsonProperty("title-loc-key")]
        public string TitleLocKey { get; set; }

        [JsonProperty("title-loc-args")]
        public IEnumerable<string> TitleLocArgs { get; set; }

        [JsonProperty("subtitle-loc-key")]
        public string SubtitleLocKey { get; set; }

        [JsonProperty("subtitle-loc-args")]
        public IEnumerable<string> SubtitleLocArgs { get; set; }

        [JsonProperty("action-loc-key")]
        public string ActionLocKey { get; set; }

        [JsonProperty("launch-image")]
        public string LaunchImage { get; set; }

        internal ApsAlert CopyAndValidate()
        {
            return new ApsAlert()
            {
                Title = this.Title,
                Subtitle = this.Subtitle,
                Body = this.Body,
                LocKey = this.LocKey,
                LocArgs = this.LocArgs?.ToList(),
                TitleLocKey = this.TitleLocKey,
                TitleLocArgs = this.TitleLocArgs?.ToList(),
                SubtitleLocKey = this.SubtitleLocKey,
                SubtitleLocArgs = this.SubtitleLocArgs?.ToList(),
                ActionLocKey = this.ActionLocKey,
                LaunchImage = this.LaunchImage,
            };
        }
    }
}