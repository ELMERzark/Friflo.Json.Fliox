﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    public class SubscribeTask<T> : SyncTask where T : Entity
    {
        internal            TaskState               state;
        internal readonly   HashSet<Change>         types;
        internal readonly   FilterOperation         filter;
        private  readonly   string                  filterLinq; // use as string identifier of a filter
            
        internal override   TaskState               State           => state;
        public   override   string                  Details         => $"SubscribeTask<{typeof(T).Name}> (filter: {filterLinq})";
        

        internal SubscribeTask(HashSet<Change> types, FilterOperation filter) {
            this.types      = types ?? new HashSet<Change>();
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }
    }
}