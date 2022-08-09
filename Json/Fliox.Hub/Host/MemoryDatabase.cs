﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Utils;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;

namespace Friflo.Json.Fliox.Hub.Host
{
    /// <summary>
    /// A <see cref="MemoryDatabase"/> is a non-persistent database used to store records in memory.
    /// </summary>
    /// <remarks>
    /// The intention is having a shared database which can be used in high performance scenarios. <br/>
    /// E.g. on a 4 Core CPU it is able to achieve more than 500.000 request / second. <br/>
    /// Following use-cases are suitable for a <see cref="MemoryDatabase"/>
    /// <list type="bullet">
    ///   <item>Run a big amount of unit tests fast and efficient as instantiation of <see cref="MemoryDatabase"/> take only some micro seconds. </item>
    ///   <item>Use as a Game Session database for online multiplayer games as it provide sub millisecond response latency</item>
    ///   <item>Use as test database for <b>TDD</b> without any configuration </item>
    ///   <item>Is the benchmark reference for all other database implementations regarding throughput and latency</item>
    /// </list>
    /// <see cref="MemoryDatabase"/> has no third party dependencies.
    /// <i>Storage characteristics</i> <br/>
    /// <b>Keys</b> are stored as <see cref="JsonKey"/> - keys that can be converted to <see cref="long"/> or <see cref="Guid"/>
    /// are stored without heap allocation. Otherwise a <see cref="string"/> is allocated <br/>
    /// <b>Values</b> are stored as <see cref="JsonValue"/> - essentially a <see cref="byte"/>[]
    /// </remarks>
    public sealed class MemoryDatabase : EntityDatabase
    {
        private  readonly   bool        pretty;
        private  readonly   MemoryType  containerType;
        public   override   string      StorageType => "in-memory";

        public MemoryDatabase(string dbName, TaskHandler handler = null, MemoryType? type = null, DbOpt opt = null, bool pretty = false)
            : base(dbName, handler, opt)
        {
            this.pretty     = pretty;
            containerType   = type ?? MemoryType.Concurrent;
        }
        
        public override EntityContainer CreateContainer(string name, EntityDatabase database) {
            return new MemoryContainer(name, database, containerType, pretty);
        }
    }
    
    public enum MemoryType {
        Concurrent,
        /// used to preserve insertion order of entities in ClusterDB and MonitorDB
        NonConcurrent
    }
    
    public sealed class MemoryContainer : EntityContainer
    {
        private  readonly   IDictionary<JsonKey, JsonValue>             keyValues;
        private  readonly   ConcurrentDictionary<JsonKey, JsonValue>    keyValuesConcurrent;
        
        public   override   bool                                        Pretty      { get; }
        
        public    override  string  ToString()  => $"{name}  Count: {keyValues.Count}";

        public MemoryContainer(string name, EntityDatabase database, MemoryType type, bool pretty)
            : base(name, database)
        {
            if (type == MemoryType.Concurrent) {
                keyValuesConcurrent = new ConcurrentDictionary<JsonKey, JsonValue>(JsonKey.Equality);
                keyValues           = keyValuesConcurrent;
            } else {
                keyValues = new Dictionary<JsonKey, JsonValue>(JsonKey.Equality);
            }
            Pretty = pretty;
        }
        
        public override Task<CreateEntitiesResult> CreateEntities(CreateEntities command, SyncContext syncContext) {
            var entities = command.entities;
            List<EntityError> createErrors = null;
            AssertEntityCounts(command.entityKeys, entities);
            for (int n = 0; n < entities.Count; n++) {
                var key     = command.entityKeys[n];
                var payload = entities[n];
                if (keyValues.TryAdd(key, payload))
                    continue;
                var error = new EntityError(EntityErrorType.WriteError, name, key, "entity already exist");
                AddEntityError(ref createErrors, key, error);
            }
            var result = new CreateEntitiesResult { errors = createErrors };
            return Task.FromResult(result);
        }

        public override Task<UpsertEntitiesResult> UpsertEntities(UpsertEntities command, SyncContext syncContext) {
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

        public override Task<ReadEntitiesResult> ReadEntities(ReadEntities command, SyncContext syncContext) {
            var keys = command.ids;
            var entities = new Dictionary<JsonKey, EntityValue>(keys.Count, JsonKey.Equality);
            foreach (var key in keys) {
                keyValues.TryGetValue(key, out var payload);
                var entry = new EntityValue(payload);
                entities.TryAdd(key, entry);
            }
            var result = new ReadEntitiesResult{entities = entities};
            return Task.FromResult(result);
        }
        
        public override async Task<QueryEntitiesResult> QueryEntities(QueryEntities command, SyncContext syncContext) {
            if (!FindCursor(command.cursor, syncContext, out var keyValueEnum, out var error)) {
                return new QueryEntitiesResult { Error = error };
            }
            keyValueEnum = keyValueEnum ?? new MemoryQueryEnumerator(keyValues);   // TAG_PERF
            try {
                var result  = await FilterEntities(command, keyValueEnum, syncContext).ConfigureAwait(false);
                return result;
            }
            finally {
                keyValueEnum.Dispose();
            }
        }
        
        public override async Task<AggregateEntitiesResult> AggregateEntities (AggregateEntities command, SyncContext syncContext) {
            var filter = command.GetFilter();
            switch (command.type) {
                case AggregateType.count:
                    // count all?
                    if (filter.IsTrue) {
                        var count = keyValues.Count;
                        return new AggregateEntitiesResult { container = command.container, value = count };
                    }
                    var result = await CountEntities(command, syncContext).ConfigureAwait(false);
                    return result;
            }
            return new AggregateEntitiesResult { Error = new CommandError($"aggregate {command.type} not implement") };
        }

        public override Task<DeleteEntitiesResult> DeleteEntities(DeleteEntities command, SyncContext syncContext) {
            var keys = command.ids;
            if (keys != null && keys.Count > 0) {
                foreach (var key in keys) {
                    if (keyValuesConcurrent != null) {
                        keyValuesConcurrent.TryRemove(key, out _);
                        continue;
                    }
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
    
    internal class MemoryQueryEnumerator : QueryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<JsonKey, JsonValue>>  enumerator;
        
        internal MemoryQueryEnumerator(IDictionary<JsonKey, JsonValue> map) {
            enumerator = map.GetEnumerator();
        }

        public override bool MoveNext() {
            return enumerator.MoveNext();
        }

        public override JsonKey Current => enumerator.Current.Key;

        protected override void DisposeEnumerator() {
            enumerator.Dispose();
        }
        
        // --- ContainerEnumerator
        public override bool            IsAsync             => false;
        public override JsonValue       CurrentValue        => enumerator.Current.Value;
        public override Task<JsonValue> CurrentValueAsync() => throw new NotImplementedException();
    }
}