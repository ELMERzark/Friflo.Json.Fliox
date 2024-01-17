﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
// ReSharper disable InconsistentNaming
namespace Friflo.Engine.ECS;

public enum ChangedEventAction
{
    Add     = 0,
    Remove  = 1,
}

/// <summary>
/// Is the event for event handlers added to <see cref="Entity.OnComponentChanged"/>,
/// <see cref="EntityStoreBase.OnComponentAdded"/> or <see cref="EntityStoreBase.OnComponentRemoved"/>.<br/>
/// <br/>
/// These events are fired on:
/// <list type="bullet">
///     <item><see cref="Entity.AddComponent{T}()"/></item>
///     <item><see cref="Entity.RemoveComponent{T}()"/></item>
/// </list>
/// </summary>
public readonly struct  ComponentChanged
{
    public readonly     EntityStore         Store;          //  8
    public readonly     int                 EntityId;       //  4
    public readonly     ChangedEventAction  Action;         //  4
    public readonly     ComponentType       ComponentType;  //  8
    
    public              Entity              Entity      => new Entity(Store, EntityId);
    
    public override     string              ToString()  => $"entity: {EntityId} - event > {Action} {ComponentType}";

    internal ComponentChanged(EntityStoreBase store, int entityId, ChangedEventAction action, int structIndex)
    {
        this.Store          = store as EntityStore; 
        this.EntityId       = entityId;
        this.Action         = action;
        this.ComponentType  = EntityStoreBase.Static.EntitySchema.components[structIndex];
    }
}