﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Client.Internal.Map;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Transform;
using static System.Diagnostics.DebuggerBrowsableState;


// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client
{
    /// <summary>
    /// An EntitySet represents a collection (table) of entities (records) with a specific type <typeparamref name="T"/>. <br/>
    /// <br/>
    /// The methods of an <see cref="EntitySet{TKey,T}"/> enable to create, read, upsert or delete container entities. <br/>
    /// It also allows to subscribe to entity changes made by other database clients. <br/>
    /// <br/>
    /// <see cref="EntitySet{TKey,T}"/>'s are designed to be used as fields or properties inside a <see cref="FlioxClient"/>. <br/>
    /// The type <typeparamref name="T"/> of a container entity need to be a class containing a field or property used as its key
    /// usually named <b>id</b>. <br/>
    /// Supported <typeparamref name="TKey"/> types are:
    /// <see cref="string"/>, <see cref="long"/>, <see cref="int"/>, <see cref="short"/>, <see cref="byte"/>
    /// and <see cref="Guid"/>.
    /// <br/>
    /// The key type <typeparamref name="TKey"/> must match the <see cref="Type"/> used for the key field / property in an entity class.
    /// In case of a type mismatch a runtime exceptions is thrown.
    /// </summary>
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    [TypeMapper(typeof(EntitySetMatcher))]
    public sealed partial class EntitySet<TKey, T> : EntitySetBase<T>  where T : class
    {
    #region - Members    
        // Keep all utility related fields of EntitySet in SetIntern (intern) to enhance debugging overview.
        // Reason:  EntitySet<,> is used as field or property by an application which is mainly interested
        //          in following fields or properties while debugging:
        //          name, _peers & SetInfo
        internal            SetIntern<TKey, T>          intern;
        [DebuggerBrowsable(Never)]
        private             SyncSet<TKey, T>            syncSet;        //  Note: must be private by all means
        /// <summary> key: <see cref="Peer{T}.entity"/>.id </summary>
        private             Dictionary<TKey, Peer<T>>   _peers;         //  Note: must be private by all means
        // create _peers map on demand                                  //  Note: must be private by all means
        private             Dictionary<TKey, Peer<T>>   Peers() => _peers ?? (_peers = SyncSet.CreateDictionary<TKey,Peer<T>>());
        
        private             SyncSet<TKey, T>            GetSyncSet()    => syncSet ?? (syncSet = new SyncSet<TKey, T>(this));
        internal override   SyncSetBase<T>              GetSyncSetBase()=> syncSet;
        public   override   string                      ToString()      => SetInfo.ToString();

        internal override   SyncSet                     SyncSet         => syncSet;
        internal override   SetInfo                     SetInfo         => GetSetInfo();

        [DebuggerBrowsable(Never)] internal override    Type    KeyType      => typeof(TKey);
        [DebuggerBrowsable(Never)] internal override    Type    EntityType   => typeof(T);
        
        [DebuggerBrowsable(Never)] public   override    bool    WritePretty { get => intern.writePretty;   set => intern.writePretty = value; }
        [DebuggerBrowsable(Never)] public   override    bool    WriteNull   { get => intern.writeNull;     set => intern.writeNull   = value; }
        
        internal static readonly EntityKeyT<TKey, T>    EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private  static readonly KeyConverter<TKey>     KeyConvert      = KeyConverter.GetConverter<TKey>();
        #endregion
    
    // ----------------------------------------- public methods -----------------------------------------
    #region - initialize     
        /// constructor is called via <see cref="EntitySetMapper{T,TKey,TEntity}.CreateEntitySet"/> 
        internal EntitySet(string name) : base (name) {
            // ValidateKeyType(typeof(TKey)); // only required if constructor is public
        }
        #endregion
        
    #region - Cache    
        public bool TryGet (TKey key, out T entity) {
            var peers = Peers();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                entity = peer.NullableEntity;
                return true;
            }
            entity = null;
            return false;
        }
        
        public bool Contains (TKey key) {
            var peers = Peers();
            return peers.ContainsKey(key);
        }
        
        public List<T> ToList() {
            var peers   = Peers();
            var result  = new List<T>(peers.Count);
            foreach (var pair in peers) {
                var entity = pair.Value.NullableEntity;
                if (entity == null)
                    continue;
                result.Add(entity);
            }
            return result;
        }
        #endregion
        
    #region - Read
        public ReadTask<TKey, T> Read() {
            // ReadTasks<> are not added with intern.store.AddTask(task) as it only groups the tasks created via its
            // methods like: Find(), FindRange(), ReadRefTask() & ReadRefsTask().
            // A ReadTask<> its self cannot fail.
            return GetSyncSet().Read();
        }
        #endregion

    #region - Query
        public QueryTask<T> Query(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Query() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            var task = GetSyncSet().QueryFilter(op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<T> QueryByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.QueryByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().QueryFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        public QueryTask<T> QueryAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().QueryFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        
        public CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var task = GetSyncSet().CloseCursors(cursors);
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Aggregate
        public CountTask<T> Count(Expression<Func<T, bool>> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.Aggregate() filter must not be null. EntitySet: {name}");
            var op = Operation.FromFilter(filter, RefQueryPath);
            var task = GetSyncSet().CountFilter(op);
            intern.store.AddTask(task);
            return task;
        }

        // ReSharper disable once UnusedMember.Local - may be public in future 
        private CountTask<T> CountByFilter(EntityFilter<T> filter) {
            if (filter == null)
                throw new ArgumentException($"EntitySet.AggregateByFilter() filter must not be null. EntitySet: {name}");
            var task = GetSyncSet().CountFilter(filter.op);
            intern.store.AddTask(task);
            return task;
        }
        
        public CountTask<T> CountAll() {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().CountFilter(all);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - SubscribeChanges
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesFilter(Change change, Expression<Func<T, bool>> filter, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            intern.store.AssertSubscription();
            var op = Operation.FromFilter(filter);
            var task = GetSyncSet().SubscribeChangesFilter(change, op);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChangesByFilter(Change change, EntityFilter<T> filter, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (filter == null)  throw new ArgumentNullException(nameof(filter));
            intern.store.AssertSubscription();
            var task = GetSyncSet().SubscribeChangesFilter(change, filter.op);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        
        /// <summary>
        /// Subscribe to database changes of the related <see cref="EntityContainer"/> with the given <paramref name="change"/>.
        /// By default these changes are applied to the <see cref="EntitySet{TKey,T}"/>.
        /// To unsubscribe from receiving change events set <paramref name="change"/> to null.
        /// <seealso cref="FlioxClient.SetEventProcessor"/>
        /// </summary>
        public SubscribeChangesTask<T> SubscribeChanges(Change change, ChangeSubscriptionHandler<TKey, T> handler) {
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            intern.store.AssertSubscription();
            var all = Operation.FilterTrue;
            var task = GetSyncSet().SubscribeChangesFilter(change, all);
            intern.store.AddTask(task);
            changeCallback = new GenericChangeCallback<TKey,T>(handler);
            return task;
        }
        #endregion
        
    #region - ReserveKeys
        public ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var task = GetSyncSet().ReserveKeys(count);
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Create
        public CreateTask<T> Create(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Create() entity must not be null. EntitySet: {name}");
            var task = GetSyncSet().Create(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public CreateTask<T> CreateRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.CreateRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.CreateRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().CreateRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Upsert
        public UpsertTask<T> Upsert(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Upsert() entity must not be null. EntitySet: {name}");
            if (EntityKeyTMap.IsEntityKeyNull(entity))
                throw new ArgumentException($"EntitySet.Upsert() entity.id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Upsert(entity);
            intern.store.AddTask(task);
            return task;
        }
        
        public UpsertTask<T> UpsertRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.UpsertRange() entity must not be null. EntitySet: {name}");
            foreach (var entity in entities) {
                if (EntityKeyTMap.IsEntityKeyNull(entity))
                    throw new ArgumentException($"EntitySet.UpsertRange() entity.id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().UpsertRange(entities);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Delete
        public DeleteTask<TKey, T> Delete(T entity) {
            if (entity == null)
                throw new ArgumentException($"EntitySet.Delete() entity must not be null. EntitySet: {name}");
            var key = GetEntityKey(entity);
            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Delete(key);
            intern.store.AddTask(task);
            return task;
        }

        public DeleteTask<TKey, T> Delete(TKey key) {
            if (key == null)
                throw new ArgumentException($"EntitySet.Delete() id must not be null. EntitySet: {name}");
            var task = GetSyncSet().Delete(key);
            intern.store.AddTask(task);
            return task;
        }
        
        public DeleteTask<TKey, T> DeleteRange(ICollection<T> entities) {
            if (entities == null)
                throw new ArgumentException($"EntitySet.DeleteRange() entities must not be null. EntitySet: {name}");
            var keys = new List<TKey>(entities.Count);
            foreach (var entity in entities) {
                var key = GetEntityKey(entity);
                keys.Add(key);
            }
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().DeleteRange(keys);
            intern.store.AddTask(task);
            return task;
        }
        
        public DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            if (keys == null)
                throw new ArgumentException($"EntitySet.DeleteRange() ids must not be null. EntitySet: {name}");
            foreach (var key in keys) {
                if (key == null) throw new ArgumentException($"EntitySet.DeleteRange() id must not be null. EntitySet: {name}");
            }
            var task = GetSyncSet().DeleteRange(keys);
            intern.store.AddTask(task);
            return task;
        }
        
        public DeleteAllTask<TKey, T> DeleteAll() {
            var task = GetSyncSet().DeleteAll();
            intern.store.AddTask(task);
            return task;
        }
        #endregion

    #region - Patch creation
        /// <summary> Create <see cref="PatchTask{T}.Patches"/> for the fields of the passed <paramref name="selection"/>
        /// and the entities added with <see cref="PatchTask{T}.Add"/> subsequently </summary>
        /// <remarks> is applicable for tracked and untracked entities </remarks>
        public PatchTask<T> Patch(MemberSelectionBuilder<T> selection) {
            var memberSelection = new MemberSelection<T>();
            selection(memberSelection);
            var task = GetSyncSet().Patch(memberSelection);
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary> Create <see cref="PatchTask{T}.Patches"/> for the fields of the passed <paramref name="memberSelection"/>
        /// and the entities added with <see cref="PatchTask{T}.Add"/> subsequently </summary>
        /// <remarks> is applicable for tracked and untracked entities </remarks>
        public PatchTask<T> Patch(MemberSelection<T> memberSelection) {
            var task = GetSyncSet().Patch(memberSelection);
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Patch detection
        /// <summary> Detect <see cref="DetectPatchesTask.Patches"/> for all tracked entities </summary>
        /// <remarks> Consider using <see cref="DetectPatches(T)"/> <see cref="DetectPatches(ICollection{T})"/>
        /// as this method run detection on all tracked entities </remarks>
        public DetectPatchesTask DetectPatches() {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask(set);
            var peers   = Peers();
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                foreach (var peerPair in peers) {
                    Peer<T> peer = peerPair.Value;
                    set.DetectPeerPatches(peer, task, pooled.instance);
                }
            }
            intern.store.AddTask(task);
            return task;
        }

        /// <summary> Detect <see cref="DetectPatchesTask.Patches"/> for the passed tracked <paramref name="entity"/> </summary>
        public DetectPatchesTask DetectPatches(T entity) {
            if (entity == null)                             throw new ArgumentNullException(nameof(entity));
            var key     = EntityKeyTMap.GetKey(entity);
            if (KeyConvert.IsKeyNull(key))                  throw new ArgumentException($"entity key must not be null.");
            if (!TryGetPeerByKey(key, out var peer))        throw new ArgumentException($"entity is not tracked. key: {key}");
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask(set);
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                set.DetectPeerPatches(peer, task, pooled.instance);
            }
            intern.store.AddTask(task);
            return task;
        }
        
        /// <summary> Detect <see cref="DetectPatchesTask.Patches"/> for the passed tracked <paramref name="entities"/> </summary>
        public DetectPatchesTask DetectPatches(ICollection<T> entities) {
            if(entities == null)                            throw new ArgumentNullException(nameof(entities));
            int n       = 0;
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask(set);
            set.AddDetectPatches(task);
            using (var pooled = intern.store.ObjectMapper.Get()) {
                foreach (var entity in entities) {
                    if (entity == null)                         throw new ArgumentException($"entities[{n}] is null");
                    var key     = EntityKeyTMap.GetKey(entity);
                    if (KeyConvert.IsKeyNull(key))              throw new ArgumentException($"entity key must not be null. entities[{n}]");
                    if (!TryGetPeerByKey(key, out var peer))    throw new ArgumentException($"entity is not tracked. entities[{n}] key: {key}");
                    set.DetectPeerPatches(peer, task, pooled.instance);
                    n++;
                }
            }
            intern.store.AddTask(task);
            return task;
        }
        #endregion
        
    #region - Relation
        public RelationPath<TRef> RelationPath<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>        relation,
            Expression<Func<T, TRefKey>>    selector) where TRef : class
        {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(path);
        }
        
        public RelationsPath<TRef> RelationsPath<TRefKey, TRef>(
            EntitySet<TRefKey, TRef>                    relation,
            Expression<Func<T, IEnumerable<TRefKey>>>   selector) where TRef : class
        {
            string path = ExpressionSelector.PathFromExpression(selector, out _);
            return new RelationPath<TRef>(path);
        }
        #endregion
    }
}
