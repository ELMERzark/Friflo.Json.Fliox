﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Friflo.Fliox.Engine.ECS.Serialize;
using Friflo.Json.Fliox;

// ReSharper disable InlineTemporaryVariable
// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable MergeIntoLogicalPattern
// ReSharper disable ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

// This file contains implementation specific for storing DataEntity's.
// Loading and storing DataEntity's is implemented in EntityStore to enable declare all its fields private.
public partial class EntityStore
{
// --------------------------------------- Entity -> DataEntity ---------------------------------------
#region Entity -> DataEntity
    internal void EntityToDataEntity(Entity entity, DataEntity dataEntity, ComponentWriter writer, bool pretty)
    {
        ProcessChildren(dataEntity, nodes[entity.id]);
        
        // --- write components & scripts
        var jsonComponents = writer.Write(entity, pretty);
        if (!jsonComponents.IsNull()) {
            JsonUtils.FormatComponents(jsonComponents, ref writer.buffer);
            jsonComponents = new JsonValue(writer.buffer);
        }
        dataEntity.components = new JsonValue(jsonComponents); // create array copy for now
        
        ProcessTags(entity, dataEntity);
    }

    private static void ProcessTags(Entity entity, DataEntity dataEntity)
    {
        var tagCount    = entity.Tags.Count;
        var tags        = dataEntity.tags;
        if (tagCount == 0) {
            tags?.Clear();
        } else {
            if (tags == null) {
                tags = dataEntity.tags = new List<string>(tagCount);
            } else {
                tags.Clear();
            }
            foreach (var tag in entity.Tags) {
                tags.Add(tag.tagName);
            }
        }
        if (!entity.TryGetComponent<Unresolved>(out var unresolved)) {
            return;
        }
        var unresolvedTags = unresolved.tags;
        if (unresolvedTags != null) {
            tags ??= dataEntity.tags = new List<string>(unresolvedTags.Length);
            foreach (var tag in unresolvedTags) {
                tags.Add(tag);
            }
        }
    }

    private void ProcessChildren(DataEntity dataEntity, in EntityNode node)
    {
        var children = dataEntity.children;
        if (node.childCount > 0) {
            if (children == null) {
                children = dataEntity.children = new List<long>(node.childCount);
            } else {
                children.Clear();
            }
            foreach (var childId in node.ChildIds) {
                var pid = nodes[childId].pid;
                children.Add(pid);
            }
        } else {
            dataEntity.children?.Clear();
        }
    }
    #endregion
    
// --------------------------------------- DataEntity -> Entity ---------------------------------------
#region DataEntity -> Entity

    internal Entity DataEntityToEntity(DataEntity dataEntity, out string error, ComponentReader reader)
    {
        Entity entity;
        if (pidType == PidType.UsePidAsId) {
            entity = CreateFromDataEntityUsePidAsId(dataEntity);
        } else {
            entity = CreateFromDataEntityRandomPid (dataEntity);
        }
        error = reader.Read(dataEntity, entity, this);
        return entity;
    }
    
    private Entity CreateFromDataEntityRandomPid(DataEntity dataEntity)
    {
        // --- map pid to id
        var pid     = dataEntity.pid;
        var pidMap  = pid2Id;
        if (!pidMap.TryGetValue(pid, out int id)) {
            id = NewId();
            pidMap.Add(pid, id);
        }
        // --- map children pid's to id's
        var children    = CollectionsMarshal.AsSpan(dataEntity.children);
        var childCount  = children.Length;
        EnsureIdBufferCapacity(childCount);
        Span<int> ids   = new (idBuffer, 0, childCount);
        for (int n = 0; n < childCount; n++)
        {
            var childPid = children[n];
            if (!pidMap.TryGetValue(childPid, out int childId)) {
                childId = NewId();
                pidMap.Add(childPid, childId);
            }
            ids[n] = childId;
        }
        EnsureNodesLength(sequenceId);
        var entity  = CreateEntityNode(id, pid);

        if (ids.Length > 0) {
            UpdateEntityNodes(ids, children);
        }
        SetChildNodes(id, ids);
        return entity;
    }
    
    private Entity CreateFromDataEntityUsePidAsId(DataEntity dataEntity)
    {
        var pid = dataEntity.pid;
        if (pid < Static.MinNodeId || pid > int.MaxValue) {
            throw PidOutOfRangeException(pid, $"{nameof(DataEntity)}.{nameof(dataEntity.pid)}");
        }
        var id          = (int)pid;
        // --- use pid's as id's
        var maxPid      = id;
        var children    = CollectionsMarshal.AsSpan(dataEntity.children);
        var childCount  = children.Length; 
        EnsureIdBufferCapacity(childCount);
        Span<int> ids   = new (idBuffer, 0, childCount);
        for (int n = 0; n < childCount; n++)
        {
            var childId = children[n];
            if (childId < Static.MinNodeId || childId > int.MaxValue) {
                throw PidOutOfRangeException(childId, $"{nameof(DataEntity)}.{nameof(dataEntity.children)}");
            }
            ids[n] = (int)childId;
        }
        foreach (var childId in ids) {
            maxPid = Math.Max(maxPid, childId);
        }
        EnsureNodesLength(maxPid + 1);
        var entity  = CreateEntityNode(id, id);
        
        if (ids.Length > 0) {
            UpdateEntityNodes(ids, children);
        }
        SetChildNodes(id, ids);
        return entity;
    }
    
    private void EnsureIdBufferCapacity(int count) {
        if (idBuffer.Length >= count) {
            return;
        }
        ArrayUtils.Resize(ref idBuffer, Math.Max(2 * idBuffer.Length, count));
    }
    
    /// update EntityNode.pid of the child nodes
    private void UpdateEntityNodes(ReadOnlySpan<int> childIds, ReadOnlySpan<long> children)
    {
        var localNodes  = nodes;
        var count       = childIds.Length;
        for (int n = 0; n < count; n++) {
            var childId             = childIds[n];
            localNodes[childId].pid = children[n];
        }
    }
    #endregion
}
