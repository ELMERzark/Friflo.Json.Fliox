﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Friflo.Json.Flow.Graph.Internal;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph
{
    // ----------------------------------------- QueryTask -----------------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class QueryTask<T> : SyncTask, IReadRefsTask<T> where T : Entity
    {
        internal            TaskState               state;
        internal            RefsTask                refsTask;
        internal readonly   FilterOperation         filter;
        internal readonly   string                  filterLinq; // use as string identifier of a filter 
        internal            Dictionary<string, T>   entities;

        public              Dictionary<string, T>   Results         => IsOk("QueryTask.Result",  out Exception e) ? entities     : throw e;
        public              T                       this[string id] => IsOk("QueryTask[]",       out Exception e) ? entities[id] : throw e;
            
        internal override   TaskState               State          => state;
        internal override   string                  Label           => $"QueryTask<{typeof(T).Name}> filter: {filterLinq}";
        public   override   string                  ToString()      => Label;


        internal QueryTask(FilterOperation filter) {
            refsTask        = new RefsTask(this);
            this.filter     = filter;
            this.filterLinq = filter.Linq;
        }

        public ReadRefsTask<TRef> Read<TRef>(RefsPath<T, TRef> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByPath<TRef>(selector.path);
        }

        public ReadRefsTask<TRef> ReadRefs<TRef>(Expression<Func<T, Ref<TRef>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector);
        }
        
        public ReadRefsTask<TRef> ReadArrayRefs<TRef>(Expression<Func<T, IEnumerable<Ref<TRef>>>> selector) where TRef : Entity {
            if (State.IsSynced())
                throw AlreadySyncedError();
            return refsTask.ReadRefsByExpression<TRef>(selector);
        }
    }
    
    
    public class EntityFilter<T>
    {
        internal readonly FilterOperation op;

        public EntityFilter(Expression<Func<T, bool>> filter) {
            op = Operation.FromFilter(filter, EntitySet.RefQueryPath);
        }
    }    
    
}

