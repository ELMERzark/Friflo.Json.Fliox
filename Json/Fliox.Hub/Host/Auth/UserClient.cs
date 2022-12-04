// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Burst.Utils;
using Friflo.Json.Fliox.Hub.DB.Cluster;

namespace Friflo.Json.Fliox.Hub.Host.Auth
{
    internal readonly struct UserClient {
        internal readonly   JsonKey                                 userId;
        /// <b>Note</b> requires lock when accessing. Did not use ConcurrentDictionary to avoid heap allocation
        internal readonly   Dictionary<SmallString, RequestCount>   requestCounts;
        
        public   override   string                                  ToString() => userId.AsString();

        internal UserClient (in JsonKey userId) {
            requestCounts   = new Dictionary<SmallString, RequestCount>(SmallString.Equality);
            this.userId     = userId;
        }
    }
}