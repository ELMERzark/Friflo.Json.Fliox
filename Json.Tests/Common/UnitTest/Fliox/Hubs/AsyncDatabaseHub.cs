﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    /// <summary>
    /// A <see cref="FlioxHub"/> implementation which execute the continuation of <see cref="ExecuteSync"/>
    /// never synchronously to test <see cref="FlioxClient.SyncTasks"/> running not synchronously.
    /// </summary>
    public class AsyncDatabaseHub : FlioxHub
    {
        private readonly    FlioxHub  local;

        public AsyncDatabaseHub(EntityDatabase database, SharedEnv env, string hostName = null) : base(database, env, hostName) {
            local = new FlioxHub(database, env);
        }
        
        public override async Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            const bool originalContext = true;
            // force release the thread back to the caller so continuation will not be executed synchronously.
            await Task.Delay(1).ConfigureAwait(originalContext);
            var response = await local.ExecuteSync(syncRequest, syncContext).ConfigureAwait(false);
            return response;
        }
    }
}
