﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

/// <summary>
/// A <see cref="ScriptChangedHandler"/> added to <see cref="EntityStore.ScriptAdded"/> get events on <see cref="Entity.AddScript{T}"/>
/// A <see cref="ScriptChangedHandler"/> added to <see cref="EntityStore.ScriptRemoved"/> get events on <see cref="Entity.RemoveScript{T}"/>
/// </summary>
public delegate void   ScriptChangedHandler    (in ScriptChangedArgs e);

public readonly struct  ScriptChangedArgs
{
    public readonly     int                 entityId;
    public readonly     ChangedEventType    type; 
    public readonly     ScriptType          scriptType;
    
    public override     string              ToString() => $"entity: {entityId} - {type} {scriptType}";

    internal ScriptChangedArgs(int entityId, ChangedEventType type, ScriptType scriptType)
    {
        this.entityId       = entityId;
        this.type           = type;
        this.scriptType     = scriptType;
    }
}