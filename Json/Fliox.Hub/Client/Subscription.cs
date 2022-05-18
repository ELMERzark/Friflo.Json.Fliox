﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.Hub.Client
{
    public delegate void SubscriptionHandler (SubscriptionProcessor processor, EventMessage ev);
    
    
    public abstract class SubscriptionProcessor
    {
        private readonly    Dictionary<Type, EntityChanges>     results   = new Dictionary<Type, EntityChanges>();
        private readonly    List<Message>                       messages  = new List<Message>();

        public              int                                 EventSequence { get; private set ; }
        public override     string                              ToString() => $"EventSequence: {EventSequence}";


        public abstract void EnqueueEvent(FlioxClient client, EventMessage ev);

        /// <summary>
        /// Process the <see cref="EventMessage.tasks"/> of the given <see cref="EventMessage"/>.
        /// These <see cref="EventMessage.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="FlioxClient.SubscribeAllChanges"/> or
        /// <see cref="FlioxClient.SubscribeMessage"/>.
        /// <br></br>
        /// Tasks notifying about database changes are applied to the <see cref="FlioxClient"/> the <see cref="SubscriptionProcessor"/>
        /// is attached to.
        /// Types of database changes refer to <see cref="Change.create"/>ed, <see cref="Change.upsert"/>ed,
        /// <see cref="Change.delete"/>ed and <see cref="Change.patch"/>ed entities.
        /// <br></br>
        /// Tasks notifying "messages" are ignored. These message subscriptions are registered by <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        protected virtual void ProcessEvent(FlioxClient client, EventMessage ev) {
            if (client._intern.disposed)  // store may already be disposed
                return;
            EventSequence++;
            using (var pooled = client.ObjectMapper.Get()) {
                var mapper = pooled.instance;
                var logger = client.Logger;
                foreach (var task in ev.tasks) {
                    EntitySet set;
                    switch (task.TaskType) {
                        
                        case TaskType.create:
                            var create = (CreateEntities)task;
                            set = client.GetEntitySet(create.container);
                            // apply changes only if subscribed
                            if (set.GetSubscription() == null)
                                continue;
                            create.entityKeys = GetKeysFromEntities (client, set.GetKeyName(), create.entities);
                            SyncPeerEntities(set, create.entityKeys, create.entities, mapper);
                            break;
                        
                        case TaskType.upsert:
                            var upsert = (UpsertEntities)task;
                            set = client.GetEntitySet(upsert.container);
                            // apply changes only if subscribed
                            if (set.GetSubscription() == null)
                                continue;
                            upsert.entityKeys = GetKeysFromEntities (client, set.GetKeyName(), upsert.entities);
                            SyncPeerEntities(set, upsert.entityKeys, upsert.entities, mapper);
                            break;
                        
                        case TaskType.delete:
                            var delete = (DeleteEntities)task;
                            set = client.GetEntitySet(delete.container);
                            // apply changes only if subscribed
                            if (set.GetSubscription() == null)
                                continue;
                            set.DeletePeerEntities (delete.ids);
                            break;
                        
                        case TaskType.patch:
                            var patches = (PatchEntities)task;
                            set = client.GetEntitySet(patches.container);
                            // apply changes only if subscribed
                            if (set.GetSubscription() == null)
                                continue;
                            set.PatchPeerEntities(patches.patches, mapper);
                            break;
                        
                        case TaskType.message:
                        case TaskType.command:
                            var message = (SyncMessageTask)task;
                            var name = message.name;
                            // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
                            // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
                            // and ProcessEvent() can be called concurrently from the 'main' thread.
                            var invokeContext = new InvokeContext(name, message.param, mapper.reader, logger);
                            if (client._intern.subscriptions.TryGetValue(name, out MessageSubscriber subscriber)) {
                                subscriber.InvokeCallbacks(invokeContext);    
                            }
                            foreach (var sub in client._intern.subscriptionsPrefix) {
                                if (name.StartsWith(sub.name)) {
                                    sub.InvokeCallbacks(invokeContext);
                                }
                            }
                            break;
                    }
                }
            }
            var subHandler = client._intern.subscriptionHandler;
            // ReSharper disable once UseNullPropagation
            if (subHandler == null)
                return;
            subHandler(this, ev); // subHandler.Invoke(this, ev);
        }
        
        private List<JsonKey> GetKeysFromEntities(FlioxClient client, string keyName, List<JsonValue> entities) {
            var processor = client._intern.EntityProcessor();
            var keys = new List<JsonKey>(entities.Count);
            foreach (var entity in entities) {
                if (!processor.GetEntityKey(entity, keyName, out JsonKey key, out string error))
                    throw new InvalidOperationException($"CreateEntityKeys() error: {error}");
                keys.Add(key);
            }
            return keys;
        }
        
        private static void SyncPeerEntities (EntitySet set, List<JsonKey> keys, List<JsonValue> entities, ObjectMapper mapper) {
            if (keys.Count != entities.Count)
                throw new InvalidOperationException("Expect equal counts");
            var syncEntities = new Dictionary<JsonKey, EntityValue>(entities.Count, JsonKey.Equality);
            for (int n = 0; n < entities.Count; n++) {
                var entity  = entities[n];
                var key     = keys[n];
                var value = new EntityValue(entity);
                syncEntities.Add(key, value);
            }
            set.SyncPeerEntities(syncEntities, mapper);
        }
        
        private EntityChanges<TKey, T> GetChanges<TKey, T> (EntitySet<TKey, T> entitySet) where T : class {
            if (!results.TryGetValue(typeof(T), out var result)) {
                var resultTyped = new EntityChanges<TKey, T>(entitySet);
                results.Add(typeof(T), resultTyped);
                return resultTyped;
            }
            return (EntityChanges<TKey, T>)result;
        }
        
        public List<Message> GetMessages(FlioxClient client, EventMessage eventMessage) {
            messages.Clear();
            using (var pooled = client.ObjectMapper.Get()) {
                var reader = pooled.instance.reader;
                var logger = client.Logger;
                foreach (var task in eventMessage.tasks) {
                    if (!(task is SyncMessageTask messageTask)) 
                        continue;
                    var invokeContext   = new InvokeContext(messageTask.name, messageTask.param, reader, logger);
                    var message         = new Message(invokeContext);
                    messages.Add(message);
                }
                return messages;
            }
        }
        
        public EntityChanges<TKey, T> GetEntityChanges<TKey, T>(EntitySet<TKey, T> entitySet, EventMessage eventMessage) where T : class {
            var result  = GetChanges(entitySet);
            result.Clear();
            
            foreach (var task in eventMessage.tasks) {
                switch (task.TaskType) {
                    
                    case TaskType.create:
                        var create = (CreateEntities)task;
                        if (create.container != entitySet.name)
                            continue;
                        for (int n = 0; n < create.entityKeys.Count; n++) {
                            var     id      = create.entityKeys[n];
                            TKey    key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                            var     peer    = entitySet.GetOrCreatePeerByKey(key, id);
                            var     entity  = peer.Entity;
                            result.creates.Add(key, entity);
                        }
                        result.Info.creates += create.entityKeys.Count;
                        break;
                    
                    case TaskType.upsert:
                        var upsert = (UpsertEntities)task;
                        if (upsert.container != entitySet.name)
                            continue;
                        for (int n = 0; n < upsert.entityKeys.Count; n++) {
                            var     id      = upsert.entityKeys[n];
                            TKey    key     = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                            var     peer    = entitySet.GetOrCreatePeerByKey(key, id);
                            var     entity  = peer.Entity;
                            result.upserts.Add(key, entity);
                        }
                        result.Info.upserts += upsert.entityKeys.Count;
                        break;
                    
                    case TaskType.delete:
                        var delete = (DeleteEntities)task;
                        if (delete.container != entitySet.name)
                            continue;
                        foreach (var id in delete.ids) {
                            TKey    key      = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                            result.deletes.Add(key);
                        }
                        result.Info.deletes += delete.ids.Count;
                        break;
                    
                    case TaskType.patch:
                        var patch = (PatchEntities)task;
                        if (patch.container != entitySet.name)
                            continue;
                        foreach (var pair in patch.patches) {
                            var         id          = pair.Key;
                            TKey        key         = Ref<TKey,T>.RefKeyMap.IdToKey(id);
                            var         peer        = entitySet.GetOrCreatePeerByKey(key, id);
                            var         entity      = peer.Entity;
                            EntityPatch entityPatch = pair.Value;
                            var         changePatch = new ChangePatch<T>(entity, entityPatch.patches);
                            result.patches.Add(key, changePatch);
                        }
                        result.Info.patches += patch.patches.Count;
                        break;
                }
            }
            return result;
        }
    }
    
    public sealed class ChangeInfo<T> : ChangeInfo where T : class
    {
        public bool IsEqual(ChangeInfo<T> other) {
            return creates == other.creates &&
                   upserts == other.upserts &&
                   deletes == other.deletes &&
                   patches == other.patches;
        }
    }
    
    public abstract class EntityChanges { }
    
    public sealed class EntityChanges<TKey, T> : EntityChanges where T : class {
        public              ChangeInfo<T>                       Info { get; }
        // ReSharper disable once NotAccessedField.Local
        private  readonly   EntitySet<TKey, T>                  entitySet; // only for debugging ergonomics
        
        public   readonly   Dictionary<TKey, T>                 creates = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   Dictionary<TKey, T>                 upserts = SyncSet.CreateDictionary<TKey, T>();
        public   readonly   HashSet   <TKey>                    deletes = SyncSet.CreateHashSet<TKey>();
        public   readonly   Dictionary<TKey, ChangePatch<T>>    patches = SyncSet.CreateDictionary<TKey, ChangePatch<T>>();
        
        public override     string                              ToString() => Info.ToString();       

        internal EntityChanges(EntitySet<TKey, T> entitySet) {
            this.entitySet = entitySet;
            Info = new ChangeInfo<T>();
        }

        internal void Clear() {
            creates.Clear();
            upserts.Clear();
            deletes.Clear();
            patches.Clear();
            //
            Info.Clear();
        }
    }
    
    public readonly struct ChangePatch<T> where T : class {
        public  readonly    T                   entity;
        public  readonly    List<JsonPatch>     patches;

        public  override    string              ToString() => EntitySetBase<T>.EntityKeyMap.GetId(entity).AsString();
        
        public ChangePatch(T entity, List<JsonPatch> patches) {
            this.entity     = entity;
            this.patches    = patches;
        }
    }
}