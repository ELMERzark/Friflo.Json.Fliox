﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.Hub.Host.Auth;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;

namespace Friflo.Json.Fliox.Hub.Host.Event
{
    public interface IEventReceiver {
        bool        IsOpen ();
        bool        IsRemoteTarget ();
        Task<bool>  ProcessEvent(ProtocolEvent ev);
    }
    
    /// <summary>
    /// An <see cref="EventDispatcher"/> is used to enable Pub-Sub.
    /// </summary>
    /// <remarks>
    /// If assigned to <see cref="FlioxHub.EventDispatcher"/> the <see cref="FlioxHub"/> send
    /// push events to clients for database changes and messages these clients have subscribed. <br/>
    /// In case of remote database connections <b>WebSockets</b> are used to send push events to clients.
    /// </remarks> 
    public sealed class EventDispatcher : IDisposable
    {
        private  readonly   SharedEnv                                       sharedEnv;
        private  readonly   JsonEvaluator                                   jsonEvaluator;
        //
        /// key: <see cref="EventSubClient.clientId"/>
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubClient>   subClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>                     SubClients  => subClients.Values;
        
        /// <summary> Subset of <see cref="subClients"/> eligible for sending events. Either they
        /// are <see cref="EventSubClient.Connected"/> or they <see cref="EventSubClient.queueEvents"/> </summary> 
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubClient>   sendClients;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubClient>                     SendClients  => subClients.Values;
        //
        [DebuggerBrowsable(Never)]
        private  readonly   ConcurrentDictionary<JsonKey, EventSubUser>     subUsers;
        // ReSharper disable once UnusedMember.Local - expose Dictionary as list in Debugger
        private             ICollection<EventSubUser>                       SubUsers    => subUsers.Values;
        //
        /// <summary> exposed only for test assertions. <see cref="EventDispatcher"/> lives on Hub. <br/>
        /// If required its state (subscribed client) can be exposed by <see cref="DB.Monitor.ClientHits"/></summary>
        [DebuggerBrowsable(Never)]
        public              int                                             SubscribedClientsCount => subClients.Count;
        //
        internal readonly   bool                                            background;

        public   override   string                                          ToString() => $"subscribers: {subClients.Count}";

        private const string MissingEventReceiver = "subscribing events requires an eventReceiver. E.g a WebSocket as a target for push events.";

        public EventDispatcher (bool background, SharedEnv env = null) {
            sharedEnv       = env ?? SharedEnv.Default;
            jsonEvaluator   = new JsonEvaluator();
            subClients      = new ConcurrentDictionary<JsonKey, EventSubClient>(JsonKey.Equality);
            sendClients     = new ConcurrentDictionary<JsonKey, EventSubClient>(JsonKey.Equality);
            subUsers        = new ConcurrentDictionary<JsonKey, EventSubUser>(JsonKey.Equality);
            this.background = background;
        }

        public void Dispose() {
            jsonEvaluator.Dispose();
        }
        
        internal bool TryGetSubscriber(JsonKey key, out EventSubClient subClient) {
            return subClients.TryGetValue(key, out subClient);
        }
        
        /// used for test assertion
        public int QueuedEventsCount() {
            int count = 0;
            foreach (var pair in subClients) {
                count += pair.Value.QueuedEventsCount;
            }
            return count;
        }

        public async Task FinishQueues() {
            if (!background)
                return;
            var loopTasks = new List<Task>();
            foreach (var pair in subClients) {
                var subClient = pair.Value;
                subClient.FinishQueue();
                loopTasks.Add(subClient.triggerLoop);
            }
            await Task.WhenAll(loopTasks).ConfigureAwait(false);
        }
        
