﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.EntityGraph.Internal;

namespace Friflo.Json.EntityGraph
{
    // ----------------------------------------- ReadTask -----------------------------------------
    public class ReadTask<T> : RefsTask<T> where T : Entity
    {
        private  readonly   string          id;
        internal readonly   PeerEntity<T>   peer;
        internal            T               result;

        public              T               Result      => synced ? result : throw RequiresSyncError();
        public   override   string          ToString()  => id;
        public   override   string          Label       => $"ReadTask<{typeof(T).Name}> id: {id}";

        internal ReadTask(string id, PeerEntity<T> peer) {
            this.id     = id;
            this.peer   = peer;
        }

        private Exception RequiresSyncError() {
            return new TaskNotSyncedException($"ReadTask.Result requires Sync(). ReadTask<{typeof(T).Name}> id: {id}");
        }

        // lab - ReadRefs by Entity Type
        public SubRefsTask<TValue> ReadRefsOfType<TValue>() where TValue : Entity {
            throw new NotImplementedException("ReadRefsOfType() planned to be implemented");
        }
        
        // lab - all ReadRefs
        public SubRefsTask<Entity> ReadAllRefs()
        {
            throw new NotImplementedException("ReadAllRefs() planned to be implemented");
        }
    }
}
