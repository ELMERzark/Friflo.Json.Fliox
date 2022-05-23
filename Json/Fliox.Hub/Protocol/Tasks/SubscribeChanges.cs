﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Subscribe to specific <see cref="changes"/> of the specified <see cref="container"/> using the given <see cref="filter"/> 
    /// </summary>
    public sealed class SubscribeChanges : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Fri.Required]  public      string          container;
        /// <summary>subscribe to entity <see cref="changes"/> of the given <see cref="container"/></summary>
        [Fri.Required]  public      List<Change>    changes;
        /// <summary>subscription filter as a <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions">Lambda expression</a> (infix notation)
        /// returning a boolean value. E.g. <c>o.name == 'Smartphone'</c></summary>
                        public      JsonValue       filter;
                        
        [Fri.Ignore]    internal    FilterOperation filterOp;
        
        internal override           TaskType        TaskType  => TaskType.subscribeChanges;
        public   override           string          TaskName  => $"container: '{container}'";

        internal override Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, ExecuteContext executeContext) {
            var hub         = executeContext.Hub;
            var eventBroker = hub.EventBroker;
            if (eventBroker == null)
                return Task.FromResult<SyncTaskResult>(InvalidTask("Hub has no eventBroker"));
            if (container == null)
                return Task.FromResult<SyncTaskResult>(MissingContainer());
            if (changes == null)
                return Task.FromResult<SyncTaskResult>(MissingField(nameof(changes)));
            
            if (!hub.Authenticator.EnsureValidClientId(hub.ClientController, executeContext, out string error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));

            using (var pooled = executeContext.ObjectMapper.Get()) {
                var reader  = pooled.instance.reader;
                filterOp    = reader.Read<FilterOperation>(filter);
                if (reader.Error.ErrSet) {
                    return Task.FromResult<SyncTaskResult>(InvalidTask($"filterTree error: {reader.Error.msg.ToString()}"));
                }
            }
            
            var eventTarget = executeContext.eventTarget;
            if (!eventBroker.SubscribeChanges(this, executeContext.clientId, eventTarget, out error))
                return Task.FromResult<SyncTaskResult>(InvalidTask(error));
            
            return Task.FromResult<SyncTaskResult>(new SubscribeChangesResult());
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="SubscribeChanges"/> task
    /// </summary>
    public sealed class SubscribeChangesResult : SyncTaskResult
    {
        internal override   TaskType    TaskType => TaskType.subscribeChanges;
    }
    
    /// <summary>Contains predefined sets of common database <see cref="Change"/> filters.</summary>
    public static class Changes
    {
        /// <summary>Shortcut to subscribe to all types database changes. These ase <see cref="Change.create"/>,
        /// <see cref="Change.upsert"/>, <see cref="Change.patch"/> and <see cref="Change.delete"/></summary>
        public static readonly Change All  = Change.create | Change.upsert | Change.delete | Change.patch;
        /// <summary>Shortcut to unsubscribe from all database change types.</summary>
        public static readonly Change None = 0;
        
        internal static IReadOnlyList<Change> ToList(Change changeFlags) {
            var list = new List<Change>(4);
            if ((changeFlags & Change.create) != 0) list.Add(Change.create);
            if ((changeFlags & Change.upsert) != 0) list.Add(Change.upsert);
            if ((changeFlags & Change.delete) != 0) list.Add(Change.delete);
            if ((changeFlags & Change.patch)  != 0) list.Add(Change.patch);
            return list;
        }
    }
    
    /// <summary>Filter type used to specify the type of a database change.</summary>
    /// <remarks>
    /// Consider using the predefined sets <see cref="Changes.All"/> or <see cref="Changes.None"/> as shortcuts.
    /// </remarks>
    // ReSharper disable InconsistentNaming
    [Flags]
    public enum Change
    {
        /// <summary>filter change events of created entities.</summary>
        create  = 1,
        /// <summary>filter change events of upserted entities.</summary>
        upsert  = 2,
        /// <summary>filter change events of entity patches.</summary>
        patch   = 4,
        /// <summary>filter change events of deleted entities.</summary>
        delete  = 8
    }
}