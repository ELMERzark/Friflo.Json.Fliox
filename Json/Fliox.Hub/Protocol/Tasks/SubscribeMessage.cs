﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Subscribe to commands and messages sent to a database by their <see cref="name"/><br/>
    /// Unsubscribe by setting <see cref="remove"/> to true 
    /// </summary>
    public sealed class SubscribeMessage : SyncRequestTask
    {
        /// <summary>subscribe a specific message: 'std.Echo', multiple messages by prefix: 'std.*', all messages: '*'</summary>
        [Required]  public  string      name;
        /// <summary>if true a previous added subscription is removed. Otherwise added</summary>
                    public  bool?       remove;
        
        internal override   TaskType    TaskType    => TaskType.subscribeMessage;
        public   override   string      TaskName    => $"name: '{name}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            var hub             = syncContext.Hub;
            var eventDispatcher = hub.EventDispatcher;
            if (eventDispatcher == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("Hub has no EventDispatcher"));
            if (name == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(name)));
            
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, syncContext, out string error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));
            
            var eventReceiver   = syncContext.eventReceiver;
            var eventAck        = syncContext.eventAck ?? 0;
            var user            = syncContext.User;
            if (!eventDispatcher.SubscribeMessage(database.name, this, user, syncContext.clientId, eventAck, eventReceiver, out error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));
            
            return Task.FromResult<SyncTaskResult>(new SubscribeMessageResult());
        }
        
        internal static string GetPrefix (string name) {
            return name.EndsWith("*") ? name.Substring(0, name.Length - 1) : null;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SubscribeMessage"/> task
    /// </summary>
    public sealed class SubscribeMessageResult : SyncTaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeMessage;
    }
}