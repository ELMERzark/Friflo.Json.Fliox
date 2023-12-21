﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using static Friflo.Engine.ECS.StructInfo;

// Hard rule: this file MUST NOT use type: Entity

// ReSharper disable ConvertToAutoPropertyWithPrivateSetter
// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStoreBase
{
#region get / add archetype
    internal bool TryGetValue(ArchetypeKey searchKey, out ArchetypeKey archetypeKey) {
        return archSet.TryGetValue(searchKey, out archetypeKey);
    }
        
    private static Archetype GetArchetypeWith(EntityStoreBase store, Archetype current, int structIndex)
    {
        var searchKey = store.searchKey;
        searchKey.SetWith(current, structIndex);
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config          = GetArchetypeConfig(store);
        var schema          = Static.EntitySchema;
        var heaps           = current.Heaps();
        var componentTypes  = new List<ComponentType>(heaps.Length + 1);
        foreach (var heap in heaps) {
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        componentTypes.Add(schema.components[structIndex]);
        var archetype = Archetype.CreateWithComponentTypes(config, componentTypes, current.tags);
        AddArchetype(store, archetype);
        return archetype;
    }
    
    private static Archetype GetArchetypeWithout(EntityStoreBase store, Archetype archetype, int structIndex)
    {
        var searchKey = store.searchKey;
        searchKey.SetWithout(archetype, structIndex);
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var heaps           = archetype.Heaps();
        var componentCount  = heaps.Length - 1;
        var componentTypes  = new List<ComponentType>(componentCount);
        var config          = GetArchetypeConfig(store);
        var schema          = Static.EntitySchema;
        foreach (var heap in heaps) {
            if (heap.structIndex == structIndex)
                continue;
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        var result = Archetype.CreateWithComponentTypes(config, componentTypes, archetype.tags);
        AddArchetype(store, result);
        return result;
    }
    
    private static Archetype GetArchetypeWithTags(EntityStoreBase store, Archetype archetype, in Tags tags)
    {
        var heaps           = archetype.Heaps();
        var componentTypes  = new List<ComponentType>(heaps.Length);
        var config          = GetArchetypeConfig(store);
        var schema          = Static.EntitySchema;
        foreach (var heap in heaps) {
            componentTypes.Add(schema.components[heap.structIndex]);
        }
        var result = Archetype.CreateWithComponentTypes(config, componentTypes, tags);
        AddArchetype(store, result);
        return result;
    }
    
    internal static void AddArchetype (EntityStoreBase store, Archetype archetype)
    {
        if (store.archsCount == store.archs.Length) {
            var newLen = 2 * store.archs.Length;
            ArrayUtils.Resize(ref store.archs,     newLen);
        }
        if (archetype.archIndex != store.archsCount) {
            throw new InvalidOperationException($"invalid archIndex. expect: {store.archsCount}, was: {archetype.archIndex}");
        }
        store.archs[store.archsCount] = archetype;
        store.archsCount++;
        store.archSet.Add(archetype.key);
    }
    #endregion
    
    // ------------------------------------ add / remove component ------------------------------------
#region add / remove component
    internal static bool AddComponent<T>(
            int         id,
            int         structIndex,
        ref Archetype   archetype,  // possible mutation is not null
        ref int         compIndex,
        // ReSharper disable once RedundantAssignment - archIndex must be changed before send event
        ref int         archIndex,
        in  T           component)      where T : struct, IComponent
    {
        var         arch    = archetype;
        var         store   = arch.store;   
        bool        added;
        StructHeap  structHeap;
        
        if (arch != store.defaultArchetype)
        {
            structHeap = arch.heapMap[structIndex];
            if (structHeap != null) {
                // --- case: archetype contains the component type  => archetype remains unchanged
                added = false;
                goto AssignComponent;
            }
            // --- case: archetype doesn't contain component type   => change entity archetype
            // removed passing typeof(T) in commit:
            //   Engine - extract EntityStoreBase.AddComponentInternal() to prepare sending events for EntityStoreBase.AddComponent<>()
            //   https://github.com/friflo/Friflo.Json.Fliox/commit/f1cf0db5a59a961fc39c30918157678d82d3573e
            var newArchetype    = GetArchetypeWith(store, arch, structIndex);
            compIndex           = Archetype.MoveEntityTo(arch, id, compIndex, newArchetype);
            archetype           = arch = newArchetype;
            added               = true;
        } else {
            // --- case: entity is assigned to default archetype    => get archetype and add entity
            arch                = GetArchetype(store, arch.tags, structIndex);
            compIndex           = Archetype.AddEntity(arch, id);
            archetype           = arch;
            added               = true;
        }
        archIndex   = arch.archIndex;
        structHeap  = arch.heapMap[structIndex];
        
    AssignComponent:  // --- assign passed component value
        var heap    = (StructHeap<T>)structHeap;
        heap.chunks[compIndex / ChunkSize].components[compIndex % ChunkSize] = component;
        // Send event. See: SEND_EVENT notes
        store.componentAdded?.Invoke(new ComponentChangedArgs (id, ChangedEventAction.Add, structIndex));
        return added;
    }

    /*
    /// <remarks>
    /// Used to minimize method body size of <see cref="AddComponent{T}"/> by extracting all non generic processing
    /// to this method.<br/>
    /// This minimizes the code generated by the runtime for each specific generic type.
    /// </remarks>
    private bool AddComponentInternal(
        int             id,
        ref Archetype   archetype,  // possible mutation is not null
        ref int         compIndex,
        int             structIndex,
        out StructHeap  structHeap)
    {
        var arch        = archetype;

        if (arch != defaultArchetype) {
            structHeap = arch.heapMap[structIndex];
            if (structHeap != null) {
                // case: archetype contains the component type
                return false;
            }
            // --- change entity archetype
            // removed passing typeof(T) in commit:
            //   Engine - extract EntityStoreBase.AddComponentInternal() to prepare sending events for EntityStoreBase.AddComponent<>()
            //   https://github.com/friflo/Friflo.Json.Fliox/commit/f1cf0db5a59a961fc39c30918157678d82d3573e
            var newArchetype    = GetArchetypeWith(arch, structIndex);
            compIndex           = arch.MoveEntityTo(id, compIndex, newArchetype);
            archetype           = arch = newArchetype;
        } else {
            // --- add entity to archetype
            arch                = GetArchetype(arch.tags, structIndex);
            compIndex           = arch.AddEntity(id);
            archetype           = arch;
        }
        structHeap = arch.heapMap[structIndex];
        return true;
    } */
    
    internal static bool RemoveComponent(
            int         id,
        ref Archetype   archetype,    // possible mutation is not null
        ref int         compIndex,
        // ReSharper disable once RedundantAssignment - archIndex must be changed before send event
        ref int         archIndex,
            int         structIndex)
    {
        var arch    = archetype;
        var store   = arch.store;
        var heap    = arch.heapMap[structIndex];
        if (heap == null) {
            return false;
        }
        var newArchetype = GetArchetypeWithout(store, arch, structIndex);
        if (newArchetype == store.defaultArchetype) {
            int removePos = compIndex; 
            // --- update entity
            archetype   = store.defaultArchetype;
            compIndex   = 0;
            Archetype.MoveLastComponentsTo(arch, removePos);
        } else {
            // --- change entity archetype
            archetype   = newArchetype;
            compIndex   = Archetype.MoveEntityTo(arch, id, compIndex, newArchetype);
        }
        archIndex   = archetype.archIndex;
        // Send event. See: SEND_EVENT notes
        store.componentRemoved?.Invoke(new ComponentChangedArgs (id, ChangedEventAction.Remove, structIndex));
        return true;
    }
    #endregion
    
    // ------------------------------------ add / remove entity Tag ------------------------------------
#region add / remove tags

    internal static bool AddTags(
        EntityStoreBase store,
        in Tags         tags,
        int             id,
        ref Archetype   archetype,      // possible mutation is not null
        ref int         compIndex,
        ref int         archIndex)
    {
        var arch            = archetype;
        var archTagsValue   = arch.tags.bitSet.value;
        var tagsValue       = tags.bitSet.value;
        if (archTagsValue == tagsValue) {
            return false;
        }
        var searchKey = store.searchKey;
        searchKey.componentTypes    = arch.componentTypes;
        searchKey.tags.bitSet.value = archTagsValue | tagsValue;
        searchKey.CalculateHashCode();
        Archetype newArchetype;
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            newArchetype = archetypeKey.archetype;
        } else {
            newArchetype = GetArchetypeWithTags(store, arch, searchKey.tags);
        }
        if (arch != store.defaultArchetype) {
            archetype   = newArchetype;
            compIndex   = Archetype.MoveEntityTo(arch, id, compIndex, newArchetype);
        } else {
            compIndex   = Archetype.AddEntity(newArchetype, id);
            archetype   = newArchetype;
        }
        archIndex = archetype.archIndex;
        // Send event. See: SEND_EVENT notes
        store.tagsChanged?.Invoke(new TagsChangedArgs(id, tags));
        return true;
    }
    
    internal static bool RemoveTags(
        EntityStoreBase store,
        in Tags         tags,
        int             id,
        ref Archetype   archetype,      // possible mutation is not null
        ref int         compIndex,
        ref int         archIndex)
    {
        var arch            = archetype;
        var archTags        = arch.tags.bitSet.value;
        var archTagsRemoved = archTags & ~tags.bitSet.value;
        if (archTagsRemoved == archTags) {
            return false;
        }
        var searchKey = store.searchKey;
        searchKey.componentTypes    = arch.componentTypes;
        searchKey.tags.bitSet.value = archTagsRemoved;
        searchKey.CalculateHashCode();
        Archetype newArchetype;
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            newArchetype = archetypeKey.archetype;
        } else {
            newArchetype = GetArchetypeWithTags(store, arch, searchKey.tags);
        }
        if (newArchetype == store.defaultArchetype) {
            int removePos = compIndex; 
            // --- update entity
            compIndex   = 0;
            archetype   = store.defaultArchetype;
            Archetype.MoveLastComponentsTo(arch, removePos);
        } else {
            compIndex   = Archetype.MoveEntityTo(arch, id, compIndex, newArchetype);
            archetype   = newArchetype;
        }
        archIndex = archetype.archIndex;
        // Send event. See: SEND_EVENT notes
        store.tagsChanged?.Invoke(new TagsChangedArgs(id, tags));
        return true;
    }
    #endregion
}
