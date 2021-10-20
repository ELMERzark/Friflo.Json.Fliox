﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client.Internal;
using Friflo.Json.Fliox.DB.Client.Internal.Map;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Internal;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Models;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.DB.Remote;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;

#if !UNITY_5_3_OR_NEWER
[assembly: CLSCompliant(true)]
#endif

// ReSharper disable UseObjectOrCollectionInitializer
namespace Friflo.Json.Fliox.DB.Client
{
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [Fri.TypeMapper(typeof(FlioxClientMatcher))]
    public class FlioxClient : ITracerContext, IDisposable
    {
        // Keep all EntityStore fields in StoreIntern to enhance debugging overview.
        // Reason: EntityStore is extended by application and add multiple EntitySet fields.
        //         So internal fields are encapsulated in field intern.
        // ReSharper disable once InconsistentNaming
        internal            ClientIntern            _intern;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public              TypeStore               TypeStore       => _intern.typeStore;
        public              StoreInfo               StoreInfo       => new StoreInfo(_intern.syncStore, _intern.setByType); 
        public   override   string                  ToString()      => StoreInfo.ToString();
        public              IReadOnlyList<SyncTask> Tasks           => _intern.syncStore.appTasks;
        
        public              int                     GetSyncCount()  => _intern.syncCount;
        
        /// <summary>
        /// Instantiate an <see cref="FlioxClient"/> with a given <see cref="database"/> and an optional <see cref="typeStore"/>.
        ///
        /// Optimization note:
        /// In case an application create many (> 10) <see cref="FlioxClient"/> instances it should provide
        /// a <see cref="typeStore"/>. <see cref="TypeStore"/> instances are designed to be reused from multiple threads.
        /// Their creation is expensive compared to the instantiation of an <see cref="FlioxClient"/>. 
        /// </summary>
        public FlioxClient(DatabaseHub database, TypeStore typeStore, string userId, string clientId)
        {
            if (database  == null) throw new ArgumentNullException(nameof(database));
            if (typeStore == null) throw new ArgumentNullException(nameof(typeStore));
            
            ITracerContext tracer       = this;
            var eventTarget             = new EventTarget(this);
            var subscriptionProcessor   = new SubscriptionProcessor(this);
            _intern = new ClientIntern(null, typeStore, database, tracer, eventTarget, subscriptionProcessor);
            _intern.syncStore = new SyncStore();
            SetUserClient(userId, clientId);
            StoreUtils.InitEntitySets(this);
        }
        
        protected FlioxClient(DatabaseHub database, FlioxClient baseClient) {
            if (database  == null) throw new ArgumentNullException(nameof(database));
            if (baseClient == null) throw new ArgumentNullException(nameof(baseClient));
            if (baseClient._intern.database.extensionBase != null)
                throw new ArgumentException("database of baseStore must not be an extension database", nameof(baseClient));
            
            ITracerContext tracer       = this;
            var subscriptionProcessor   = new SubscriptionProcessor(this);
            _intern = new ClientIntern(baseClient, baseClient._intern.typeStore, database, tracer, null, subscriptionProcessor);
            _intern.syncStore = new SyncStore();
            StoreUtils.InitEntitySets(this);
        }
        
        public virtual void Dispose() {
            _intern.Dispose();
        }
        
        public static Type[] GetEntityTypes<TEntityStore> () where TEntityStore : FlioxClient {
            return StoreUtils.GetEntityTypes<TEntityStore>();
        }


        // --------------------------------------- public interface ---------------------------------------
        /// <summary>
        /// Process continuation of <see cref="ExecuteSync"/> on caller context.
        /// This ensures modifications to entities are applied on the same context used by the caller. 
        /// </summary>
        private const bool OriginalContext = true;
        
        // --- ExecuteTasksAsync / TryExecuteTasksAsync
        public async Task<ExecuteTasksResult> ExecuteTasksAsync() {
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var messageContext  = new MessageContext(_intern.pools, _intern.eventTarget, _intern.clientId);
            var response        = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(OriginalContext);
            
            var result = HandleSyncResponse(syncRequest, response, syncStore);
            if (!result.Success)
                throw new ExecuteTasksException(response.error, result.failed);
            messageContext.Release();
            return result;
        }
        
