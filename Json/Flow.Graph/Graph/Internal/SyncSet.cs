﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Friflo.Json.Flow.Sync;
using Friflo.Json.Flow.Graph.Internal.Map;
using Friflo.Json.Flow.Transform;

namespace Friflo.Json.Flow.Graph.Internal
{
    internal abstract class SyncSet
    {
        internal    Dictionary<string, EntityError> createErrors = new Dictionary<string, EntityError>();
        internal    Dictionary<string, EntityError> updateErrors = new Dictionary<string, EntityError>();
        internal    Dictionary<string, EntityError> patchErrors  = new Dictionary<string, EntityError>();
        internal    Dictionary<string, EntityError> deleteErrors = new Dictionary<string, EntityError>();

        internal  abstract  void    AddTasks                (List<DatabaseTask> tasks);
        
        internal  abstract  void    CreateEntitiesResult    (CreateEntities     task, TaskResult result);
        internal  abstract  void    UpdateEntitiesResult    (UpdateEntities     task, TaskResult result);
        internal  abstract  void    ReadEntitiesListResult  (ReadEntitiesList   task, TaskResult result, ContainerEntities readEntities);
        internal  abstract  void    QueryEntitiesResult     (QueryEntities      task, TaskResult result, ContainerEntities queryEntities);
        internal  abstract  void    PatchEntitiesResult     (PatchEntities      task, TaskResult result);
        internal  abstract  void    DeleteEntitiesResult    (DeleteEntities     task, TaskResult result);
    }


    /// Multiple instances of this class can be created when calling EntitySet.Sync() without awaiting the result.
    /// Each instance is mapped to a <see cref="SyncRequest"/> / <see cref="SyncResponse"/> instance.
    internal class SyncSet<T> : SyncSet where T : Entity
    {
        // Note!
        // All fields must be private by all means to ensure that all scheduled tasks of a Sync() request managed
        // by this instance can be mapped to their task results safely.
        
        private readonly    EntitySet<T>                        set;
        private readonly    List<string>                        idsBuf       = new List<string>(); 
            
        private readonly    List<ReadTask<T>>                   reads        = new List<ReadTask<T>>();
        /// key: <see cref="QueryTask{T}.filterLinq"/> 
        private readonly    Dictionary<string, QueryTask<T>>    queries      = new Dictionary<string, QueryTask<T>>();
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   creates      = new Dictionary<string, PeerEntity<T>>();
        private readonly    List<WriteTask>                     createTasks  = new List<WriteTask>();
        
        /// key: <see cref="PeerEntity{T}.entity"/>.id
        private readonly    Dictionary<string, PeerEntity<T>>   updates      = new Dictionary<string, PeerEntity<T>>();
        private readonly    List<WriteTask>                     updateTasks  = new List<WriteTask>();

        /// key: entity id
        private readonly    Dictionary<string, EntityPatch>     patches      = new Dictionary<string, EntityPatch>();
        
        /// key: entity id
        private readonly    HashSet<string>                     deletes      = new HashSet   <string>();
        private readonly    List<DeleteTask>                    deleteTasks  = new List<DeleteTask>();

        internal SyncSet(EntitySet<T> set) {
            this.set = set;
        }
        
        internal bool AddCreate (PeerEntity<T> peer) {
            peer.assigned = true;
            if (!peer.created) {
                peer.created = true;                // sole place created set to true
                creates.Add(peer.entity.id, peer);  // sole place a peer (entity) is added
                return true;
            }
            return false;
        }
        
        internal void AddUpdate (PeerEntity<T> peer) {
            peer.assigned = true;
            if (!peer.updated) {
                peer.updated = true;                // sole place created set to true
                updates.Add(peer.entity.id, peer);  // sole place a peer (entity) is added
            }
        }
        
        // --- Read
        internal ReadTask<T> Read() {
            var read = new ReadTask<T>(set);
            reads.Add(read);
            return read;
        }
        
        // --- Query
        internal QueryTask<T> QueryFilter(FilterOperation filter) {
            var filterLinq = filter.Linq;
            if (queries.TryGetValue(filterLinq, out QueryTask<T> query))
                return query;
            query = new QueryTask<T>(filter);
            queries.Add(filterLinq, query);
            return query;
        }
        
        // --- Create
        internal CreateTask<T> Create(T entity) {
            var peer = set.CreatePeer(entity);
            AddCreate(peer);
            var create = new CreateTask<T>(peer.entity);
            createTasks.Add(create);
            return create;
        }
        