        // -------------------------------- add / remove subscriptions --------------------------------
        internal bool SubscribeMessage(
            string              database,
            SubscribeMessage    subscribe,
            User                user,
            in JsonKey          clientId,
            IEventReceiver      eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            var remove = subscribe.remove;
            if (remove.HasValue && remove.Value) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    return true;
                }
                databaseSubs.RemoveMessageSubscription(subscribe.name);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subClient.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddMessageSubscription(subscribe.name);
                return true;
            }
        }

        internal bool SubscribeChanges (
            string              database,
            SubscribeChanges    subscribe,
            User                user,
            in JsonKey          clientId,
            IEventReceiver      eventReceiver,
            out string          error)
        {
            if (eventReceiver == null) {
                error = MissingEventReceiver; 
                return false;
            }
            error = null;
            EventSubClient subClient;
            if (subscribe.changes.Count == 0) {
                if (!subClients.TryGetValue(clientId, out subClient))
                    return true;
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                    return true;
                databaseSubs.RemoveChangeSubscription(subscribe.container);
                RemoveEmptySubClient(subClient);
                return true;
            } else {
                subClient = GetOrCreateSubClient(user, clientId, eventReceiver);
                if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs)) {
                    databaseSubs = new DatabaseSubs(database);
                    subClient.databaseSubs.Add(database, databaseSubs);
                }
                databaseSubs.AddChangeSubscription(subscribe);
                return true;
            }
        }
        
        internal EventSubClient GetOrCreateSubClient(User user, in JsonKey clientId, IEventReceiver eventReceiver) {
            subClients.TryGetValue(clientId, out EventSubClient subClient);
            if (subClient != null)
                return subClient;
            if (!subUsers.TryGetValue(user.userId, out var subUser)) {
                subUser = new EventSubUser (user.userId, user.GetGroups());
                subUsers.TryAdd(user.userId, subUser);
            }
            subClient = new EventSubClient(sharedEnv, subUser, clientId, eventReceiver, background);
            subClients. TryAdd(clientId, subClient);
            sendClients.TryAdd(clientId, subClient);
            subUser.clients.Add(subClient);
            return subClient;
        }

        /// <summary>
        /// Don't remove empty subClient as the state of <see cref="EventSubClient.eventCounter"/> need to be preserved.
        /// </summary>
        private void RemoveEmptySubClient(EventSubClient subClient) {
            /* if (subClient.SubCount > 0)
                return;
            subClients.TryRemove(subClient.clientId, out _);
            var user = subClient.user;
            user.clients.Remove(subClient);
            if (user.clients.Count == 0) {
                subUsers.TryRemove(user.userId, out _);
            } */
        }
        
        internal void UpdateSubUserGroups(in JsonKey userId, IReadOnlyCollection<String> groups) {
            if (!subUsers.TryGetValue(userId, out var subUser))
                return;
            subUser.groups.Clear();
            if (groups != null) {
                subUser.groups.UnionWith(groups);
            }
        }

        // -------------------------- event distribution --------------------------------
        // use only for testing
        internal async Task SendQueuedEvents() {
            if (background) {
                throw new InvalidOperationException("must not be called, if using a background Tasks");
            }
            foreach (var pair in subClients) {
                var subClient = pair.Value;
                await subClient.SendEvents().ConfigureAwait(false);
            }
        }
        
        private void ProcessSubscriber(SyncRequest syncRequest, SyncContext syncContext) {
            ref JsonKey  clientId = ref syncContext.clientId;
            if (clientId.IsNull())
                return;
            
            if (!subClients.TryGetValue(clientId, out var subClient))
                return;
            var eventReceiver = syncContext.eventReceiver;
            if (eventReceiver != null && eventReceiver.IsRemoteTarget()) {
                if (subClient.UpdateTarget (eventReceiver)) {
                    // remote client is using a new connection (WebSocket) so add to queuingClients again
                    sendClients.TryAdd(subClient.clientId, subClient);
                }
            }
            
            var eventAck = syncRequest.eventAck;
            if (!eventAck.HasValue)
                return;
            if (!syncContext.authState.hubPermission.queueEvents)
                 return;
            int value =  eventAck.Value;
            subClient.AcknowledgeEvents(value);
        }
        
        private static bool HasSubscribableTask(List<SyncRequestTask> tasks) {
            foreach (var task in tasks) {
                switch (task.TaskType) {
                    case TaskType.message:
                    case TaskType.command:
                    case TaskType.create:
                    case TaskType.upsert:
                    case TaskType.delete:
                    case TaskType.patch:
                        return true;
                }
            }
            return false;
        }
        
        internal void EnqueueSyncTasks (SyncRequest syncRequest, SyncContext syncContext) {
            var syncTasks = syncRequest.tasks;
            ProcessSubscriber (syncRequest, syncContext);

            if (!HasSubscribableTask(syncTasks)) {
                return; // early out
            }
            using (var pooled = syncContext.ObjectMapper.Get()) {
                ObjectWriter writer     = pooled.instance.writer;
                var database            = syncContext.DatabaseName;
                writer.Pretty           = false;    // write sub's as one liner
                writer.WriteNullMembers = false;
                foreach (var pair in sendClients) {
                    EventSubClient subClient = pair.Value;
                    if (!subClient.queueEvents && !subClient.Connected) {
                        sendClients.TryRemove(subClient.clientId, out _);
                        continue;
                    }
                    if (!subClient.databaseSubs.TryGetValue(database, out var databaseSubs))
                        continue;
                    
                    List<SyncRequestTask>  eventTasks = null;
                    databaseSubs.AddEventTasks(syncTasks, subClient, ref eventTasks, jsonEvaluator);

                    if (eventTasks == null)
                        continue;
                    // mark change events for (change) tasks which are sent by the client itself
                    bool?   isOrigin    = syncContext.clientId.IsEqual(subClient.clientId) ? true : (bool?)null;
                    var     tasks       = eventTasks.ToArray();
                    var syncEvent = new SyncEvent { db = database, tasks = tasks, srcUserId = syncRequest.userId, isOrigin = isOrigin };
                    
                    if (SerializeRemoteEvents && subClient.IsRemoteTarget) {
                        SerializeRemoteEvent(syncEvent, eventTasks, writer);
                    }
                    subClient.EnqueueEvent(syncEvent);
                }
            }
        }
        
        internal static bool SerializeRemoteEvents = true; // set to false for development

        /// Optimization: For remote connections the tasks are serialized to <see cref="SyncEvent.tasksJson"/>.
        /// Benefits of doing this:
        /// - serialize a task only once for multiple targets
        /// - storing only a single byte[] for a task instead of a complex SyncRequestTask which is not used anymore
        private static void SerializeRemoteEvent(SyncEvent syncEvent, List<SyncRequestTask> tasks, ObjectWriter writer) {
            var tasksJson = new JsonValue [tasks.Count];
            syncEvent.tasksJson = tasksJson;
            for (int n = 0; n < tasks.Count; n++) {
                var task = tasks[n];
                if (task.json == null) {
                    task.json = new JsonValue(writer.WriteAsArray(task));
                }
                tasksJson[n] = task.json.Value;
            }
            tasks.Clear();
            syncEvent.tasks = null;
        }
    }
}