﻿// ﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Text;
using System;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Provide extension methods to optimize <see cref="Entity"/> modifications.<br/>
/// <c>Add()</c> and <c>Remove()</c> cause only none or one structural change.   
/// </summary>
public static partial class EntityExtensions
{
#region get component type indexes
    internal static Span<int> GetTypes<T1>(Span<int> components)
        where T1 : struct, IComponent
    {
        components[0] = StructHeap<T1>.StructIndex;
        return components;
    }
    
    internal static Span<int> GetTypes<T1, T2>(Span<int> components)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        components[0] = StructHeap<T1>.StructIndex;
        components[1] = StructHeap<T2>.StructIndex;
        return components;
    }
    
    internal static Span<int> GetTypes<T1, T2, T3>(Span<int> components)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        components[0] = StructHeap<T1>.StructIndex;
        components[1] = StructHeap<T2>.StructIndex;
        components[2] = StructHeap<T3>.StructIndex;
        return components;
    }
    
    internal static Span<int> GetTypes<T1, T2, T3, T4>(Span<int> components)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        components[0] = StructHeap<T1>.StructIndex;
        components[1] = StructHeap<T2>.StructIndex;
        components[2] = StructHeap<T3>.StructIndex;
        components[3] = StructHeap<T4>.StructIndex;
        return components;
    }
    
    internal static Span<int> GetTypes<T1, T2, T3, T4, T5>(Span<int> components)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
        where T5 : struct, IComponent
    {
        components[0] = StructHeap<T1>.StructIndex;
        components[1] = StructHeap<T2>.StructIndex;
        components[2] = StructHeap<T3>.StructIndex;
        components[3] = StructHeap<T4>.StructIndex;
        components[4] = StructHeap<T5>.StructIndex;
        return components;
    }
    #endregion


#region assign components
    internal static void AssignComponents<T1>(
        Archetype   archetype,
        int         compIndex,
        in T1       component1)
            where T1 : struct, IComponent
    {
        var heapMap = archetype.heapMap;
        ((StructHeap<T1>)heapMap[StructHeap<T1>.StructIndex]).components[compIndex] = component1;
    }

    internal static void AssignComponents<T1, T2>(
        Archetype   archetype,
        int         compIndex,
        in T1       component1,
        in T2       component2)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
    {
        var heapMap = archetype.heapMap;
        ((StructHeap<T1>)heapMap[StructHeap<T1>.StructIndex]).components[compIndex] = component1;
        ((StructHeap<T2>)heapMap[StructHeap<T2>.StructIndex]).components[compIndex] = component2;
    }

    internal static void AssignComponents<T1, T2, T3>(
        Archetype   archetype,
        int         compIndex,
        in T1       component1,
        in T2       component2,
        in T3       component3)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
    {
        var heapMap = archetype.heapMap;
        ((StructHeap<T1>)heapMap[StructHeap<T1>.StructIndex]).components[compIndex] = component1;
        ((StructHeap<T2>)heapMap[StructHeap<T2>.StructIndex]).components[compIndex] = component2;
        ((StructHeap<T3>)heapMap[StructHeap<T3>.StructIndex]).components[compIndex] = component3;
    }

    internal static void AssignComponents<T1, T2, T3, T4>(
        Archetype   archetype,
        int         compIndex,
        in T1       component1,
        in T2       component2,
        in T3       component3,
        in T4       component4)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
    {
        var heapMap = archetype.heapMap;
        ((StructHeap<T1>)heapMap[StructHeap<T1>.StructIndex]).components[compIndex] = component1;
        ((StructHeap<T2>)heapMap[StructHeap<T2>.StructIndex]).components[compIndex] = component2;
        ((StructHeap<T3>)heapMap[StructHeap<T3>.StructIndex]).components[compIndex] = component3;
        ((StructHeap<T4>)heapMap[StructHeap<T4>.StructIndex]).components[compIndex] = component4;
    }

    internal static void AssignComponents<T1, T2, T3, T4, T5>(
        Archetype   archetype,
        int         compIndex,
        in T1       component1,
        in T2       component2,
        in T3       component3,
        in T4       component4,
        in T5       component5)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
    {
        var heapMap = archetype.heapMap;
        ((StructHeap<T1>)heapMap[StructHeap<T1>.StructIndex]).components[compIndex] = component1;
        ((StructHeap<T2>)heapMap[StructHeap<T2>.StructIndex]).components[compIndex] = component2;
        ((StructHeap<T3>)heapMap[StructHeap<T3>.StructIndex]).components[compIndex] = component3;
        ((StructHeap<T4>)heapMap[StructHeap<T4>.StructIndex]).components[compIndex] = component4;
        ((StructHeap<T5>)heapMap[StructHeap<T5>.StructIndex]).components[compIndex] = component5;
    }
    #endregion


