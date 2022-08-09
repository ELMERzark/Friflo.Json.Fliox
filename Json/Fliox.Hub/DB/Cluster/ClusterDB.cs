// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Schema.Native;

namespace Friflo.Json.Fliox.Hub.DB.Cluster
{
    /// <summary>
    /// <see cref="ClusterDB"/> store information about databases hosted by the Hub: <br/>
    /// - available containers aka tables per database <br/>
    /// - available commands per database <br/>
    /// - the schema assigned to each database
    /// </summary>
    public sealed class ClusterDB : EntityDatabase
    {
        // --- private / internal
        internal readonly   EntityDatabase      stateDB;
        private  readonly   FlioxHub            clusterHub;
        private  readonly   FlioxHub            hub;

        public   override   string              StorageType => stateDB.StorageType;

        public ClusterDB (string dbName, FlioxHub hub, DbOpt opt = null)
            : base (dbName, new ClusterHandler(), opt)
        {
            this.hub        = hub  ?? throw new ArgumentNullException(nameof(hub));
            var typeSchema  = NativeTypeSchema.Create(typeof(ClusterStore));
            Schema          = new DatabaseSchema(typeSchema);
            stateDB         = new MemoryDatabase(dbName, null, MemoryType.NonConcurrent);
            clusterHub      = new FlioxHub(stateDB, hub.sharedEnv);
        }

        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return stateDB.CreateContainer(name, database);
        }

        public override async Task ExecuteSyncPrepare(SyncRequest syncRequest, SyncContext syncContext) {
            var pool = syncContext.pool;
            using (var pooled  = pool.Type(() => new ClusterStore(clusterHub)).Get()) {
                var cluster = pooled.instance;
                var tasks = syncRequest.tasks;
                await cluster.UpdateClusterDB  (hub, tasks).ConfigureAwait(false);
                
                await cluster.SyncTasks().ConfigureAwait(false);
            }
        }
        
        internal static bool FindTask(string container, JsonKey dbKey, List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                if (task is ReadEntities read && read.container == container) {
                    return read.ids.Contains(dbKey, JsonKey.Equality);
                }
                if (task is QueryEntities query && query.container == container)
                    return true;
            }
            return false;
        }
    }
    
    public partial class ClusterStore
    {
        internal async Task UpdateClusterDB(FlioxHub hub, List<SyncRequestTask> tasks) {
            var hubDbs = hub.GetDatabases();
            foreach (var pair in hubDbs) {
                var database        = pair.Value;
                var databaseName    = pair.Key;
                var dbKey           = new JsonKey(databaseName);
                if (ClusterDB.FindTask(nameof(containers), dbKey, tasks)) {
                    var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
                    dbContainers.id     = databaseName;
                    containers.Upsert(dbContainers);
                }
                if (ClusterDB.FindTask(nameof(messages), dbKey, tasks)) {
                    var dbMessages  = database.GetDbMessages();
                    dbMessages.id   = databaseName;
                    messages.Upsert(dbMessages);
                }
                if (ClusterDB.FindTask(nameof(schemas),dbKey, tasks)) {
                    var schema = CreateDbSchema(database, databaseName);
                    if (schema != null)
                        schemas.Upsert(schema);
                }
            }
        }
        
        internal static DbSchema CreateDbSchema (EntityDatabase database, string databaseName) {
            var databaseSchema = database.Schema;
            if (databaseSchema == null)
                return null;
            var jsonSchemas = databaseSchema.GetJsonSchemas();
            jsonSchemas.Remove("openapi.json");
            var schema = new DbSchema {
                id          = databaseName,
                schemaName  = databaseSchema.Name,
                schemaPath  = databaseSchema.Path,
                jsonSchemas = jsonSchemas
            };
            return schema;
        }
        
        internal static async Task<HostCluster> GetDbList (MessageContext context) {
            var authorizedDatabases = Helper.CreateHashSet(4, AuthorizeDatabaseComparer.Instance);
            var authorizer          = context.SyncContext.authState.authorizer;
            authorizer.AddAuthorizedDatabases(authorizedDatabases);
            var hub             = context.Hub;
            var databases       = hub.GetDatabases();
            var databaseList    = new List<DbContainers>(databases.Count);
            foreach (var pair in databases) {
                var databaseName    = pair.Key;
                if (!AuthorizeDatabase.IsAuthorizedDatabase(authorizedDatabases, databaseName))
                    continue;
                var database        = pair.Value;
                var dbContainers    = await database.GetDbContainers().ConfigureAwait(false);
                dbContainers.id     = databaseName;
                databaseList.Add(dbContainers);
            }
            return new HostCluster{ databases = databaseList };
        }
    }
}
