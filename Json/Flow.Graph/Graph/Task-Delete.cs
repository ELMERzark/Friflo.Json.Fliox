﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Flow.Graph.Internal;

namespace Friflo.Json.Flow.Graph
{
    // todo remove class
    public abstract class DeleteTask : SyncTask {
        internal            TaskState   state;
        internal override   TaskState   State      => state;
        
        internal abstract void GetIds(List<string> ids);
    }
    
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public class DeleteTask<T> : DeleteTask where T : Entity
    {
        private  readonly   ICollection<string> ids;

        internal override   string              Label       => $"DeleteTask<{typeof(T).Name}> #ids: {ids.Count}";
        public   override   string              ToString()  => Label;
        
        internal DeleteTask(ICollection<string> ids) {
            this.ids = ids;
        }

        internal override void GetIds(List<string> ids) {
            ids.AddRange(this.ids);
        }
    }
}