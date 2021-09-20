﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Sync
{
    // ----------------------------------- request -----------------------------------
    public class SyncRequest : DatabaseRequest
    {
        /// <summary>
        /// Specify an optional id to identify the client performing a request by a host.
        /// In case the request contains a <see cref="SubscribeChanges"/> <see cref="clientId"/> is required to
        /// enable sending <see cref="SubscriptionEvent"/>'s to the desired subscriber.
        /// </summary>
        [Fri.Property(Name = "client")] public  string              clientId;
        /// <summary>
        /// <see cref="eventAck"/> is used to ensure (change) events are delivered reliable.
        /// A client set <see cref="eventAck"/> to the last received <see cref="DatabaseEvent.seq"/> in case
        /// it has subscribed to database changes by a <see cref="SubscribeChanges"/> task.
        /// Otherwise <see cref="eventAck"/> is null.
        /// </summary>
        [Fri.Property(Name = "ack")]    public  int?                eventAck;
                                        public  string              token;
        [Fri.Required]                  public  List<DatabaseTask>  tasks;
        
        internal override                       RequestType         RequestType => RequestType.sync;
    }
    
    // ----------------------------------- response -----------------------------------
    public class SyncResponse : DatabaseResponse
    {
                        public  ErrorResponse                           error;
                        public  List<TaskResult>                        tasks;
                        public  List<ContainerEntities>                 results;
                        public  List<EntityErrors>                      createErrors;
                        public  List<EntityErrors>                      upsertErrors;
                        public  List<EntityErrors>                      patchErrors;
                        public  List<EntityErrors>                      deleteErrors;

        // key of all Dictionary's is the container name
        [Fri.Ignore]    public  Dictionary<string, ContainerEntities>   resultMap;
        [Fri.Ignore]    public  Dictionary<string, EntityErrors>        createErrorMap; // lazy instantiation
        [Fri.Ignore]    public  Dictionary<string, EntityErrors>        upsertErrorMap; // lazy instantiation
        [Fri.Ignore]    public  Dictionary<string, EntityErrors>        patchErrorMap;  // lazy instantiation
        [Fri.Ignore]    public  Dictionary<string, EntityErrors>        deleteErrorMap; // lazy instantiation
        
        internal override   RequestType                 RequestType => RequestType.sync;
        
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
    public class ContainerEntities
    {
        [Fri.Required]  public  string                              container;
        [Fri.Required]  public  List<JsonValue>                     entities  = new List<JsonValue>();
                        public  List<JsonKey>                       notFound;
                        public  List<EntityError>                   errors;
                        
        [Fri.Ignore]    public  Dictionary<JsonKey, EntityValue>    entityMap = new Dictionary<JsonKey, EntityValue>(JsonKey.Equality); // todo remove instantiation
        
        internal void AddEntities(Dictionary<JsonKey, EntityValue> add) {
            entityMap.EnsureCapacity(entityMap.Count + add.Count);
            foreach (var entity in add) {
                entityMap.TryAdd(entity.Key, entity.Value);
            }
        }
    }
    
    public class EntityErrors
    {
        [Fri.Required]  public  string                              container;
                        public  List<EntityError>                   errors;
        [Fri.Ignore]    public  Dictionary<JsonKey, EntityError>    errorMap;
        
        public EntityErrors() {} // required for TypeMapper

        public EntityErrors(string container) {
            this.container  = container;
            errorMap        = new Dictionary<JsonKey, EntityError>(JsonKey.Equality);
        }
        
        internal void AddErrors(Dictionary<JsonKey, EntityError> errors) {
            foreach (var error in errors) {
                this.errorMap.TryAdd(error.Key, error.Value);
            }
        }

        internal void SetInferredErrorFields() {
            foreach (var errorEntry in errorMap) {
                var error = errorEntry.Value;
                // error.container are not serialized as they are redundant data.
                // Infer their values from containing errors dictionary
                error.container = container;
            }
        }
    }
}
