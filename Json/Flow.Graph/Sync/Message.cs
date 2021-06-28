﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class Message : DatabaseTask
    {
        public              string          text;
            
        internal override   TaskType        TaskType    => TaskType.message;
        public   override   string          ToString()  => text;

        internal override Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            TaskResult result = new MessageResult{text = text};
            return Task.FromResult(result); 
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class MessageResult : TaskResult, ICommandResult
    {
        public              string          text;
        
        public CommandError                 Error { get; set; }

        internal override   TaskType        TaskType => TaskType.message;
    }
}