        public async Task<ExecuteTasksResult> TryExecuteTasksAsync() {
            var syncRequest     = CreateSyncRequest(out SyncStore syncStore);
            var messageContext  = new MessageContext(_intern.pools, _intern.eventTarget, _intern.clientId);
            var response        = await ExecuteSync(syncRequest, messageContext).ConfigureAwait(OriginalContext);
            
            var result = HandleSyncResponse(syncRequest, response, syncStore);
            messageContext.Release();
            return result;
        }
        
        private void AssertBaseStore() {
            if (_intern.client != null)
                throw new InvalidOperationException("only base store can set: userId, clientId & token");
        } 

        public void SetUserClient (string userId, string clientId) {
            SetUserClient(new JsonKey(userId), new JsonKey(clientId));
        }

        public void SetUserClient (in JsonKey userId, in JsonKey clientId) {
            AssertBaseStore();
            _intern.userId      = userId;
            var newClientId     = clientId;
            if (newClientId.IsEqual(_intern.clientId))
                return;
            if (!_intern.clientId.IsNull()) {
                _intern.database.RemoveEventTarget(_intern.clientId);
            }
            _intern.clientId    = newClientId;
            if (!_intern.clientId.IsNull()) {
                _intern.database.AddEventTarget(newClientId, _intern.eventTarget);
            }
        }

        internal void SetUser (JsonKey user) {
            AssertBaseStore();
            _intern.userId  = user;
        }
        
        public void SetToken (string token) {
            AssertBaseStore();
            _intern.token   = token;
        }

