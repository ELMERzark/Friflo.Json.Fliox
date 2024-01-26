﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable InconsistentNaming
// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
// ReSharper disable ConvertConstructorToMemberInitializers
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

#pragma warning disable CS0618 // Type or member is obsolete


[Obsolete("Experimental")]
public struct EntityCommandBuffer
{
    private readonly    ComponentCommands[] _componentCommands;
    private readonly    EntityChanges       entityChanges;
    private             ComponentTypes      _changedComponents;
    
#region general methods
    public EntityCommandBuffer(EntityStore store)
    {
        var buffers         = store.GetCommandBuffers();
        entityChanges       = buffers.entityChanges;
        _componentCommands  = buffers.componentCommands;
    }
    
    public void Playback()
    {
        var componentCommands   = _componentCommands;
        var changedComponents   = _changedComponents;
        foreach (var componentType in changedComponents)
        {
            var commands = componentCommands[componentType.StructIndex];
            commands.UpdateComponentTypes(entityChanges);
        }
        MoveEntitiesToNewArchetypes();
        
        foreach (var componentType in changedComponents)
        {
            var commands = componentCommands[componentType.StructIndex];
            commands.ExecuteCommands(entityChanges);
        }
        Reset();
        entityChanges.store.ReturnCommandBuffers(componentCommands, entityChanges);
    }
    
    private void MoveEntitiesToNewArchetypes()
    {
        var store               = entityChanges.store;
        var nodes               = store.nodes;
        var defaultArchetype    = store.defaultArchetype;
        foreach (var (entityId, componentTypes) in entityChanges.entities)
        {
            ref var node        = ref nodes[entityId];
            var curArchetype    = node.Archetype;
            if (curArchetype.componentTypes.bitSet.value == componentTypes.bitSet.value) {
                continue;
            }
            var newArchetype = store.GetArchetype(componentTypes);
            if (curArchetype == defaultArchetype) {
                node.compIndex  = Archetype.AddEntity(newArchetype, entityId);
            } else {
                if (newArchetype == defaultArchetype) {
                    Archetype.MoveLastComponentsTo(curArchetype, node.compIndex);
                    node.compIndex = 0;
                } else {
                    node.compIndex  = Archetype.MoveEntityTo(curArchetype, entityId, node.compIndex, newArchetype);
                }
            }
            node.archetype  = newArchetype;
        }
    }
    
    private void Reset()
    {
        var commands = _componentCommands;
        foreach (var componentType in _changedComponents)
        {
            commands[componentType.StructIndex].commandCount = 0;
        }
        entityChanges.entities.Clear();
        _changedComponents = default;
    }
    #endregion
        
#region component
    public void AddComponent<T>(int entityId)
        where T : struct, IComponent
    {
        ChangeComponent<T>(default, entityId,ComponentChangedAction.Add);
    }
    
    public void AddComponent<T>(int entityId, in T component)
        where T : struct, IComponent
    {
        ChangeComponent(component,  entityId, ComponentChangedAction.Add);
    }
    
    public void SetComponent<T>(int entityId, in T component)
        where T : struct, IComponent
    {
        ChangeComponent(component,  entityId, ComponentChangedAction.Update);
    }
    
    public void RemoveComponent<T>(int entityId)
        where T : struct, IComponent
    {
        ChangeComponent<T>(default, entityId, ComponentChangedAction.Remove);
    }
    
    private void ChangeComponent<T>(in T component, int entityId, ComponentChangedAction change)
        where T : struct, IComponent
    {
        var structIndex = StructHeap<T>.StructIndex;
        _changedComponents.bitSet.SetBit(structIndex);
        var commands    = (ComponentCommands<T>)_componentCommands[structIndex];
        var count       = commands.commandCount; 
        if (count == commands.componentCommands.Length) {
            ArrayUtils.Resize(ref commands.componentCommands, 2 * count);
        }
        commands.commandCount   = count + 1;
        ref var command         = ref commands.componentCommands[count];
        command.change          = change;
        command.entityId        = entityId;
        command.component       = component;
    }
    #endregion
    
#region tag
    
    public void AddTag<T>(int entityId)
        where T : struct, ITag
    {
        
    }
    
    public void RemoveTag<T>(int entityId)
        where T : struct, ITag
    {
        
    }
#endregion
}

