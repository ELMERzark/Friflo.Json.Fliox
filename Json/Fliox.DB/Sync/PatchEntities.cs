﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.NoSQL;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- task -----------------------------------
    public class PatchEntities : DatabaseTask
    {
        [Fri.Required]  public  string              container;
                        public  string              keyName;
        [Fri.Required]  public  List<EntityPatch>   entities;
        
        internal override       TaskType            TaskType => TaskType.patch;
        public   override       string              TaskName =>  $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            var entityContainer = database.GetOrCreateContainer(container);
            var result = await entityContainer.PatchEntities(this, response, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error); 
            }
            if (result.patchErrors != null && result.patchErrors.Count > 0) {
                var patchErrors = SyncResponse.GetEntityErrors(ref response.patchErrorMap, container);
                patchErrors.AddErrors(result.patchErrors);
            }
            return result;
        }
    }

    public class EntityPatch
    {
                        public  JsonKey                     key;
        [Fri.Required]  public  List<JsonPatch>             patches;
        
        public EntityPatch() {}
        public EntityPatch(JsonKey key, List<JsonPatch> patches) {
            this.key        = key;
            this.patches    = patches;
        }
    }

    // ----------------------------------- task result -----------------------------------
    public class PatchEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    patchErrors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        
        internal override   TaskType                        TaskType => TaskType.patch;
    }
}