        // --- LogChanges
        public LogTask LogChanges() {
            var task = _intern.syncStore.CreateLog();
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.LogSetChangesInternal(task);
            }
            AddTask(task);
            return task;
        }
        
        
        // --- SubscribeAllChanges
        /// <summary>
        /// Subscribe to database changes of all <see cref="EntityContainer"/>'s with the given <see cref="changes"/>.
        /// By default these changes are applied to the <see cref="FlioxClient"/>.
        /// To react on specific changes use <see cref="SetSubscriptionHandler"/>.
        /// To unsubscribe from receiving change events set <see cref="changes"/> to null.
        /// </summary>
        public List<SyncTask> SubscribeAllChanges(IEnumerable<Change> changes) {
            AssertSubscriptionProcessor();
            var tasks = new List<SyncTask>();
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                // ReSharper disable once PossibleMultipleEnumeration
                var task = set.SubscribeChangesInternal(changes);
                tasks.Add(task);
            }
            return tasks;
        }
        
        /// <summary>
        /// Set a custom <see cref="SubscriptionProcessor"/> to enable reacting on specific database change or message (or command) events.
        /// E.g. notifying other application modules about created, updated, deleted or patches entities.
        /// To subscribe to database change events use <see cref="EntitySet{TKey,T}.SubscribeChanges"/>.
        /// The default <see cref="SubscriptionProcessor"/> apply all changes to the <see cref="FlioxClient"/> as they arrive.
        /// To subscribe to message events use <see cref="SubscribeMessage"/>.
        /// <br></br>
        /// In contrast to <see cref="SetSubscriptionHandler"/> this method provide additional possibilities by the
        /// given <see cref="SubscriptionProcessor"/>. These are:
        /// <para>
        ///   Defer processing of events by queuing them for later processing.
        ///   E.g. by doing nothing in an override of <see cref="SubscriptionProcessor.ProcessEvent"/>.  
        /// </para>
        /// <para>
        ///   Manipulation of the received <see cref="EventMessage"/> in an override of
        ///   <see cref="SubscriptionProcessor.ProcessEvent"/> before processing it.
        /// </para>
        /// </summary>
        public void SetSubscriptionProcessor(SubscriptionProcessor subscriptionProcessor) {
            _intern.subscriptionProcessor = subscriptionProcessor ?? throw new NullReferenceException(nameof(subscriptionProcessor));
        }
        
        /// <summary>
        /// Set a <see cref="SubscriptionHandler"/> which is called for all events received by the store.
        /// These events fall in two categories:
        /// <para>
        ///   1. change events.
        ///      To receive change events use <see cref="SubscribeAllChanges"/> or
        ///      <see cref="EntitySet{TKey,T}.SubscribeChanges"/> and its sibling methods.
        /// </para>
        /// <para>
        ///   2. message events.
        ///      To receive message events use <see cref="SubscribeMessage"/> or sibling methods.
        /// </para>
        /// </summary>
        public void SetSubscriptionHandler(SubscriptionHandler handler) {
            AssertSubscriptionProcessor();
            _intern.subscriptionHandler = handler;
        }

        // --- SendMessage
        public SendMessageTask SendMessage(string name) {
            var task = new SendMessageTask(name, new JsonUtf8(), _intern.jsonMapper.reader);
            _intern.syncStore.MessageTasks().Add(task);
            AddTask(task);
            return task;
        }
        
        public SendMessageTask SendMessage<TValue>(string name, TValue value) {
            var json    = _intern.jsonMapper.WriteAsArray(value);
            var task    = new SendMessageTask(name, new JsonUtf8(json), _intern.jsonMapper.reader);
            _intern.syncStore.MessageTasks().Add(task);
            AddTask(task);
            return task;
        }
        
        public SendMessageTask SendMessage<TValue>(TValue value) {
            var name = typeof(TValue).Name;
            return SendMessage(name, value);
        }
        
        public SendMessageTask<TResult> SendMessage<TValue, TResult>(string name, TValue value) {
            var json    = _intern.jsonMapper.WriteAsArray(value);
            var task    = new SendMessageTask<TResult>(name, new JsonUtf8(json), _intern.jsonMapper.reader);
            _intern.syncStore.MessageTasks().Add(task);
            AddTask(task);
            return task;
        }
        
        public SendMessageTask<TResult> SendMessage<TValue, TResult>(TValue value) {
            var name = typeof(TValue).Name;
            return SendMessage<TValue, TResult>(name, value);
        }
        
        
        // --- SubscribeMessage
        public SubscribeMessageTask SubscribeMessage<TValue>    (string name, MessageHandler<TValue> handler) {
            AssertSubscriptionProcessor();
            var callbackHandler = new GenericMessageCallback<TValue>(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        public SubscribeMessageTask SubscribeMessage<TValue>    (MessageHandler<TValue> handler) {
            var name = typeof(TValue).Name;
            return SubscribeMessage(name, handler);
        }
        
        public SubscribeMessageTask SubscribeMessage            (string name, MessageHandler handler) {
            AssertSubscriptionProcessor();
            var callbackHandler = new NonGenericMessageCallback(name, handler);
            var task            = _intern.AddCallbackHandler(name, callbackHandler);
            AddTask(task);
            return task;
        }
        
        // --- UnsubscribeMessage
        public SubscribeMessageTask UnsubscribeMessage<TValue>  (string name, MessageHandler<TValue> handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }
        
        public SubscribeMessageTask UnsubscribeMessage          (string name, MessageHandler handler) {
            var task = _intern.RemoveCallbackHandler(name, handler);
            AddTask(task);
            return task;
        }

        
        // ------------------------------------------- internals -------------------------------------------
        internal void AssertSubscriptionProcessor() {
            if (_intern.subscriptionProcessor != null)
                return;
            var msg = $"subscriptions require a {nameof(SubscriptionProcessor)} - {nameof(SetSubscriptionProcessor)}() before";
            throw new InvalidOperationException(msg);
        }
        
        public async Task CancelPendingSyncs() {
            foreach (var pair in _intern.pendingSyncs) {
                var messageContext = pair.Value;
                messageContext.Cancel();
            }
            await Task.WhenAll(_intern.pendingSyncs.Keys).ConfigureAwait(false);
        }
        
        public int GetPendingSyncsCount() {
            return _intern.pendingSyncs.Count;
        }
        
        private async Task<MsgResponse<SyncResponse>> ExecuteSync(SyncRequest syncRequest, MessageContext messageContext) {
            _intern.syncCount++;
            MsgResponse<SyncResponse>       response;
            Task<MsgResponse<SyncResponse>> task = null;
            var pendingSyncs = _intern.pendingSyncs;
            try {
                var database            = _intern.database; 
                syncRequest.database    = database.extensionName;
                var execDB              = database.extensionBase ?? database;
                task = execDB.ExecuteSync(syncRequest, messageContext);

                pendingSyncs.TryAdd(task, messageContext);
                response = await task.ConfigureAwait(false);
                pendingSyncs.TryRemove(task, out _);
            }
            catch (Exception e) {
                pendingSyncs.TryRemove(task, out _);
                var errorMsg = ErrorResponse.ErrorFromException(e).ToString();
                response = new MsgResponse<SyncResponse>(errorMsg);
            }
            return response;
        }
        
        // ReSharper disable once UnusedMember.Local
        private int GetSubscriptionCount() {
            int count = _intern.subscriptions.Count;
            foreach (var setPair in _intern.setByType) {
                var set = setPair.Value;
                if (set.GetSubscription() != null)
                    count++;
            }
            return count;
        }
        
        internal void AddTask(SyncTask task) {
            _intern.syncStore.appTasks.Add(task);
        }
        
        internal EntitySet GetEntitySet(string name) {
            if (_intern.setByName.TryGetValue(name, out var entitySet))
                return entitySet;
            throw new InvalidOperationException($"unknown EntitySet. name: {name}");
        }
        
        // TAG_NULL_REF
        internal EntitySetBase<T> GetEntitySetBase<T>() where T : class {
            Type entityType = typeof(T);
            if (_intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySetBase<T>)set;
            throw new InvalidOperationException($"unknown EntitySet<{entityType.Name}>");
        }

        internal EntitySet<TKey, T> GetEntitySet<TKey, T>() where T : class {
            Type entityType = typeof(T);
            if (_intern.setByType.TryGetValue(entityType, out EntitySet set))
                return (EntitySet<TKey, T>)set;
            throw new InvalidOperationException($"unknown EntitySet<{entityType.Name}>");
        }

        /// <summary>
        /// Returning current <see cref="ClientIntern.syncStore"/> as <see cref="syncStore"/> enables request handling
        /// in a worker thread while calling <see cref="SyncStore"/> methods from "main" thread.
        /// 
        /// If store has <see cref="ClientIntern.subscriptionProcessor"/> acknowledge received events to clear
        /// <see cref="Host.Event.EventSubscriber.sentEvents"/>. This avoids resending already received events on reconnect. 
        /// </summary>
        private SyncRequest CreateSyncRequest(out SyncStore syncStore) {
            syncStore = _intern.syncStore;
            syncStore.SetSyncSets(this);
            
            var tasks       = new List<SyncRequestTask>();
            var client      = _intern.client ?? this;
            var syncRequest = new SyncRequest {
                // database    = _intern.database.ExtensionName, 
                tasks       = tasks,
                userId      = client._intern.userId,
                clientId    = client._intern.clientId, 
                token       = client._intern.token
            };

            // see method docs
            if (client._intern.subscriptionProcessor != null) {
                syncRequest.eventAck = client._intern.lastEventSeq;
            }

            foreach (var setPair in _intern.setByType) {
                EntitySet set       = setPair.Value;
                var setInfo         = set.SetInfo;
                var curTaskCount    = tasks.Count;
                var syncSet         = set.SyncSet;
                // ReSharper disable once UseNullPropagation
                if (syncSet != null) {
                    syncSet.AddTasks(tasks);
                }
                AssertTaskCount(setInfo, tasks.Count - curTaskCount);
            }
            syncStore.AddTasks(tasks);
            
            // --- create new SyncStore and SyncSet's to collect future SyncTask's and executed via the next ExecuteTasksAsync() 
            foreach (var setPair in _intern.setByType) {
                EntitySet set = setPair.Value;
                set.ResetSync();
            }
            _intern.syncStore = new SyncStore();
            return syncRequest;
        }

        [Conditional("DEBUG")]
        private static void AssertTaskCount(in SetInfo setInfo, int taskCount) {
            int expect  = setInfo.tasks; 
            if (expect != taskCount)
                throw new InvalidOperationException($"Unexpected task.Count. expect: {expect}, got: {taskCount}");
        }

        private static void SetErrors(SyncResponse response, SyncStore syncStore) {
            var syncSets = syncStore.SyncSets;
            var createErrors = response.createErrors;
            if (createErrors != null) {
                foreach (var createError in createErrors) {
                    createError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[createError.Key];
                    syncSet.errorsCreate = createError.Value.errors;
                }
            }
            var upsertErrors = response.upsertErrors;
            if (upsertErrors != null) {
                foreach (var upsertError in upsertErrors) {
                    upsertError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[upsertError.Key];
                    syncSet.errorsUpsert = upsertError.Value.errors;
                }
            }
            var patchErrors = response.patchErrors;
            if (patchErrors != null) {
                foreach (var patchError in patchErrors) {
                    patchError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[patchError.Key];
                    syncSet.errorsPatch = patchError.Value.errors;
                }
            }
            var deleteErrors = response.deleteErrors;
            if (deleteErrors != null) {
                foreach (var deleteError in deleteErrors) {
                    deleteError.Value.SetInferredErrorFields();
                    var syncSet = syncSets[deleteError.Key];
                    syncSet.errorsDelete = deleteError.Value.errors;
                }
            }
        }
        
        /// Map <see cref="ContainerEntities.entities"/>, <see cref="ContainerEntities.notFound"/> and
        /// <see cref="ContainerEntities.errors"/> to <see cref="ContainerEntities.entityMap"/>.
        /// These properties are set by <see cref="RemoteHostDatabase.SetContainerResults"/>.
        private void GetContainerResults(SyncResponse response) {
            var results     = response.results;
            if (results == null)
                return;
            response.results = null;
            var resultMap   = response.resultMap = new Dictionary<string, ContainerEntities>(results.Count);
            foreach (var result in results) {
                resultMap.Add(result.container, result);
            }
            var processor = _intern.processor;
            foreach (var container in results) {
                string name         = container.container;
                if (!_intern.setByName.TryGetValue(name, out EntitySet set)) {
                    continue;
                }
                var keyName         = set.GetKeyName();
                var entityMap       = container.entityMap;
                var entities        = container.entities;
                var notFound        = container.notFound;
                var notFoundCount   = notFound?.Count ?? 0;
                var errors          = container.errors;
                var errorCount      = errors?.Count ?? 0;
                container.errors    = null;
                entityMap.Clear(); // Not necessary, be safe
                entityMap.EnsureCapacity(entities.Count + notFoundCount + errorCount);
                
                // --- entities
                foreach (var entity in entities) {
                    if (!processor.GetEntityKey(entity.json, keyName, out JsonKey key, out string errorMsg)) {
                        throw new InvalidOperationException($"GetEntityResults not found: {errorMsg}");
                    }
                    entityMap.Add(key, new EntityValue(entity.json));
                }
                entities.Clear();
                container.entities = null;
                
                // --- notFound
                if (notFound != null) {
                    foreach (var notFoundKey in notFound) {
                        entityMap.Add(notFoundKey, new EntityValue());
                    }
                    notFound.Clear();
                    container.notFound = null;
                }
                
                // --- errors
                if (errors == null || errors.Count == 0)
                    continue;
                foreach (var errorPair in errors) {
                    var key = errorPair.Key;
                    entityMap.Add(key, new EntityValue(errorPair.Value));
                }
                errors.Clear();
                container.errors = null;
            }
            results.Clear();
        }

        private ExecuteTasksResult HandleSyncResponse(SyncRequest syncRequest, MsgResponse<SyncResponse> response, SyncStore syncStore) {
            ExecuteTasksResult      syncResult;
            ErrorResponse   error       = response.error;
            var             syncSets    = syncStore.SyncSets;
            try {
                TaskErrorResult                         syncError;
                Dictionary<string, ContainerEntities>   containerResults;
                if (error == null) {
                    var result = response.success;
                    response.success.AssertResponse(syncRequest);
                    syncError = null;
                    var db = _intern.database; 
                    if (db is RemoteClientDatabase || db is ExtensionDatabase)
                        GetContainerResults(result);
                    containerResults = result.resultMap;
                    foreach (var containerResult in containerResults) {
                        ContainerEntities containerEntities = containerResult.Value;
                        var set = _intern.setByName[containerResult.Key];
                        set.SyncPeerEntities(containerEntities.entityMap);
                    }
                    SetErrors(result, syncStore);
                } else {
                    syncError = new TaskErrorResult {
                        message = error.message,
                        type    = TaskErrorResultType.SyncError
                    };
                    containerResults = new Dictionary<string, ContainerEntities>();
                }

                var tasks = syncRequest.tasks;
                for (int n = 0; n < tasks.Count; n++) {
                    var task = tasks[n];
                    TaskType    taskType = task.TaskType;
                    SyncTaskResult  result;
                    if (syncError == null) {
                        var results = response.success.tasks;
                        result = results[n];
                        var actual = result.TaskType;
                        if (actual != TaskType.error) {
                            if (taskType != actual) {
                                var msg = $"Expect task type of response matches request. index:{n} expect: {taskType} actual: {actual}";
                                throw new InvalidOperationException(msg);
                            }
                        }
                    } else {
                        result = syncError;
                    }

                    switch (taskType) {
                        case TaskType.reserveKeys:
                            var reserveKeys =     (ReserveKeys) task;
                            var syncSet = syncSets[reserveKeys.container];
                            syncSet.ReserveKeysResult(reserveKeys, result);
                            break;
                        case TaskType.create:
                            var create =            (CreateEntities) task;
                            syncSet = syncSets[create.container];
                            syncSet.CreateEntitiesResult(create, result);
                            break;
                        case TaskType.upsert:
                            var upsert =            (UpsertEntities) task;
                            syncSet = syncSets[upsert.container];
                            syncSet.UpsertEntitiesResult(upsert, result);
                            break;
                        case TaskType.read:
                            var readList =          (ReadEntities) task;
                            syncSet = syncSets[readList.container];
                            containerResults.TryGetValue(readList.container, out ContainerEntities entities);
                            syncSet.ReadEntitiesResult(readList, result, entities);
                            break;
                        case TaskType.query:
                            var query =             (QueryEntities) task;
                            syncSet = syncSets[query.container];
                            containerResults.TryGetValue(query.container, out ContainerEntities queryEntities);
                            syncSet.QueryEntitiesResult(query, result, queryEntities);
                            break;
                        case TaskType.patch:
                            var patch =             (PatchEntities) task;
                            syncSet = syncSets[patch.container];
                            syncSet.PatchEntitiesResult(patch, result);
                            break;
                        case TaskType.delete:
                            var delete =            (DeleteEntities) task;
                            syncSet = syncSets[delete.container];
                            syncSet.DeleteEntitiesResult(delete, result);
                            break;
                        case TaskType.message:
                            var message =           (SendMessage) task;
                            syncStore.MessageResult(message, result);
                            break;
                        case TaskType.subscribeChanges:
                            var subscribeChanges =  (SubscribeChanges) task;
                            syncSet = syncSets[subscribeChanges.container];
                            syncSet.SubscribeChangesResult(subscribeChanges, result);
                            break;
                        case TaskType.subscribeMessage:
                            var subscribeMessage =  (SubscribeMessage) task;
                            syncStore.SubscribeMessageResult(subscribeMessage, result);
                            break;
                    }
                }
                syncStore.LogResults();
            }
            finally {
                var failed = new List<SyncTask>();
                foreach (SyncTask task in syncStore.appTasks) {
                    task.AddFailedTask(failed);
                }
                syncResult = new ExecuteTasksResult(syncStore.appTasks, failed, error);
            }
            return syncResult;
        }
    }
    
    public static class StoreExtension
    {
        public static FlioxClient Store(this ITracerContext store) {
            return (FlioxClient)store;
        }
    }
}
