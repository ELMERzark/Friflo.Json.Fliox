// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using Friflo.Json.Fliox.Hub.Protocol;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary> Defines how to execute a <see cref="SyncRequest"/> </summary>
    /// <remarks>
    /// It is used to
    /// <list type="bullet">
    ///   <item>
    ///     Execute a <see cref="SyncRequest"/> with a synchronous call if possible to avoid heap allocation
    ///     and CPU costs required for asynchronous methods if possible.<br/>
    ///     Also the stacktrace of synchronous calls are smaller - one level instead of three for async calls -
    ///     are easier to read. 
    ///   </item>
    ///   <item>
    ///     Enable queued execution of <see cref="SyncRequest"/>. See <see cref="DatabaseService.queue"/>
    ///   </item>
    /// </list>
    /// </remarks>
    public enum ExecutionType {
        None    = 0,
        /// <summary>execute request error synchronous with <see cref="FlioxHub.ExecuteRequest"/></summary>
        Error   = 1,
        /// <summary>execute request synchronous with <see cref="FlioxHub.ExecuteRequest"/></summary>
        Sync    = 2,
        /// <summary>execute request asynchronous with <see cref="FlioxHub.ExecuteRequestAsync"/></summary>
        Async   = 3,
        /// <summary>queue request execution with <see cref="FlioxHub.QueueRequestAsync"/></summary>
        Queue   = 4,
    }
}