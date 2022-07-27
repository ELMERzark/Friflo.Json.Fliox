﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Hub.Client.Event;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// Used to process <see cref="EventMessage"/>'s received by a <see cref="FlioxClient"/>.  
    /// </summary>
    public sealed class SubscriptionProcessor : IDisposable
    {
        private  readonly   EventContext                eventContext;
        private  readonly   Dictionary<string, Changes> changes         = new Dictionary<string, Changes>();
        /// <summary> contain only <see cref="Changes"/> where <see cref="Changes.Count"/> > 0 </summary>
        internal readonly   List<Changes>               contextChanges  = new List<Changes>();
        internal readonly   List<Message>               messages        = new List<Message>();
        private             ObjectMapper                objectMapper;
        internal            int                         EventCount { get; private set ; }
        
        public   override   string                      ToString()  => $"EventCount: {EventCount}";
        
        public SubscriptionProcessor() {
            eventContext = new EventContext(this);
        }

        public void Dispose() {
            objectMapper?.Dispose();
        }

        /// <summary>
        /// Process the <see cref="EventMessage.tasks"/> of the given <see cref="EventMessage"/>.
        /// These <see cref="EventMessage.tasks"/> are "messages" resulting from subscriptions registered by
        /// methods like <see cref="EntitySet{TKey,T}.SubscribeChanges"/>, <see cref="FlioxClient.SubscribeAllChanges"/> or
        /// <see cref="FlioxClient.SubscribeMessage"/>.
        /// </summary>
        public void ProcessEvent(FlioxClient client, EventMessage ev) {
            eventContext.Init(client, ev);
            if (client._intern.disposed)  // store may already be disposed
                return;
            if (objectMapper == null) {
                // use individual ObjectMapper for messages as they are used by App outside the pooled scope below
                objectMapper = new ObjectMapper(client._intern.typeStore);
                objectMapper.ErrorHandler = ObjectReader.NoThrow;
            }
            messages.Clear();
            // clear all changes from the last event
            foreach (var change in contextChanges) {
                change.Clear();
            }
            contextChanges.Clear();
            EventCount++;

            foreach (var task in ev.tasks) {
                switch (task.TaskType)
                {
                    case TaskType.create:   ProcessCreate (client, (CreateEntities) task);  break;
                    case TaskType.upsert:   ProcessUpsert (client, (UpsertEntities) task);  break;
                    case TaskType.delete:   ProcessDelete (client, (DeleteEntities) task);  break;
                    case TaskType.patch:    ProcessPatch  (client, (PatchEntities)  task);  break;
                    case TaskType.message:
                    case TaskType.command:  ProcessMessage(        (SyncMessageTask)task);  break;
                }
            }
            // After processing event message invoke their handler methods:
            
            // --- invoke subscription event handler
            client._intern.subscriptionEventHandler?.Invoke(eventContext);
            
            // --- invoke changes handlers
            foreach (var change in contextChanges) {
                var container = change.Container;
                client._intern.TryGetSetByName(container, out EntitySet set);
                set.changeCallback?.InvokeCallback(change, eventContext);
            }
            if (contextChanges.Count > 0) {
                client._intern.changeSubscriptionHandler?.Invoke(eventContext);
            }
            
            // --- invoke message handlers
            foreach (var message in messages) {
                var name = message.Name;
                var subs = client._intern.subscriptions;
                if (subs != null && subs.TryGetValue(name, out MessageSubscriber subscriber)) {
                    subscriber.InvokeCallbacks(message.invokeContext, eventContext);    
                }
                var subsPrefix = client._intern.subscriptionsPrefix;
                if (subsPrefix != null) {
                    foreach (var sub in subsPrefix) {
                        if (name.StartsWith(sub.name)) {
                            sub.InvokeCallbacks(message.invokeContext, eventContext);
                        }
                    }
                }
            }
        }
        
        private void ProcessCreate(FlioxClient client, CreateEntities create) {
            var entities = create.entities;
            if (entities.Count == 0)
                return;
            var set = client.GetEntitySet(create.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            entityChanges.rawCreates.AddRange(entities);
            entityChanges.changeInfo.creates += entities.Count;
        }
        
        private void ProcessUpsert(FlioxClient client, UpsertEntities upsert) {
            var entities = upsert.entities;
            if (entities.Count == 0)
                return;
            var set = client.GetEntitySet(upsert.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            entityChanges.rawUpserts.AddRange(entities);
            entityChanges.changeInfo.upserts += entities.Count;
        }
        
        private void ProcessDelete(FlioxClient client, DeleteEntities delete) {
            var ids = delete.ids;
            if (ids.Count == 0)
                return;
            var set = client.GetEntitySet(delete.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            entityChanges.AddDeletes(ids);
        }
        
        private void ProcessPatch(FlioxClient client, PatchEntities patchEntities) {
            var patches = patchEntities.patches;
            if (patches.Count == 0)
                return;
            var set = client.GetEntitySet(patchEntities.container);
            if (set.GetSubscription() == null) {
                return;
            }
            // --- update changes
            var entityChanges = GetChanges(set);
            AddChanges(entityChanges);
            entityChanges.AddPatches(patches);
        }
        
        private void ProcessMessage(SyncMessageTask task) {
            var name = task.name;
            // callbacks require their own reader as store._intern.jsonMapper.reader cannot be used.
            // This jsonMapper is used in various threads caused by .ConfigureAwait(false) continuations
            // and ProcessEvent() can be called concurrently from the 'main' thread.
            var invokeContext   = new InvokeContext(name, task.param, objectMapper.reader);
            var message         = new Message(invokeContext);
            messages.Add(message);
        }
        
        private void AddChanges(Changes entityChanges) {
            if (entityChanges.added)
                return;
            contextChanges.Add(entityChanges);
            entityChanges.added = true;
        }
        
        internal Changes GetChanges (EntitySet entitySet) {
            if (changes.TryGetValue(entitySet.name, out var change))
                return change;
            object[] constructorParams = { entitySet, objectMapper };
            var keyType     = entitySet.KeyType;
            var entityType  = entitySet.EntityType;
            var genericArgs = new[] { keyType, entityType };
            var instance    = TypeMapperUtils.CreateGenericInstance(typeof(Changes<,>), genericArgs, constructorParams);
            change          = (Changes)instance;
            changes.Add(entitySet.name, change);
            return change;
        }
    }
}