        internal CreateRangeTask<T> CreateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddCreate(peer);
            }
            var create = new CreateRangeTask<T>(entities);
            createTasks.Add(create);
            return create;
        }
        
        // --- Update
        internal UpdateTask<T> Update(T entity) {
            var peer = set.CreatePeer(entity);
            AddUpdate(peer);
            var update = new UpdateTask<T>(peer.entity);
            updateTasks.Add(update);
            return update;
        }
        
        internal UpdateRangeTask<T> UpdateRange(ICollection<T> entities) {
            foreach (var entity in entities) {
                var peer = set.CreatePeer(entity);
                AddUpdate(peer);
            }
            var update = new UpdateRangeTask<T>(entities);
            updateTasks.Add(update);
            return update;
        }
        
        // --- Delete
        internal DeleteTask<T> Delete(string id) {
            deletes.Add(id);
            var delete = new DeleteTask<T>(id);
            deleteTasks.Add(delete);
            return delete;
        }
        
        internal DeleteRangeTask<T> DeleteRange(ICollection<string> ids) {
            foreach (var id in ids) {
                deletes.Add(id);
            }
            var delete = new DeleteRangeTask<T>(ids);
            deleteTasks.Add(delete);
            return delete;
        }
        
        // --- Log changes -> create patches
        internal void LogSetChanges(Dictionary<string, PeerEntity<T>> peers, LogTask logTask) {
            foreach (var peerPair in peers) {
                PeerEntity<T> peer = peerPair.Value;
                GetEntityChanges(peer, logTask);
            }
        }

        internal void LogEntityChanges(T entity, LogTask logTask) {
            var peer = set.GetPeerById(entity.id);
            GetEntityChanges(peer, logTask);
        }

        /// In case the given entity was added via <see cref="Create"/> (peer.create != null) trace the entity to
        /// find changes in referenced entities in <see cref="Ref{T}"/> fields of the given entity.
        /// In these cases <see cref="RefMapper{T}.Trace"/> add untracked entities (== have no <see cref="PeerEntity{T}"/>)
        /// which is not already assigned) 
        private void GetEntityChanges(PeerEntity<T> peer, LogTask logTask) {
            if (peer.created) {
                set.intern.store.logTask = logTask;
                set.intern.tracer.Trace(peer.entity);
                return;
            }
            var patchSource = peer.PatchSource;
            if (patchSource != null) {
                var diff = set.intern.objectPatcher.differ.GetDiff(patchSource, peer.entity);
                if (diff == null)
                    return;
                var patchList = set.intern.objectPatcher.CreatePatches(diff);
                var entityPatch = new EntityPatch {
                    patches = patchList
                };
                var json = set.intern.jsonMapper.writer.Write(peer.entity);
                peer.SetNextPatchSource(set.intern.jsonMapper.Read<T>(json));
                patches[peer.entity.id] = entityPatch;
                logTask.AddPatch(this, peer.entity.id);
            }
        }

        internal override void AddTasks(List<DatabaseTask> tasks) {
            // --- CreateEntities
            if (creates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var createPair in creates) {
                    T entity = createPair.Value.entity;
                    var json = set.intern.jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new CreateEntities {
                    container = set.name,
                    entities = entries
                };
                tasks.Add(req);
                creates.Clear();
            }
            // --- UpdateEntities
            if (updates.Count > 0) {
                var entries = new Dictionary<string, EntityValue>();
                foreach (var updatePair in updates) {
                    T entity = updatePair.Value.entity;
                    var json = set.intern.jsonMapper.Write(entity);
                    var entry = new EntityValue(json);
                    entries.Add(entity.id, entry);
                }
                var req = new UpdateEntities {
                    container = set.name,
                    entities = entries
                };
                tasks.Add(req);
                updates.Clear();
            }
            // --- ReadEntities
            if (reads.Count > 0) {
                var readList = new ReadEntitiesList {
                    reads       = new List<ReadEntities>(),
                    container   = set.name
                };
                foreach (var read in reads) {
                    List<References> references = null;
                    if (read.refsTask.subRefs.Count >= 0) {
                        references = new List<References>(reads.Count);
                        AddReferences(references, read.refsTask.subRefs);
                    }
                    var req = new ReadEntities {
                        ids = read.idMap.Keys.ToHashSet(),
                        references = references
                    };
                    readList.reads.Add(req);
                }
                tasks.Add(readList);
            }
            // --- QueryEntities
            if (queries.Count > 0) {
                foreach (var queryPair in queries) {
                    QueryTask<T> query = queryPair.Value;
                    var subRefs = query.refsTask.subRefs;
                    List<References> references = null;
                    if (subRefs.Count > 0) {
                        references = new List<References>(subRefs.Count);
                        AddReferences(references, subRefs);
                    }
                    var req = new QueryEntities {
                        container   = set.name,
                        filter      = query.filter,
                        filterLinq  = query.filterLinq,
                        references  = references
                    };
                    tasks.Add(req);
                }
            }
            // --- PatchEntities
            if (patches.Count > 0) {
                var req = new PatchEntities {
                    container = set.name,
                    patches = new Dictionary<string, EntityPatch>(patches)
                };
                tasks.Add(req);
                patches.Clear();
            }
            // --- DeleteEntities
            if (deletes.Count > 0) {
                var req = new DeleteEntities {
                    container   = set.name,
                    ids         = new HashSet<string>(deletes)
                };
                tasks.Add(req);
                deletes.Clear();
            }
        }

        private void AddReferences(List<References> references, SubRefs refs) {
            foreach (var readRefs in refs) {
                var queryReference = new References {
                    container = readRefs.Container,
                    selector  = readRefs.Selector
                };
                references.Add(queryReference);
                var subRefsMap = readRefs.SubRefs;
                if (subRefsMap.Count > 0) {
                    queryReference.references = new List<References>(subRefsMap.Count);
                    AddReferences(queryReference.references, subRefsMap);
                }
            }
        }

        internal override void CreateEntitiesResult(CreateEntities task, TaskResult result) {
            CreateUpdateEntitiesResult(task.entities, result, createTasks, createErrors);
        }

        internal override void UpdateEntitiesResult(UpdateEntities task, TaskResult result) {
            CreateUpdateEntitiesResult(task.entities, result, updateTasks, updateErrors);
        }

        private void CreateUpdateEntitiesResult(
            Dictionary<string, EntityValue> entities,
            TaskResult                      result,
            List<WriteTask>                 writeTasks,
            Dictionary<string, EntityError> writeErrors)
        {
            if (result is TaskError taskError) {
                foreach (var writeTask in writeTasks) {
                    writeTask.state.SetError(new TaskErrorInfo(taskError));
                }
                return;
            }
            foreach (var entry in entities) {
                var id = entry.Key;
                if (writeErrors != null && writeErrors.TryGetValue(id, out EntityError error)) {
                    continue;
                }
                var peer = set.GetPeerById(id);
                peer.created = false;
                peer.updated = false;
                peer.SetPatchSource(set.intern.jsonMapper.Read<T>(entry.Value.Json));
            }
            foreach (var writeTask in writeTasks) {
                var entityErrorInfo = new TaskErrorInfo();
                idsBuf.Clear();
                writeTask.GetIds(idsBuf);
                foreach (var id in idsBuf) {
                    var value = entities[id];
                    if (value.Error != null)
                        entityErrorInfo.AddEntityError(value.Error);
                }
                if (entityErrorInfo.HasErrors) {
                    writeTask.state.SetError(entityErrorInfo);
                    continue;
                }
                writeTask.state.Synced = true;
            }
        }

        internal override void ReadEntitiesListResult(ReadEntitiesList taskList, TaskResult result, ContainerEntities readEntities) {
            if (result is TaskError taskError) {
                foreach (var read in reads) {
                    read.state.SetError(new TaskErrorInfo(taskError));
                }
                return;
            }
            var readListResult = (ReadEntitiesListResult) result;
            var expect = reads.Count;
            var actual = taskList.reads.Count;
            if (expect != actual) {
                throw new InvalidOperationException($"Expect reads.Count == result.reads.Count. expect: {expect}, actual: {actual}");
            }

            for (int i = 0; i < taskList.reads.Count; i++) {
                var task = taskList.reads[i];
                var read = reads[i];
                var readResult = readListResult.reads[i];
                ReadEntitiesResult(task, readResult, read, readEntities);
            }
        }

        private void ReadEntitiesResult(ReadEntities task, ReadEntitiesResult result, ReadTask<T> read, ContainerEntities readEntities) {
            // remove all requested peers from EntitySet which are not present in database
            foreach (var id in task.ids) {
                var value = readEntities.entities[id];
                if (value.Error != null) {
                    continue;
                }
                var json = value.Json;  // in case of RemoteClient json is "null"
                var isNull = json == null || json == "null";
                if (isNull)
                    set.DeletePeer(id);
            }

            var entityErrorInfo = new TaskErrorInfo();
            var readIds = read.idMap.Keys.ToList();
            foreach (var id in readIds) {
                var value = readEntities.entities[id];
                if (value.Error != null) {
                    entityErrorInfo.AddEntityError(value.Error);
                    continue;
                }
                var json = value.Json;  // in case of RemoteClient json is "null"
                if (json == null || json == "null") {
                    read.idMap[id] = null;
                } else {
                    var peer = set.GetPeerById(id);
                    read.idMap[id] = peer.entity;
                }
            }
            // A ReadTask is set to error if at least one of its JSON results has an error.
            if (entityErrorInfo.HasErrors) {
                read.state.SetError(entityErrorInfo);
                return;
            }
            read.state.Synced = true;
            AddReferencesResult(task.references, result.references, read.refsTask.subRefs);
        }
        
        internal override void QueryEntitiesResult(QueryEntities task, TaskResult result, ContainerEntities queryEntities) {
            var filterLinq = task.filterLinq;
            var query = queries[filterLinq];
            if (result is TaskError taskError) {
                query.state.SetError(new TaskErrorInfo(taskError));
                return;
            }
            var queryResult = (QueryEntitiesResult)result;
            var entityErrorInfo = new TaskErrorInfo();
            var entities = query.entities = new Dictionary<string, T>(queryResult.ids.Count);
            foreach (var id in queryResult.ids) {
                var value = queryEntities.entities[id];
                if (value.Error != null) {
                    entityErrorInfo.AddEntityError(value.Error);
                    continue;
                }
                var peer = set.GetPeerById(id);
                entities.Add(id, peer.entity);
            }
            if (entityErrorInfo.HasErrors) {
                query.state.SetError(entityErrorInfo);
                return;
            }
            AddReferencesResult(task.references, queryResult.references, query.refsTask.subRefs);
            query.state.Synced = true;
        }

        private void AddReferencesResult(List<References> references, List<ReferencesResult> referencesResult, SubRefs refs) {
            // in case (references != null &&  referencesResult == null) => no reference ids found for references 
            if (references == null || referencesResult == null)
                return;
            for (int n = 0; n < references.Count; n++) {
                References          reference    = references[n];
                ReferencesResult    refResult    = referencesResult[n];
                EntitySet           refContainer = set.intern.store._intern.setByName[refResult.container];
                ReadRefsTask        subRef       = refs[reference.selector];
                subRef.SetResult(refContainer, refResult.ids);

                var subReferences = reference.references;
                if (subReferences != null) {
                    var readRefs = subRef.SubRefs;
                    AddReferencesResult(subReferences, refResult.references, readRefs);
                }
            }
        }
        
        internal override void PatchEntitiesResult(PatchEntities task, TaskResult result) {
            if (result is TaskError) {
                /* foreach (var patchPair in patches) {
                    var patch = patchPair.Value;
                    // patch.taskError = taskError; todo
                } */
                return;
            }
            // var patchResult = (PatchEntitiesResult)result;
            var entityPatches = task.patches;
            foreach (var entityPatchPair in entityPatches) {
                var id = entityPatchPair.Key;
                var peer = set.GetPeerById(id);
                peer.SetPatchSource(peer.NextPatchSource);
                peer.SetNextPatchSourceNull();
            }
        }

        internal override void DeleteEntitiesResult(DeleteEntities task, TaskResult result) {
            if (result is TaskError taskError) {
                foreach (var deleteTask in deleteTasks) {
                    deleteTask.state.SetError(new TaskErrorInfo(taskError));
                }
                return;
            }
            foreach (var id in task.ids) {
                set.DeletePeer(id);
            }
            foreach (var deleteTask in deleteTasks) {
                var entityErrorInfo = new TaskErrorInfo();
                idsBuf.Clear();
                deleteTask.GetIds(idsBuf);
                foreach (var id in idsBuf) {
                    if (deleteErrors.TryGetValue(id, out EntityError error)) {
                        entityErrorInfo.AddEntityError(error);
                    }
                }
                if (entityErrorInfo.HasErrors) {
                    deleteTask.state.SetError(entityErrorInfo);
                    continue;
                }
                deleteTask.state.Synced = true;
            }
        }

        private static int Some(int count) { return count != 0 ? 1 : 0; }

        internal void SetTaskInfo(ref SetInfo info) {
            info.tasks =
                reads.Count         +
                queries.Count       +
                Some(creates.Count) +
                Some(updates.Count) +
                Some(patches.Count) +
                Some(deletes.Count);
            //
            info.reads      = reads.Count;
            info.queries    = queries.Count;
            info.create     = creates.Count;
            info.update     = updates.Count;
            info.patch      = patches.Count;
            info.delete     = deletes.Count;
            // info.readRefs   = readRefsMap.Count;
        }
    }
}