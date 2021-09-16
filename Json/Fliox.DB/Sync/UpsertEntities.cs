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
    public class UpsertEntities : DatabaseTask
    {
        [Fri.Required]  public  string                          container;
                        public  JsonKey?                        key;
        [Fri.Required]  public  List<JsonValue>                 entities;
        
        [Fri.Ignore]    public  List<JsonKey>                   entityKeys;
        
        internal override       TaskType                        TaskType => TaskType.upsert;
        public   override       string                          TaskName => $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            entityKeys = EntityContainer.CreateEntityKeys(key, entities, messageContext, out string error);
            if (entityKeys == null) {
                return InvalidTask(error);
            }
            error = database.schema?.ValidateEntities (container, entityKeys, entities, messageContext, EntityErrorType.WriteError, ref response.upsertErrors);
            if (error != null) {
                return TaskError(new CommandError(error));
            }
            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooledPatcher.instance;
                    for (int n = 0; n < entities.Count; n++) {
                        var entity = entities[n];
                        // if (entity.json == null)  continue; // TAG_ENTITY_NULL
                        // if (json == null)
                        //     return InvalidTask("value of entities key/value elements not be null");
                        entities[n] = new JsonValue(patcher.Copy(entity.json, true));
                    }
                }
            }
            var result = await entityContainer.UpsertEntities(this, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.upsertErrors != null && result.upsertErrors.Count > 0) {
                var upsertErrors = SyncResponse.GetEntityErrors(ref response.upsertErrors, container);
                upsertErrors.AddErrors(result.upsertErrors);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class UpsertEntitiesResult : TaskResult, ICommandResult
    {
        public              CommandError                        Error { get; set; }
        [Fri.Ignore] public Dictionary<JsonKey, EntityError>    upsertErrors;

        internal override   TaskType                        TaskType => TaskType.upsert;
    }
}