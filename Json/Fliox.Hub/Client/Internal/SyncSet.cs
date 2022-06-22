﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Fliox.Hub.Client.Internal.Key;
using Friflo.Json.Fliox.Hub.Client.Internal.KeyEntity;
using Friflo.Json.Fliox.Hub.Protocol;
using Friflo.Json.Fliox.Hub.Protocol.Models;
using Friflo.Json.Fliox.Hub.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Fliox.Transform.Query.Ops;

// ReSharper disable RedundantArgumentDefaultValue
// ReSharper disable InconsistentNaming
namespace Friflo.Json.Fliox.Hub.Client.Internal
{
    internal abstract class SyncSetBase <T> : SyncSet where T : class
    {
        internal abstract void AddEntityPatches(PatchTask<T> patchTask, ICollection<T> entities);
        
        internal abstract QueryEntities     QueryEntities   (QueryTask<T>               query);
        internal abstract SubscribeChanges  SubscribeChanges(SubscribeChangesTask<T>    sub);
        internal abstract CreateEntities    CreateEntities  (CreateTask<T>              create, in CreateTaskContext context);
        internal abstract UpsertEntities    UpsertEntities  (UpsertTask<T>              upsert, in CreateTaskContext context);
        internal abstract PatchEntities     PatchEntities   (PatchTask<T>               patch);
        internal abstract PatchEntities     PatchEntities   (DetectPatchesTask<T>       detectPatches);
    }

    /// Multiple instances of this class can be created when calling <see cref="FlioxClient.SyncTasks"/> without
    /// awaiting the result. Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal sealed partial class SyncSet<TKey, T> : SyncSetBase<T> where T : class
    {
        private static readonly EntityKeyT<TKey, T> EntityKeyTMap   = EntityKey.GetEntityKeyT<TKey, T>();
        private static readonly KeyConverter<TKey>  KeyConvert      = KeyConverter.GetConverter<TKey>();

        // --- internal fields
        internal  readonly  EntitySet<TKey, T>  set;
        internal  readonly  List<SyncTask>      tasks  = new List<SyncTask>();

        internal  override  EntitySet           EntitySet => set;
        public    override  string              ToString()  => "";

        internal SyncSet(EntitySet<TKey, T> set) {
            this.set = set;
        }

        // --- Read
        internal ReadTask<TKey, T> Read() {
            var read = new ReadTask<TKey, T>(this);
            tasks.Add(read);
            return read;
        }

        // --- Query
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var query = new QueryTask<T>(filter, set.intern.store, this);
            tasks.Add(query);
            return query;
        }

        internal CloseCursorsTask CloseCursors(IEnumerable<string> cursors) {
            var closeCursor = new CloseCursorsTask(cursors, this);
            tasks.Add(closeCursor);
            return closeCursor;
        }

        // --- Aggregate
        internal CountTask<T> CountFilter(FilterOperation filter) {
            var aggregate   = new CountTask<T>(filter, this);
            tasks.Add(aggregate);
            return  aggregate;
        }

        // --- SubscribeChanges
        internal SubscribeChangesTask<T> SubscribeChangesFilter(Change change, FilterOperation filter) {
            var subscribeChanges = new SubscribeChangesTask<T>(this);
            var changes = ChangeFlags.ToList(change);
            subscribeChanges.Set(changes, filter);
            tasks.Add(subscribeChanges);
            return subscribeChanges;
        }

        // --- ReserveKeys
        internal ReserveKeysTask<TKey, T> ReserveKeys(int count) {
            var reserveKeys = new ReserveKeysTask<TKey,T>(count, this);
            tasks.Add(reserveKeys);
            return reserveKeys;
        }

        // --- Create
        internal CreateTask<T> Create(T entity) {
            if (set.intern.autoIncrement) {
                //  set.NewEntities().Add(entity);
                //  Autos().Add(entity);
                //  var create1 = new CreateTask<T>(new List<T>{entity}, set, this);
                //  CreateTasks().Add(create1);
                //  return create1;
            }
            var create  = new CreateTask<T>(new List<T>{entity}, set, this);
            var peer    = set.CreatePeer(entity);
            create.AddPeer(peer, PeerState.Created);
            tasks.Add(create);
            return create;
        }

