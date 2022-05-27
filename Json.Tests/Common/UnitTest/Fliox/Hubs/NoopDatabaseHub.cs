// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Hubs
{
    public class NoopDatabaseHub : FlioxHub
    {
        internal NoopDatabaseHub (string databaseName, SharedEnv env, string hostName = null)
            : base(new NoopDatabase(databaseName), env, hostName)
        { }
                
        public override Task<ExecuteSyncResult> ExecuteSync(SyncRequest syncRequest, SyncContext syncContext) {
            var result = new SyncResponse {
                tasks       = new List<SyncTaskResult>(),
                resultMap   = new Dictionary<string, ContainerEntities>()
            };
            var response = new ExecuteSyncResult(result);
            return Task.FromResult(response);
        }
    }
    
    internal class NoopDatabase : EntityDatabase
    {
        internal NoopDatabase(string databaseName) : base(databaseName, null, null) { }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            throw new InvalidOperationException("NoopDatabase cannot create a container");
        }
    }
}