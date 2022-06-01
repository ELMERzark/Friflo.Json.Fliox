﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;
using Req = Friflo.Json.Fliox.RequiredMemberAttribute;
using Ignore = Friflo.Json.Fliox.IgnoreMemberAttribute;

// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    /// <summary>
    /// Aggregate - count - entities from the given <see cref="container"/> using a <see cref="filter"/><br/> 
    /// </summary>
    public sealed class AggregateEntities : SyncRequestTask
    {
        /// <summary>container name</summary>
        [Req]       public      string              container;
        /// <summary>aggregation type - e.g. count </summary>
                    public      AggregateType       type;
        /// <summary>aggregation filter as JSON tree. <br/>
        /// Is used in favour of <see cref="filter"/> as its serialization is more performant</summary>
                    public      JsonValue           filterTree;
        /// <summary>aggregation filter as a <a href="https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/lambda-expressions">Lambda expression</a> (infix notation)
        /// returning a boolean value. E.g. <c>o.name == 'Smartphone'</c><br/>
        /// </summary>
                    public      string              filter;
                        
        [Ignore]    private     FilterOperation     filterLambda;
        [Ignore]    public      OperationContext    filterContext;
                        
        internal override       TaskType            TaskType => TaskType.aggregate;
        public   override       string              TaskName => $"container: '{container}',type: {type}, filter: {filter}";
        
        public FilterOperation GetFilter() {
            if (filterLambda != null)
                return filterLambda;
            return Operation.FilterTrue;
        }

        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, SyncContext syncContext) {
            if (container == null)
                return MissingContainer();
            if (!QueryEntities.ValidateFilter (filterTree, filter, syncContext, ref filterLambda, out var error))
                return error;
            filterContext = new OperationContext();
            if (!filterContext.Init(GetFilter(), out var message)) {
                return InvalidTaskError($"invalid filter: {message}");
            }
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.AggregateEntities(this, syncContext).ConfigureAwait(false);
            
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    /// <summary>
    /// Result of a <see cref="AggregateEntities"/> task
    /// </summary>
    public sealed class AggregateEntitiesResult : SyncTaskResult, ICommandResult
    {
        /// <summary>container name - not utilized by Protocol</summary>
        [DebugInfo]     public  string          container;
                        public  double?         value;      // set if not using groupBy
        [Ignore]        public  CommandError    Error { get; set; }

        
        internal override   TaskType            TaskType => TaskType.aggregate;
        public   override   string              ToString() => $"(container: {container})";
    }
    
    /// <summary>
    /// Aggregation type used in <see cref="AggregateEntities"/>
    /// </summary>
    public enum AggregateType {
        /// <summary>count entities</summary>
        count
    }
}