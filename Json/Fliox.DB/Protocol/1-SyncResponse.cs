// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Protocol
{
    // ----------------------------------- response -----------------------------------
    public sealed class SyncResponse : ProtocolResponse
    {
                        /// null in case authentication was successful
                        public  string                                  authState;
                        public  List<SyncTaskResult>                    tasks;
                        public  List<ContainerEntities>                 results;
        // key of all Dictionary's is the container name
        [Fri.Ignore]    public  Dictionary<string, ContainerEntities>   resultMap;
                        public  Dictionary<string, EntityErrors>        createErrors; // lazy instantiation
                        public  Dictionary<string, EntityErrors>        upsertErrors; // lazy instantiation
                        public  Dictionary<string, EntityErrors>        patchErrors;  // lazy instantiation
                        public  Dictionary<string, EntityErrors>        deleteErrors; // lazy instantiation
        /// Can be utilized to return debug / development data - e.g. execution times or resource usage. 
                        public  JsonValue                               info;
                        
        internal override        MessageType                            MessageType => MessageType.resp;
        
        internal ContainerEntities GetContainerResult(string container) {
            if (resultMap.TryGetValue(container, out ContainerEntities result))
                return result;
            result = new ContainerEntities {
                container = container,
                entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality)
            };
            resultMap.Add(container, result);
            return result;
        }
        
        internal static EntityErrors GetEntityErrors(ref Dictionary<string, EntityErrors> entityErrorMap, string container) {
            if (entityErrorMap == null) {
                entityErrorMap = new Dictionary<string, EntityErrors>();
            }
            if (entityErrorMap.TryGetValue(container, out EntityErrors result))
                return result;
            result = new EntityErrors(container);
            entityErrorMap.Add(container, result);
            return result;
        }
        
        public void AssertResponse(SyncRequest request) {
            var expect = request.tasks.Count;
            var actual = tasks.Count;
            if (expect != actual) {
                var msg = $"Expect response.task.Count == request.task.Count: expect: {expect}, actual: {actual}"; 
                throw new InvalidOperationException(msg);
            }
        }
    }
    
    // ----------------------------------- sync results -----------------------------------
    public sealed class ContainerEntities
    {
        /// Required only by <see cref="Remote.RemoteHostDatabase"/> for serialization
        [Fri.Required]  public  string                              container;
        /// Required only by <see cref="Remote.RemoteHostDatabase"/> for serialization
        [Fri.Required]  public  List<JsonValue>                     entities;
        /// Required only by <see cref="Remote.RemoteHostDatabase"/> for serialization
                        public  List<JsonKey>                       notFound;
        /// Required only by <see cref="Remote.RemoteHostDatabase"/> for serialization
                        public  Dictionary<JsonKey, EntityError>    errors    = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        
        [Fri.Ignore]    public  Dictionary<JsonKey, EntityValue>    entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality);
        
        internal void AddEntities(Dictionary<JsonKey, EntityValue> add) {
            entityMap.EnsureCapacity(entityMap.Count + add.Count);
            foreach (var entity in add) {
                entityMap.TryAdd(entity.Key, entity.Value);
            }
        }
    }
    
    public sealed class EntityErrors
    {
                        public  string                              container; // only for debugging
        [Fri.Required]  public  Dictionary<JsonKey, EntityError>    errors = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        
        public EntityErrors() {} // required for TypeMapper

        public EntityErrors(string container) {
            this.container  = container;
            errors          = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        }
        
        internal void AddErrors(Dictionary<JsonKey, EntityError> errors) {
            foreach (var error in errors) {
                this.errors.TryAdd(error.Key, error.Value);
            }
        }

        internal void SetInferredErrorFields() {
            foreach (var errorEntry in errors) {
                var error = errorEntry.Value;
                // error .id & .container are not serialized as they are redundant data.
                // Infer their values from containing errors dictionary
                error.id        = errorEntry.Key;
                error.container = container;
            }
        }
    }
}