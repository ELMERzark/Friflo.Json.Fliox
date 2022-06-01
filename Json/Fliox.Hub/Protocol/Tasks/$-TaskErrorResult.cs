﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using Friflo.Json.Fliox.Hub.Host;
using Req = Friflo.Json.Fliox.RequiredFieldAttribute;
// ReSharper disable FieldCanBeMadeReadOnly.Global
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// A <see cref="TaskErrorResult"/> is returned in case execution of a <see cref="SyncRequestTask"/> failed
    /// </summary>
    public sealed class TaskErrorResult : SyncTaskResult
    {
        /// <summary>task error type</summary>
        [Req]   public      TaskErrorResultType type;
        /// <summary>task error details</summary>
                public      string              message;
        /// <summary>stacktrace in case the error <see cref="type"/> is a <see cref="TaskErrorResultType.UnhandledException"/></summary>
                public      string              stacktrace;

        internal override   TaskType            TaskType => TaskType.error;
        public   override   string              ToString() => $"type: {type}, message: {message}";
        
        public TaskErrorResult() {}
        public TaskErrorResult(TaskErrorResultType type, string message, string stacktrace = null) {
            this.type       = type;
            this.message    = message;
            this.stacktrace = stacktrace;
        }
    }
    
    /// <summary>Type of a task error used in <see cref="TaskErrorResult"/></summary>
    public enum TaskErrorResultType {
        /// HTTP status: 500
        None,
        /// <summary>
        /// Unhandled exception while executing a task.<br/>
        /// maps to HTTP status: 500
        /// </summary>
        /// <remarks>
        /// Unhandled exceptions in a <see cref="EntityContainer"/> implementations need to be fixed.<br/>
        /// More information at <see cref="FlioxHub.ExecuteSync"/>.
        /// </remarks>
        UnhandledException,
        /// <summary>General database error while task execution.<br/>
        /// E.g. the access is currently not available or accessing a missing table.<br/>
        /// maps to HTTP status: 500 
        /// </summary>
        DatabaseError,
        /// <summary>Invalid query filter   <br/> maps to HTTP status: 400</summary>
        FilterError,
        /// <summary>Schema validation of an entity failed  <br/> maps to HTTP status: 400</summary>
        ValidationError,
        /// <summary>Execution of message / command failed caused by invalid input  <br/>maps to HTTP status: 400</summary>
        CommandError,
        /// <summary>Invalid task. E.g. by using an invalid task parameter  <br/>maps to HTTP status: 400</summary>
        InvalidTask,
        /// <summary>database message / command not implemented     <br/>maps to HTTP status: 501</summary>
        NotImplemented,
        /// <summary>task execution not authorized  <br/>maps to HTTP status: 403</summary>
        PermissionDenied,
        /// <summary>The entire <see cref="SyncRequest"/> containing a task failed  <br/>maps to HTTP status: 500</summary>
        SyncError
    }
}