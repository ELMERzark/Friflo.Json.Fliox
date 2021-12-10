﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Protocol.Tasks
{
    // ----------------------------------- task -----------------------------------
    public sealed class QueryEntities : SyncRequestTask
    {
        [Fri.Required]  public  string              container;
                        public  string              keyName;
                        public  bool?               isIntKey;
                        public  FilterOperation     filterTree;
                        public  string              filter;
                        public  List<References>    references;
                        
        internal override       TaskType            TaskType => TaskType.query;
        public   override       string              TaskName => $"container: '{container}', filter: {filter}";
        
        public FilterOperation GetFilter() {
            if (filterTree != null)
                return filterTree;
            return Operation.FilterTrue;
        }
        
        private bool ValidateFilter(out TaskErrorResult error) {
            error = null;
            if (filterTree != null)
                return true;
            if (filter == null)
                return true;
            var operation = Operation.Parse(filter, out var parseError);
            if (operation == null) {
                error = InvalidTaskError(parseError);
                return false;
            }
            if (operation is FilterOperation filterOperation) {
                filterTree = filterOperation;
                return true;
            }
            error = InvalidTaskError("filter must be boolean operation");
            return false;
        }
        
        internal override async Task<SyncTaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (!ValidReferences(references, out var error))
                return error;
            if (!ValidateFilter (out error))
                return error;
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.QueryEntities(this, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            var containerResult = response.GetContainerResult(container);
            var entities = result.entities;
            result.entities = null;  // clear -> its not part of protocol
            containerResult.AddEntities(entities);
            var queryRefsResults = new ReadReferencesResult();
            if (references != null && references.Count > 0) {
                queryRefsResults =
                    await entityContainer.ReadReferences(references, entities, container, "", response, messageContext).ConfigureAwait(false);
                // returned queryRefsResults.references is always set. Each references[] item contain either a result or an error.
            }
            result.container    = container;
            result.ids          = entities.Keys.ToHashSet(JsonKey.Equality); // TAG_PERF
            result.references   = queryRefsResults.references;
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public sealed class QueryEntitiesResult : SyncTaskResult, ICommandResult
    {
                        public  string                          container;  // only for debugging ergonomics
        [Fri.Required]  public  HashSet<JsonKey>                ids = new HashSet<JsonKey>(JsonKey.Equality);
                        public  List<ReferencesResult>          references;
        [Fri.Ignore]    public  Dictionary<JsonKey,EntityValue> entities;
                        public  CommandError                    Error { get; set; }

        
        internal override   TaskType            TaskType => TaskType.query;
        public   override   string              ToString() => $"(container: {container})";
    }
}