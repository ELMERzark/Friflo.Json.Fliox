﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Friflo.Engine.ECS;

/// <summary>
/// Is the event for event handlers added to <see cref="Entity.OnChildEntitiesChanged"/>
/// or <see cref="EntityStore.OnChildEntitiesChanged"/>.<br/>
/// <br/>
/// These events are fired on:
/// <list type="bullet">
///     <item><see cref="Entity.AddChild"/></item>
///     <item><see cref="Entity.InsertChild"/></item>
///     <item><see cref="Entity.RemoveChild"/></item>
/// </list>
/// </summary>
public readonly struct ChildEntitiesChanged
{
    public readonly ChildEntitiesChangedAction  Action;     //  4
    public readonly EntityStore                 Store;      //  8
    public readonly int                         ParentId;   //  4
    public readonly int                         ChildId;    //  4
    public readonly int                         ChildIndex; //  4
    
    public          Entity                      Parent      => new Entity(Store, ParentId);
    public          Entity                      Child       => new Entity(Store, ChildId);

    public override string                      ToString()  => $"entity: {ParentId} - event > {Action} Child[{ChildIndex}] = {ChildId}";

    internal ChildEntitiesChanged(
        ChildEntitiesChangedAction  action,
        EntityStore                 store,
        int                         parentId,
        int                         childId,
        int                         childIndex)
    {
        this.Action     = action;
        this.Store      = store;
        this.ParentId   = parentId;
        this.ChildId    = childId;
        this.ChildIndex = childIndex;
    }
}