#region add components
    private static void StashAddComponents(EntityStoreBase store, Span<int> addComponents, Archetype oldType, int oldCompIndex)
    {
        if (store.ComponentAdded == null) {
            return;
        }
        var oldHeapMap  = oldType.heapMap;
        foreach (var addTypeIndex in addComponents)
        {
            var oldHeap = oldHeapMap[addTypeIndex];
            if (oldHeap == null) {
                continue;
            }
            oldHeap.StashComponent(oldCompIndex);
        }
    }
    
    private static void SendAddEvents(Entity entity, Span<int> addComponents, Archetype newType, Archetype oldType)
    {
        var store = entity.store;
        // --- tag event
        var tagsChanged = store.TagsChanged;
        if (tagsChanged != null && !newType.tags.bitSet.Equals(oldType.Tags.bitSet)) {
            tagsChanged(new TagsChanged(store, entity.Id, newType.tags, oldType.Tags));
        }
        // --- component events 
        var componentAdded = store.ComponentAdded;
        if (componentAdded == null) {
            return;
        }
        var oldHeapMap  = oldType.heapMap;
        var id          = entity.Id;
        foreach (var addTypeIndex in addComponents)
        {
            var oldHeap     = oldHeapMap[addTypeIndex];
            var action      = oldHeap == null ? ComponentChangedAction.Add : ComponentChangedAction.Update;
            componentAdded(new ComponentChanged (store, id, action, addTypeIndex, oldHeap));
        }
    }
    #endregion


#region remove components
    private static void StashRemoveComponents(EntityStoreBase store, Span<int> removeComponents, Archetype oldType, int oldCompIndex)
    {
        if (store.ComponentRemoved == null) {
            return;
        }
        var oldHeapMap = oldType.heapMap;
        foreach (var removeTypeIndex in removeComponents)
        {
            var oldHeap = oldHeapMap[removeTypeIndex];
            oldHeap?.StashComponent(oldCompIndex);
        }
    }
    
    private static void SendRemoveEvents(Entity entity, Span<int> removeComponents, Archetype newType, Archetype oldType)
    {
        var store = entity.store;
        // --- tag event
        var tagsChanged = store.TagsChanged;
        if (tagsChanged != null && !newType.tags.bitSet.Equals(oldType.Tags.bitSet)) {
            tagsChanged(new TagsChanged(store, entity.Id, newType.tags, oldType.Tags));
        }
        // --- component events 
        var componentRemoved = store.ComponentRemoved;
        if (componentRemoved == null) {
            return;
        }
        var oldHeapMap = oldType.heapMap;
        var id          = entity.Id;
        foreach (var removeTypeIndex in removeComponents)
        {
            var oldHeap = oldHeapMap[removeTypeIndex];
            if (oldHeap == null) {
                continue;
            }
            componentRemoved(new ComponentChanged (store, id, ComponentChangedAction.Remove, removeTypeIndex, oldHeap));
        }
    }
    #endregion


#region set components

    private static void CheckComponents(in Entity entity, Span<int> components, Archetype type, int compIndex)
    {
        foreach (var index in components) {
            if (type.componentTypes.bitSet.Has(index)) {
                continue;
            }
            throw MissingComponentException(entity, components, type);
        }
        if (entity.store.ComponentAdded == null) {
            return;
        }
        var heapMap = type.heapMap;
        foreach (var structIndex in components) {
            heapMap[structIndex].StashComponent(compIndex);
        }
    }
    
    private static MissingComponentException MissingComponentException(in Entity entity, Span<int> components, Archetype type)
    {
        bool isFirst = true;
        var sb = new StringBuilder();
        sb.Append("entity ");
        EntityUtils.EntityToString(entity.Id, type, sb);
        
        var schemaComponents = EntityStore.GetEntitySchema().components;
        sb.Append(" - missing: [");
        foreach (var index in components) {
            if (type.componentTypes.bitSet.Has(index)) {
                continue;
            }
            if (isFirst) {
                isFirst = false;
            } else {
                sb.Append(", ");
            }
            sb.Append(schemaComponents[index].Name);
        }
        sb.Append(']');
        return new MissingComponentException(sb.ToString());
    }
    
    private static void SendSetEvents(Entity entity, Span<int> components, Archetype type)
    {
        var store = entity.store;
        var componentAdded = store.ComponentAdded;
        if (componentAdded == null) {
            return;
        }
        var heapMap = type.heapMap;
        var id      = entity.Id;
        foreach (var structIndex in components) {
            componentAdded(new ComponentChanged (store, id, ComponentChangedAction.Update, structIndex, heapMap[structIndex]));
        }
    }
    #endregion
}

/// <summary>
/// Is thrown when calling <c>Entity.Set()</c> on an entity missing the specified components.
/// </summary>
public class MissingComponentException : Exception
{
    internal MissingComponentException(string message) : base (message) { }
}