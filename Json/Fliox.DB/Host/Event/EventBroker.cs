﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;

namespace Friflo.Json.Fliox.DB.Host.Event
{
    public interface IEventTarget {
        bool        IsOpen ();
        Task<bool>  ProcessEvent(ProtocolEvent ev, MessageContext messageContext);
    }
    
    public sealed class EventBroker : IDisposable
    {
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        /// key: <see cref="EventSubscriber.clientId"/>
        private  readonly   ConcurrentDictionary<JsonKey, EventSubscriber>  subscribers;
        internal readonly   bool                                            background;

        public EventBroker (bool background) {
            jsonEvaluator   = new JsonEvaluator();
            subscribers     = new ConcurrentDictionary<JsonKey, EventSubscriber>(JsonKey.Equality);
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        internal bool TryGetSubscriber(JsonKey key, out EventSubscriber subscriber) {
            return subscribers.TryGetValue(key, out subscriber);
        }
        
        /// used for test assertion
        public int NotAcknowledgedEvents() {
            int count = 0;
            foreach (var subscriber in subscribers) {
                count += subscriber.Value.SentEventsCount;
            }
            return count;
        }

        public async Task FinishQueues() {
            if (!background)
                return;
            var loopTasks = new List<Task>();
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                subscriber.FinishQueue();
                loopTasks.Add(subscriber.triggerLoop);
            }
            await Task.WhenAll(loopTasks).ConfigureAwait(false);
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal void SubscribeMessage(SubscribeMessage subscribe, in JsonKey clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            var remove = subscribe.remove;
            var prefix = Protocol.Tasks.SubscribeMessage.GetPrefix(subscribe.name);
            if (remove.HasValue && remove.Value) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                if (prefix == null) {
                    subscriber.messageSubscriptions.Remove(subscribe.name);
                } else {
                    subscriber.messagePrefixSubscriptions.Remove(prefix);
                }
                RemoveEmptySubscriber(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            if (prefix == null) {
                subscriber.messageSubscriptions.Add(subscribe.name);
            } else {
                subscriber.messagePrefixSubscriptions.Add(prefix);
            }
        }

        internal void SubscribeChanges (SubscribeChanges subscribe, in JsonKey clientId, IEventTarget eventTarget) {
            EventSubscriber subscriber;
            if (subscribe.changes.Count == 0) {
                if (!subscribers.TryGetValue(clientId, out subscriber))
                    return;
                subscriber.changeSubscriptions.Remove(subscribe.container);
                RemoveEmptySubscriber(subscriber, clientId);
                return;
            }
            subscriber = GetOrCreateSubscriber(clientId, eventTarget);
            subscriber.changeSubscriptions[subscribe.container] = subscribe;
        }
        
        private EventSubscriber GetOrCreateSubscriber(in JsonKey clientId, IEventTarget eventTarget) {
            subscribers.TryGetValue(clientId, out EventSubscriber subscriber);
            if (subscriber != null)
                return subscriber;
            subscriber = new EventSubscriber(clientId, eventTarget, background);
            subscribers.TryAdd(clientId, subscriber);
            return subscriber;
        }
        
        private void RemoveEmptySubscriber(EventSubscriber subscriber, in JsonKey clientId) {
            if (subscriber.SubscriptionCount > 0)
                return;
            subscribers.TryRemove(clientId, out _);
        }
        
        
        // -------------------------- event distribution --------------------------------
        // use only for testing
        internal async Task SendQueuedEvents() {
            if (background) {
                throw new InvalidOperationException("must not be called, if using a background Tasks");
            }
            foreach (var pair in subscribers) {
                var subscriber = pair.Value;
                await subscriber.SendEvents().ConfigureAwait(false);
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, MessageContext messageContext) {
            ref JsonKey  clientId = ref messageContext.clientId;
            if (clientId.IsNull())
                return;
            
            if (!subscribers.TryGetValue(clientId, out var subscriber))
                return;
            var eventTarget = messageContext.eventTarget;
            if (eventTarget != null) {
                subscriber.UpdateTarget (eventTarget);
            }
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            int value =  eventAck.Value;
            subscriber.AcknowledgeEvents(value);
        }
        
        private static void AddTask(ref List<SyncRequestTask> tasks, SyncRequestTask task) {
            if (tasks == null) {
                tasks = new List<SyncRequestTask>();
            }
            tasks.Add(task);
        }

        internal void EnqueueSyncTasks (SyncRequest syncRequest, MessageContext messageContext) {
            ProcessSubscriber (syncRequest, messageContext);
            using (var pooledMapper = messageContext.pools.ObjectMapper.Get()) {
                ObjectWriter writer = pooledMapper.instance.writer;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                foreach (var pair in subscribers) {
                    List<SyncRequestTask>  tasks = null;
                    EventSubscriber     subscriber = pair.Value;
                    if (subscriber.SubscriptionCount == 0)
                        throw new InvalidOperationException("Expect SubscriptionCount > 0");
                    
                    // Enqueue only change events for (change) tasks which are not send by the client itself
                    bool subscriberIsSender = messageContext.clientId.IsEqual(subscriber.clientId);
                    
                    foreach (var task in syncRequest.tasks) {
                        foreach (var changesPair in subscriber.changeSubscriptions) {
                            if (subscriberIsSender)
                                continue;
                            SubscribeChanges subscribeChanges = changesPair.Value;
                            var taskResult = FilterChanges(task, subscribeChanges);
                            if (taskResult == null)
                                continue;
                            AddTask(ref tasks, taskResult);
                        }
                        if (task.TaskType == TaskType.message) {
                            var message = (SendMessage) task;
                            if (!subscriber.FilterMessage(message.name))
                                continue;
                            AddTask(ref tasks, task);
                        }
                    }
                    if (tasks == null)
                        continue;
                    var eventMessage = new EventMessage {
                        tasks       = tasks.ToArray(),
                        srcUserId   = syncRequest.userId,
                        dstClientId = subscriber.clientId
                    };
                    if (SerializeRemoteEvents && subscriber.IsRemoteTarget) {
                        SerializeRemoteEvent(eventMessage, tasks, writer);
                    }
                    subscriber.EnqueueEvent(eventMessage);
                }
            }
        }
        
        private const bool SerializeRemoteEvents = true; // set to false for development

        /// Optimization: For remote connections the tasks are serialized to <see cref="EventMessage.tasksJson"/>.
        /// Benefits of doing this:
        /// - serialize a task only once for multiple targets
        /// - storing only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore
        private static void SerializeRemoteEvent(EventMessage eventMessage, List<SyncRequestTask> tasks, ObjectWriter writer) {
            var tasksJson = new JsonValue [tasks.Count];
            eventMessage.tasksJson = tasksJson;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                if (task.json == null) {
                    task.json = new JsonUtf8(writer.WriteAsArray(task));
                }
                tasksJson[n] = new JsonValue(task.json.Value);
            }
            tasks.Clear();
            eventMessage.tasks = null;
        }

        private SyncRequestTask FilterChanges (SyncRequestTask task, SubscribeChanges subscribe) {
            switch (task.TaskType) {
                
                case TaskType.create:
                    if (!subscribe.changes.Contains(Change.create))
                        return null;
                    var create = (CreateEntities) task;
                    if (create.container != subscribe.container)
                        return null;
                    var createResult = new CreateEntities {
                        container   = create.container,
                        entities    = FilterEntities(subscribe.filter, create.entities),
                        keyName     = create.keyName   
                    };
                    return createResult;
                
                case TaskType.upsert:
                    if (!subscribe.changes.Contains(Change.upsert))
                        return null;
                    var upsert = (UpsertEntities) task;
                    if (upsert.container != subscribe.container)
                        return null;
                    var upsertResult = new UpsertEntities {
                        container   = upsert.container,
                        entities    = FilterEntities(subscribe.filter, upsert.entities),
                        keyName     = upsert.keyName
                    };
                    return upsertResult;
                
                case TaskType.delete:
                    if (!subscribe.changes.Contains(Change.delete))
                        return null;
                    var delete = (DeleteEntities) task;
                    if (subscribe.container != delete.container)
                        return null;
                    // todo apply filter
                    return task;
                
                case TaskType.patch:
                    if (!subscribe.changes.Contains(Change.patch))
                        return null;
                    var patch = (PatchEntities) task;
                    if (subscribe.container != patch.container)
                        return null;
                    // todo apply filter
                    return task;
                
                default:
                    return null;
            }
        }
        
        private List<JsonValue> FilterEntities (FilterOperation filter, List<JsonValue> entities)    
        {
            if (filter == null)
                return entities;
            var jsonFilter      = new JsonFilter(filter); // filter can be reused
            var result          = new List<JsonValue>();

            for (int n = 0; n < entities.Count; n++) {
                var value   = entities[n];
                if (jsonEvaluator.Filter(value.json, jsonFilter)) {
                    result.Add(value);
                }
            }
            return result;
        }
    }
}