        internal CreateTask<T> CreateRange(ICollection<T> entities) {
            var create = new CreateTask<T>(entities.ToList(), set, this);
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                create.AddPeer(peer, PeerState.Created);
            }
            tasks.Add(create);
            return create;
        }

        // --- Upsert
        internal UpsertTask<T> Upsert(T entity) {
            var upsert  = new UpsertTask<T>(new List<T>{entity}, set, this);
            var peer    = set.CreatePeer(entity);
            upsert.AddPeer(peer, PeerState.Updated);
            tasks.Add(upsert);
            return upsert;
        }

        internal UpsertTask<T> UpsertRange(ICollection<T> entities) {
            var upsert = new UpsertTask<T>(entities.ToList(), set, this);
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                upsert.AddPeer(peer, PeerState.Updated);
            }
            tasks.Add(upsert);
            return upsert;
        }

        // --- Delete
        internal DeleteTask<TKey, T> Delete(TKey key) {
            var keyList = new List<TKey>{ key };
            var delete  = new DeleteTask<TKey, T>(keyList, this);
            tasks.Add(delete);
            return delete;
        }

        internal DeleteTask<TKey, T> DeleteRange(ICollection<TKey> keys) {
            var keyList = keys.ToList();
            var delete  = new DeleteTask<TKey, T>(keyList, this);
            tasks.Add(delete);
            return delete;
        }

        internal DeleteAllTask<TKey, T> DeleteAll() {
            var deleteAll = new DeleteAllTask<TKey, T>(this);
            tasks.Add(deleteAll);
            return deleteAll;
        }
        
        // --- Patch
        // - assign patches
        internal PatchTask<T> Patch(MemberSelection<T> member) {
            var patchTask  = new PatchTask<T>(this, member);
            tasks.Add(patchTask);
            return patchTask;
        }

        // - detect patches
        internal void AddDetectPatches(DetectPatchesTask<T> detectPatchesTask) {
            tasks.Add(detectPatchesTask);
        }

        // Deprecated comment - preserve for now to remember history of Ref{TKey,T} and Tracer
        //   In case the given entity is already <see cref="Peer{T}.created"/> or <see cref="Peer{T}.updated"/> trace
        //   the entity to find changes in referenced entities in <see cref="Ref{TKey,T}"/> fields of the given entity.
        //   In these cases <see cref="Map.RefMapper{TKey,T}.Trace"/> add untracked entities (== have no <see cref="Peer{T}"/>)
        //   which is not already assigned)
        internal void DetectPeerPatches(Peer<T> peer, DetectPatchesTask<T> detectPatchesTask, ObjectMapper mapper) {
            if ((peer.state & (PeerState.Created | PeerState.Updated)) != 0) {
                // tracer.Trace(peer.Entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource == null)
                return;
            var entity  = peer.Entity;
            var patcher = set.intern.store._intern.ObjectPatcher();
            var diff    = patcher.differ.GetDiff(patchSource, entity, mapper.writer);
            if (diff == null)
                return;
            var patches     = detectPatchesTask.entityPatches;
            var patchList   = patcher.CreatePatches(diff, mapper);
            var id          = peer.id;
            SetNextPatchSource(peer, mapper); // todo next patch source need to be set on Synchronize()
            if (patches.TryGetValue(id, out var entityPatch)) {
                entityPatch.patches.AddRange(patchList);
            } else{
                entityPatch = new EntityPatch { id = id, patches = patchList };
                patches[id] = entityPatch;
            }
            detectPatchesTask.AddPatch(entityPatch, entity);
            // tracer.Trace(entity);
        }

        // ----------------------------------- create task methods -----------------------------------
        internal ReserveKeys ReserveKeys(ReserveKeysTask<TKey,T> reserveKeys) {
            return new ReserveKeys {
                container   = set.name,
                count       = reserveKeys.count,
                syncTask    = reserveKeys
            };
        }

        internal override CreateEntities CreateEntities(CreateTask<T> create, in CreateTaskContext context) {
            var creates = create.peers;
            var entries = new List<JsonValue>   (creates.Count);
            var keys    = new List<JsonKey>     (creates.Count);
            var writer  = context.mapper;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;

            foreach (var createPair in creates) {
                T entity    = createPair.Value.Entity;
                var json    = writer.WriteAsArray(entity);
                var entry   = new JsonValue(json);
                var id      = EntityKeyTMap.GetId(entity);
                entries.Add(entry);
                keys.Add(id);
            }
            return new CreateEntities {
                container       = set.name,
                keyName         = SyncKeyName(set.GetKeyName()),
                entities        = entries,
                entityKeys      = keys,
                reservedToken   = new Guid(), // todo
                syncTask        = create
            };

        }

        internal override UpsertEntities UpsertEntities(UpsertTask<T> upsert, in CreateTaskContext context) {
            var peers               = upsert.peers;
            var writer              = context.mapper;
            writer.Pretty           = set.intern.writePretty;
            writer.WriteNullMembers = set.intern.writeNull;
            var entries = new List<JsonValue>  (peers.Count);
            var keys    = new List<JsonKey>    (peers.Count);

            foreach (var upsertPair in peers) {
                T entity    = upsertPair.Value.Entity;
                var json    = writer.WriteAsArray(entity);
                var entry   = new JsonValue(json);
                var id      = EntityKeyTMap.GetId(entity);
                entries.Add(entry);
                keys.Add(id);
            }
            return new UpsertEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                entities    = entries,
                entityKeys  = keys,
                syncTask    = upsert  
            };
        }

        internal SyncRequestTask ReadEntities(ReadTask<TKey,T> read) {
            List<References> references = null;
            if (read.relations.subRelations.Count > 0) {
                references = new List<References>(read.relations.subRelations.Count);
                AddReferences(references, read.relations.subRelations);
            }
            var ids = new List<JsonKey>(read.result.Keys.Count);
            foreach (var key in read.result.Keys) {
                var id = KeyConvert.KeyToId(key);
                ids.Add(id);
            }
            return new ReadEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                isIntKey    = IsIntKey(set.IsIntKey()),
                ids         = ids,
                references  = references,
                syncTask    = read
            };
        }

        internal override QueryEntities QueryEntities(QueryTask<T> query) {
            var subRelations = query.relations.subRelations;
            List<References> references = null;
            if (subRelations.Count > 0) {
                references = new List<References>(subRelations.Count);
                AddReferences(references, subRelations);
            }
            var queryFilter = query.filter;
            if (query.filter is Filter filter) {
                queryFilter = filter.body;
            }
            var filterTree  = FilterToJson(queryFilter);
            return new QueryEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                isIntKey    = IsIntKey(set.IsIntKey()),
                filterTree  = filterTree,
                filter      = query.filterLinq,
                references  = references,
                limit       = query.limit,
                maxCount    = query.maxCount,
                cursor      = query.cursor,
                syncTask    = query 
            };
        }

        internal override AggregateEntities AggregateEntities(AggregateTask aggregate) {
            var aggregateFilter = aggregate.filter;
            if (aggregate.filter is Filter filter) {
                aggregateFilter = filter.body;
            }
            var filterTree  = FilterToJson(aggregateFilter);
            return new AggregateEntities {
                container   = set.name,
                type        = aggregate.Type,
            //  keyName     = SyncKeyName(set.GetKeyName()),
            //  isIntKey    = IsIntKey(set.IsIntKey()),
                filterTree  = filterTree,
                filter      = aggregate.filterLinq,
                syncTask    = aggregate 
            };
        }
        
        private JsonValue FilterToJson(FilterOperation filter) {
            using (var pooled = set.intern.store.ObjectMapper.Get()) {
                var writer      = pooled.instance.writer;
                var jsonFilter  = writer.Write(filter);
                return new JsonValue(jsonFilter);
            }
        }

        internal override CloseCursors CloseCursors(CloseCursorsTask closeCursor) {
            return new CloseCursors {
                container   = set.name,
                cursors     = closeCursor.cursors,
                syncTask    = closeCursor 
            };
        }
        
        internal override void AddEntityPatches(PatchTask<T> patchTask, ICollection<T> entities) {
            using (var pooled = set.intern.store.ObjectMapper.Get()) {
                var mapper          = pooled.instance;
                // todo performance: cache MemberAccess instances with members as key
                var members         = patchTask.selection.Members;
                var memberAccess    = patchTask.selection.GetMemberAccess();
                var memberAccessor  = new MemberAccessor(mapper.writer);
                var entityPatches   = patchTask.entityPatches;
                var taskPatches     = patchTask.patches;
                // taskPatches.Capacity= taskPatches.Count + entities.Count;    -> degrade performance

                foreach (var entity in entities) {
                    var id = EntityKeyTMap.GetId(entity);
                    if (!entityPatches.TryGetValue(id, out EntityPatch patch)) {
                        patch = new EntityPatch { id = id, patches = new List<JsonPatch>() };
                        entityPatches.Add(id, patch);
                    }
                    var patchInfo = new EntityPatchInfo<T>(patch, entity);
                    taskPatches.Add(patchInfo);
                    var key = KeyConvert.IdToKey(id);
                    if (set.TryGetPeerByKey(key, out var peer)) {
                        SetNextPatchSource(peer, mapper);
                    }
                    var patches         = patch.patches;
                    var selectResults   = memberAccessor.GetValues(entity, memberAccess);
                    int n = 0;
                    foreach (var path in members) {
                        var value = selectResults[n++].Json;
                        patches.Add(new PatchReplace { path = path, value = value });
                    }
                }
            }
        }
        
        internal override PatchEntities PatchEntities(PatchTask<T> patch) {
            var patches = patch.entityPatches;
            if (patch.entityPatches.Count == 0) {
                patch.state.Executed = true;
            }
            var list = new List<EntityPatch>(patches.Count);
            foreach (var pair in patches) { list.Add(pair.Value); }
            return new PatchEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                patches     = list,
                syncTask    = patch 
            };
        }
        
        internal override PatchEntities PatchEntities(DetectPatchesTask<T> detectPatches) {
            var patches = detectPatches.entityPatches;
            if (detectPatches.entityPatches.Count == 0) {
                detectPatches.state.Executed = true;
            }
            var list = new List<EntityPatch>(patches.Count);
            foreach (var pair in patches) { list.Add(pair.Value); }
            return new PatchEntities {
                container   = set.name,
                keyName     = SyncKeyName(set.GetKeyName()),
                patches     = list,
                syncTask    = detectPatches
            };
        }

        internal DeleteEntities DeleteEntities(DeleteTask<TKey,T> deleteTask) {
            var deletes = deleteTask.keys;
            var ids     = new List<JsonKey>(deletes.Count);
            foreach (var key in deletes) {
                var id = KeyConvert.KeyToId(key);
                ids.Add(id);
            }
            return new DeleteEntities {
                container   = set.name,
                ids         = ids,
                syncTask    = deleteTask 
            };
        }

        internal DeleteEntities DeleteAll(DeleteAllTask<TKey,T> deleteTask) {
           return new DeleteEntities {
                container   = set.name,
                all         = true,
                syncTask    = deleteTask 
            };
        }

        internal override SubscribeChanges SubscribeChanges(SubscribeChangesTask<T> sub) {
            var filterJson = FilterToJson(sub.filter);
            return new SubscribeChanges {
                container   = set.name,
                filter      = filterJson,
                changes     = sub.changes,
                syncTask    = sub 
            };
        }

        // ----------------------------------- helper methods -----------------------------------
        private static void AddReferences(List<References> references, SubRelations relations) {
            foreach (var readRefs in relations) {
                var queryReference = new References {
                    container   = readRefs.Container,
                    keyName     = SyncKeyName(readRefs.KeyName),
                    isIntKey    = IsIntKey(readRefs.IsIntKey),
                    selector    = readRefs.Selector
                };
                references.Add(queryReference);
                var subRefsMap = readRefs.SubRelations;
                if (subRefsMap.Count > 0) {
                    queryReference.references = new List<References>(subRefsMap.Count);
                    AddReferences(queryReference.references, subRefsMap);
                }
            }
        }

        private static void SetNextPatchSource(Peer<T> peer, ObjectMapper mapper) {
            var jsonArray   = mapper.writer.WriteAsArray(peer.Entity);
            var json        = new JsonValue(jsonArray);
            peer.SetNextPatchSource(mapper.Read<T>(json));
        }

        internal void SetTaskInfo(ref SetInfo info) {
            foreach (var syncTask in tasks) {
                switch (syncTask.TaskType) {
                    case TaskType.read:             info.read++;                break;
                    case TaskType.query:            info.query++;               break;
                    case TaskType.aggregate:        info.aggregate++;           break;
                    case TaskType.create:           info.create++;              break;
                    case TaskType.upsert:           info.upsert++;              break;
                    case TaskType.patch:            info.patch++;               break;
                    case TaskType.delete:           info.delete++;              break;
                    case TaskType.closeCursors:     info.closeCursors++;        break;
                    case TaskType.subscribeChanges: info.subscribeChanges++;    break;
                    case TaskType.reserveKeys:      info.reserveKeys++;         break;
                }
            }
            info.tasks =
                info.read               +
                info.query              +
                info.aggregate          +
                info.closeCursors       +
                info.create             +  // SetInfo.Any  (_autos) +
                info.upsert             +
                info.patch              +
                info.delete             +
                info.subscribeChanges   +
                info.reserveKeys;
        }
    }
}