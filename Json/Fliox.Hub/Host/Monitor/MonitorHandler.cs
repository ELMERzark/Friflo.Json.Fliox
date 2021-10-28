// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;


namespace Friflo.Json.Fliox.Hub.Host.Monitor
{
    internal sealed class MonitorHandler : TaskHandler
    {
        private readonly   FlioxHub            hub;
        
        internal MonitorHandler (FlioxHub hub) {
            this.hub = hub;

            AddCommandHandler<ClearStats, ClearStatsResult>(nameof(ClearStats), ClearStats); // todo add handler via scanning DatabaseHandler
        }
        
        private ClearStatsResult ClearStats(Command<ClearStats> command) {
            // clear request counts of the hub. Extension databases share the same hub.
            hub.Authenticator.ClearUserStats();
            hub.ClientController.ClearClientStats();
            hub.hostStats.ClearHostStats();
            return new ClearStatsResult();
        }
        
        public override Task<SyncTaskResult> ExecuteTask (SyncRequestTask task, EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            var monitorDB = (MonitorDatabase)database;
            switch (task.TaskType) {
                case TaskType.command:
                    return base.ExecuteTask(task, database, response, messageContext);
                case TaskType.read:
                case TaskType.query:
                    return base.ExecuteTask(task, monitorDB.stateDB, response, messageContext);
                default:
                    SyncTaskResult result = SyncRequestTask.InvalidTask ($"MonitorDatabase does not support task: '{task.TaskType}'");
                    return Task.FromResult(result);
            }
        }
    }
}