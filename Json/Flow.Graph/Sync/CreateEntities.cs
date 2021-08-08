﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Flow.Database;
using Friflo.Json.Flow.Mapper;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Sync
{
    // ----------------------------------- task -----------------------------------
    public class CreateEntities : DatabaseTask
    {
        [Fri.Property(Required = true)]
        public  string                          container;
        [Fri.Property(Required = true)]
        public  Dictionary<string, EntityValue> entities;
        
        internal override   TaskType            TaskType => TaskType.create;
        public   override   string              TaskName => $"container: '{container}'";
        
        internal override async Task<TaskResult> Execute(EntityDatabase database, SyncResponse response, MessageContext messageContext) {
            if (container == null)
                return MissingContainer();
            if (entities == null)
                return MissingField(nameof(entities));
            
            var schema = database.schema;
            if (schema != null) {
                var validationResult = schema.ValidateEntities (container, entities, messageContext);
                /* if (validationResult != null) {
                    var errors = SyncResponse.GetEntityErrors(ref response.createErrors, container);
                    errors.AddErrors(validationResult.validationErrors);
                    return TaskError(validationResult.error);
                } */
            }

            var entityContainer = database.GetOrCreateContainer(container);
            // may call patcher.Copy() always to ensure a valid JSON value
            if (entityContainer.Pretty) {
                using (var pooledPatcher = messageContext.pools.JsonPatcher.Get()) {
                    JsonPatcher patcher = pooledPatcher.instance;
                    foreach (var entity in entities) {
                        var value = entity.Value;
                        if (value.Json == null)
                            return InvalidTask("value of entities key/value elements not be null");
                        value.SetJson(patcher.Copy(value.Json, true));
                    }
                }
            }
            var result = await entityContainer.CreateEntities(this, messageContext).ConfigureAwait(false);
            if (result.Error != null) {
                return TaskError(result.Error);
            }
            if (result.createErrors != null && result.createErrors.Count > 0) {
                var createErrors = SyncResponse.GetEntityErrors(ref response.createErrors, container);
                createErrors.AddErrors(result.createErrors);
            }
            return result;
        }
    }
    
    // ----------------------------------- task result -----------------------------------
    public class CreateEntitiesResult : TaskResult, ICommandResult
    {
                     public CommandError                    Error { get; set; }
        [Fri.Ignore] public Dictionary<string, EntityError> createErrors;
        
        internal override   TaskType                        TaskType => TaskType.create;
    }
}