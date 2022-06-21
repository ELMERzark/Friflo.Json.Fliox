﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Json.Fliox.Hub.Client.Internal;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Mapper.Map;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query;

// EntitySet & EntitySetBase<T> are not intended as a public API.
// These classes are declared here to simplify navigation to EntitySet<TKey, T>.
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    // --------------------------------------- EntitySet ---------------------------------------
    public abstract class EntitySet
    {
        internal  readonly  string          name;
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        internal            ChangeCallback  changeCallback;

        internal  abstract  SyncSet     SyncSet     { get; }
        internal  abstract  SetInfo     SetInfo     { get; }
        internal  abstract  Type        KeyType     { get; }
        internal  abstract  Type        EntityType  { get; }
        public    abstract  bool        WritePretty { get; set; }
        public    abstract  bool        WriteNull   { get; set; }

        internal static readonly QueryPath RefQueryPath = new RefQueryPath();
        
        internal  abstract  void                Init                    (FlioxClient store);
        internal  abstract  void                Reset                   ();
        internal  abstract  void                DetectSetPatchesInternal(DetectAllPatches task, ObjectMapper mapper);
        internal  abstract  void                SyncPeerEntities        (Dictionary<JsonKey, EntityValue> entities, ObjectMapper mapper);
        
        internal  abstract  void                ResetSync               ();
        internal  abstract  SyncTask            SubscribeChangesInternal(Change change);
        internal  abstract  SubscribeChanges    GetSubscription();
        internal  abstract  string              GetKeyName();
        internal  abstract  bool                IsIntKey();

        protected EntitySet(string name) {
            this.name = name;
        }
        
        internal const bool DefaultWritePretty   = false;
        internal const bool DefaultWriteNull     = false;
    }
    
    // --------------------------------------- EntitySetBase<T> ---------------------------------------
    public abstract class EntitySetBase<T> : EntitySet where T : class
    {
        private             HashSet<T>      newEntities;
        internal            HashSet<T>      NewEntities() => newEntities ?? (newEntities = new HashSet<T>(EntityEqualityComparer<T>.Instance));

        internal  abstract  SyncSetBase<T>  GetSyncSetBase  ();
        
        internal  abstract  Peer<T>         GetPeerById     (in JsonKey id);
        internal  abstract  Peer<T>         CreatePeer      (T entity);
        internal  abstract  JsonKey         GetEntityId     (T entity);
        
        protected EntitySetBase(string name) : base(name) { }
        
        internal static void ValidateKeyType(Type keyType) {
            var entityId        = EntityKey.GetEntityKey<T>();
            var entityKeyType   = entityId.GetKeyType();
            // TAG_NULL_REF
            var underlyingKeyType   = Nullable.GetUnderlyingType(keyType);
            if (underlyingKeyType != null) {
                keyType = underlyingKeyType;
            }
            if (keyType == entityKeyType)
                return;
            var type            = typeof(T);
            var entityKeyName   = entityId.GetKeyName();
            var name            = type.Name;
            var keyName         = keyType.Name;
            var error = $"key Type mismatch. {entityKeyType.Name} ({name}.{entityKeyName}) != {keyName} (EntitySet<{keyName},{name}>)";
            throw new InvalidTypeException(error);
        }
    }
}

namespace Friflo.Json.Fliox.Hub.Client
{
    // ---------------------------------- EntitySet<TKey, T> internals ----------------------------------
#if !UNITY_5_3_OR_NEWER
    [CLSCompliant(true)]
#endif
    public partial class EntitySet<TKey, T>
    {
        private SetInfo GetSetInfo() {
            var info = new SetInfo (name) { peers = _peers?.Count ?? 0 };
            syncSet?.SetTaskInfo(ref info);
            return info;
        }
        
        internal override void Init(FlioxClient store) {
            intern      = new SetIntern<TKey, T>(store);
        }
        
        internal override void Reset() {
            _peers?.Clear();
            intern.writePretty  = DefaultWritePretty;
            intern.writeNull    = DefaultWriteNull;
            syncSet             = null;
        }
        
        private static void SetEntityId (T entity, in JsonKey id) {
            EntityKeyTMap.SetId(entity, id);
        }
        
        internal override JsonKey GetEntityId (T entity) {
            return EntityKeyTMap.GetId(entity);
        }
        
        // ReSharper disable once UnusedMember.Local
        private static void SetEntityKey (T entity, TKey key) {
            EntityKeyTMap.SetKey(entity, key);
        }
        
        private static TKey GetEntityKey (T entity) {
            return EntityKeyTMap.GetKey(entity);
        }

