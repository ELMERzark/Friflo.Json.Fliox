﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;

namespace Friflo.Json.Fliox.DB.Host
{
    public sealed class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool    pretty;

        public MemoryDatabase(TaskHandler taskHandler = null, DbOpt opt = null, bool pretty = false) : base(taskHandler, opt) {
            this.pretty = pretty;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, pretty);
        }
    }
    
    public sealed class MemoryContainer : EntityContainer
    {
        private  readonly   Dictionary<JsonKey, JsonValue>  keyValues = new Dictionary<JsonKey, JsonValue>(JsonKey.Equality);
        
        public   override   bool                            Pretty      { get; }
        
        public    override  string                          ToString()  => $"{base.ToString()}, Count: {keyValues.Count}";

        public MemoryContainer(string name, EntityDatabase database, bool pretty)
            : base(name, database)
        {
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            for (int n = 0; n < entities.Count; n++) {
                var key     = command.entityKeys[n];
                var payload = entities[n];
                if (keyValues.TryGetValue(key, out JsonValue _))
                    throw new InvalidOperationException($"Entity with key '{key}' already in DatabaseContainer: {name}");
                keyValues[key] = payload;
            }
            var result = new CreateEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, MessageContext messageContext) {
            var entities = command.entities;
            AssertEntityCounts(command.entityKeys, entities);
            for (int n = 0; n < entities.Count; n++) {
                var key     = command.entityKeys[n];
                var payload = entities[n];
                keyValues[key] = payload;
            }
            var result = new UpsertEntitiesResult();
            return Task.FromResult(result);
        }

        public override Task<ReadEntitiesSetResult> ReadEntitiesSet(ReadEntitiesSet command, MessageContext messageContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                keyValues.TryGetValue(key, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesSetResult{entities = entities};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, MessageContext messageContext) {
            var ids     = keyValues.Keys.ToHashSet(JsonKey.Equality);  // TAG_PERF
            var result  = await FilterEntityIds(command, ids, messageContext).ConfigureAwait(false);
            return result;
        }
        
        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, MessageContext messageContext) {
            var keys = command.ids;
            if (keys != null && keys.Count > 0) {
                foreach (var key in keys) {
                    keyValues.Remove(key);
                }
            }
            var all = command.all;
            if (all != null && all.Value) {
                keyValues.Clear();
            }
            var result = new DeleteEntitiesResult();
            return Task.FromResult(result);
        }
    }
}