﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Friflo.Json.Fliox.Hub.Host;

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
        [Required]  public      TaskErrorType   type;
        /// <summary>task error details</summary>
                    public      string          message;
        /// <summary>stacktrace in case the error <see cref="type"/> is a <see cref="TaskErrorType.UnhandledException"/></summary>
                    public      string          stacktrace;

        internal override       TaskType        TaskType => TaskType.error;
        public   override       string          ToString() => $"type: {type}, message: {message}";
        
        public TaskErrorResult() {}
        public TaskErrorResult(TaskErrorType type, string message, string stacktrace = null) {
            this.type       = type;
            this.message    = message;
            this.stacktrace = stacktrace;
        }
    }
    
    /// <summary>Type of a task error used in <see cref="TaskErrorResult"/></summary>
    public enum TaskErrorType {
        /// HTTP status: 500
        None                = 0,
        /// <summary>
        /// Unhandled exception while executing a task.<br/>
        /// maps to HTTP status: 500
        /// </summary>
        /// <remarks>
        /// Unhandled exceptions in a <see cref="EntityContainer"/> implementations need to be fixed.<br/>
        /// More information at <see cref="FlioxHub.ExecuteRequestAsync"/>.
        /// </remarks>
        UnhandledException  = 1,
        /// <summary>General database error while task execution.<br/>
        /// E.g. the access is currently not available or accessing a missing table.<br/>
        /// maps to HTTP status: 500 
        /// </summary>
        DatabaseError       = 2,
        /// <summary>Invalid query filter   <br/> maps to HTTP status: 400</summary>
        FilterError         = 3,
        /// <summary>Schema validation of an entity failed  <br/> maps to HTTP status: 400</summary>
        ValidationError     = 4,
        /// <summary>Execution of message / command failed caused by invalid input  <br/>maps to HTTP status: 400</summary>
        CommandError        = 5,
        /// <summary>Invalid task. E.g. by using an invalid task parameter  <br/>maps to HTTP status: 400</summary>
        InvalidTask         = 6,
        /// <summary>database message / command not implemented     <br/>maps to HTTP status: 501</summary>
        NotImplemented      = 7,
        /// <summary>task execution not authorized  <br/>maps to HTTP status: 403</summary>
        PermissionDenied    = 8,
        /// <summary>The entire <see cref="SyncRequest"/> containing a task failed  <br/>maps to HTTP status: 500</summary>
        SyncError           = 9,
        
        // ------------------------------- client specific errors -------------------------------
        /// <summary>
        /// It is set for a <see cref="Client.SyncTask"/> if a <see cref="SyncResponse"/> contains errors in its
        /// <see cref="Dictionary{TKey,TValue}"/> fields containing <see cref="EntityErrors"/> for entities accessed via a CRUD
        /// command by the <see cref="Client.SyncTask"/>.
        /// The entity errors are available via <see cref="Client.TaskError.entityErrors"/>.  
        /// No mapping to a <see cref="TaskErrorType"/> value.
        /// </summary>
        EntityErrors        = 10,
        /// <summary> Use to indicate an invalid response.</summary>
        /// <remarks>No mapping to a <see cref="TaskErrorType"/> value.</remarks>
        InvalidResponse     = 11,
    }
}