        internal override void DetectSetPatchesInternal(DetectAllPatches allPatches, ObjectMapper mapper) {
            var set     = GetSyncSet();
            var task    = new DetectPatchesTask<T>(set);
            var peers   = Peers();
            foreach (var peerPair in peers) {
                Peer<T> peer = peerPair.Value;
                set.DetectPeerPatches(peer, task, mapper);
            }
            if (task.Patches.Count > 0) {
                allPatches.entitySetPatches.Add(task);
                set.AddDetectPatches(task);
                intern.store.AddTask(task);
            }
        }
        
        internal override Peer<T> CreatePeer (T entity) {
            var key   = GetEntityKey(entity);
            var peers = Peers();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                peer.SetEntity(entity);
                return peer;
            }
            var id = GetEntityId(entity);
            peer = new Peer<T>(entity, id);
            peers.Add(key, peer);
            return peer;
        }
        
        internal void DeletePeer (in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            var peers = Peers();
            peers.Remove(key);
        }
        
        [Conditional("DEBUG")]
        private static void AssertId(TKey key, in JsonKey id) {
            var expect = KeyConvert.KeyToId(key);
            if (!id.IsEqual(expect))
                throw new InvalidOperationException($"assigned invalid id: {id}, expect: {expect}");
        }
        
        internal bool TryGetPeerByKey(TKey key, out Peer<T> value) {
            var peers = Peers();
            return peers.TryGetValue(key, out value);
        }
        
        internal Peer<T> GetOrCreatePeerByKey(TKey key, JsonKey id) {
            var peers = Peers();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            if (id.IsNull()) {
                id = KeyConvert.KeyToId(key);
            } else {
                AssertId(key, id);
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }

        /// use <see cref="GetOrCreatePeerByKey"/> is possible
        internal override Peer<T> GetPeerById(in JsonKey id) {
            var key = KeyConvert.IdToKey(id);
            var peers = Peers();
            if (peers.TryGetValue(key, out Peer<T> peer)) {
                return peer;
            }
            peer = new Peer<T>(id);
            peers.Add(key, peer);
            return peer;
        }
        
        // ReSharper disable once UnusedMember.Local
        private bool TryGetPeerByEntity(T entity, out Peer<T> value) {
            var key     = EntityKeyTMap.GetKey(entity); 
            var peers   = Peers();
            return peers.TryGetValue(key, out value);
        }
        
        // --- EntitySet
        internal override void SyncPeerEntities(Dictionary<JsonKey, EntityValue> entities, ObjectMapper mapper) {
            var reader = mapper.reader;

            foreach (var entityPair in entities) {
                var id      = entityPair.Key;
                var value   = entityPair.Value;
                var error   = value.Error;
                var peer    = GetPeerById(id);
                if (error != null) {
                    // id & container are not serialized as they are redundant data.
                    // Infer their values from containing dictionary & EntitySet<>
                    error.id        = id;
                    error.container = name;
                    peer.error      = error;
                    continue;
                }

                peer.error = null;
                var json = value.Json;
                if (json.IsNull()) {
                    peer.SetPatchSourceNull();
                    continue;    
                }
                var entity = peer.NullableEntity;
                if (entity == null) {
                    entity = (T)intern.GetMapper().CreateInstance();
                    SetEntityId(entity, id);
                    peer.SetEntity(entity);
                }
                reader.ReadTo(json, entity);
                if (reader.Success) {
                    peer.SetPatchSource(reader.Read<T>(json));
                } else {
                    var entityError = new EntityError(EntityErrorType.ParseError, name, id, reader.Error.msg.ToString());
                    entities[id].SetError(entityError);
                }
            }
        }
        
        internal void DeletePeerEntities (ICollection<TKey> keys) {
            var peers = Peers();
            foreach (var key in keys) {
                peers.Remove(key);
            }
        }
        
        internal void PatchPeerEntities (List<Patch<TKey>> patches, ObjectMapper mapper) {
            var objectPatcher   = intern.store._intern.ObjectPatcher();
            var reader          = mapper.reader;
            foreach (var patch in patches) {
                var id      = patch.id;
                var peer    = GetPeerById(id);
                var entity  = peer.Entity;
                objectPatcher.ApplyPatches(entity, patch.patches, reader);
            }
        }

        internal override void ResetSync() {
            syncSet    = null;
        }
        
        internal override SyncTask SubscribeChangesInternal(Change change) {
            var all = Operation.FilterTrue;
            var task = GetSyncSet().SubscribeChangesFilter(change, all);
            intern.store.AddTask(task);
            return task;
        }
        
        internal override SubscribeChanges GetSubscription() {
            return intern.subscription;
        }
        
        internal override string GetKeyName() {
            return EntityKeyTMap.GetKeyName();
        }
        
        internal override bool IsIntKey() {
            return EntityKeyTMap.IsIntKey();
        }
    }
}
