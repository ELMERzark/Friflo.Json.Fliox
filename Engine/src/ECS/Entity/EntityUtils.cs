﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Friflo.Fliox.Engine.ECS.Serialize;
using Friflo.Json.Fliox;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Global
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

public class EntityEqualityComparer : IEqualityComparer<Entity>
{
    public  bool    Equals(Entity left, Entity right)   => left.id == right.id;
    public  int     GetHashCode(Entity entity)          => entity.id;
}

// ReSharper disable once UnusedType.Global
internal static class EntityExtensions { } // Engine.ECS must not have an Entity extension class to avoid confusion

public static class EntityUtils
{
    public static  readonly EntityEqualityComparer EqualityComparer = new ();
 
    // ------------------------------------------- public methods -------------------------------------------
#region non generic component - methods
    /// <summary>
    /// Returns a copy of the entity component as an object.<br/>
    /// The returned <see cref="IComponent"/> is a boxed struct.<br/>
    /// So avoid using this method whenever possible. Use <see cref="Entity.GetComponent{T}"/> instead.
    /// </summary>
    public static  IComponent   GetEntityComponent    (Entity entity, ComponentType componentType) {
        return entity.archetype.heapMap[componentType.structIndex].GetComponentDebug(entity.compIndex);
    }

    public static  bool         RemoveEntityComponent (Entity entity, ComponentType componentType)
    {
        int archIndex = 0;
        return entity.archetype.entityStore.RemoveComponent(entity.id, ref entity.refArchetype, ref entity.refCompIndex, ref archIndex, componentType.structIndex);
    }
    
    public static  bool         AddEntityComponent    (Entity entity, ComponentType componentType) {
        return componentType.AddEntityComponent(entity);
    }
    #endregion
    
#region non generic script - methods
    public static   Script      GetEntityScript    (Entity entity, ScriptType scriptType) => GetScript       (entity, scriptType.type);
    
    public static   Script      RemoveEntityScript (Entity entity, ScriptType scriptType) => RemoveScriptType(entity, scriptType);
    
    public static   Script      AddNewEntityScript (Entity entity, ScriptType scriptType) => AddNewScript    (entity, scriptType);
    
    public static   Script      AddEntityScript    (Entity entity, Script script)         => AddScript       (entity, script);

    #endregion
    
    // ------------------------------------------- internal methods -------------------------------------------
#region internal - methods
    internal static int ComponentCount (this Entity entity) {
        return entity.archetype.componentCount + entity.Scripts.Length;
    }
    
    internal static Exception NotImplemented(int id, string use) {
        var msg = $"to avoid excessive boxing. Use {use} or {nameof(EntityUtils)}.{nameof(EntityUtils.EqualityComparer)}. id: {id}";
        return new NotImplementedException(msg);
    }
    
    internal static string EntityToString(Entity entity) {
        if (entity.store == null) {
            return "null";
        }
        return EntityToString(entity.id, entity.archetype, new StringBuilder());
    }
    
    internal static string EntityToString(int id, Archetype archetype, StringBuilder sb)
    {
        sb.Append("id: ");
        sb.Append(id);
        if (archetype == null) {
            sb.Append("  (detached)");
            return sb.ToString();
        }
        var entity = new Entity(id, archetype.entityStore);
        if (entity.HasName) {
            var name = entity.Name.value;
            if (name != null) {
                sb.Append("  \"");
                sb.Append(name);
                sb.Append('\"');
                return sb.ToString();
            }
        }
        if (entity.ComponentCount() == 0) {
            sb.Append("  []");
        } else {
            sb.Append("  [");
            var scripts = GetScripts(entity);
            foreach (var script in scripts) {
                sb.Append('*');
                sb.Append(script.GetType().Name);
                sb.Append(", ");
            }
            foreach (var heap in archetype.Heaps) {
                sb.Append(heap.StructType.Name);
                sb.Append(", ");
            }
            sb.Length -= 2;
            sb.Append(']');
        }
        return sb.ToString();
    }

    private static readonly EntitySerializer EntitySerializer   = new EntitySerializer();
    
    internal static string EntityToJSON(Entity entity)
    {
        var serializer = EntitySerializer;
        lock (serializer) {
            return serializer.WriteEntity(entity);
        }
    }
    
    /// <remarks> The "id" in the passed JSON <paramref name="value"/> is ignored. </remarks>
    internal static void JsonToEntity(Entity entity, string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var serializer = EntitySerializer;
        lock (serializer) {
            var jsonValue = new JsonValue(value);
            serializer.ReadIntoEntity(entity, jsonValue);
        }
    }
    
    private static readonly DataEntitySerializer DataEntitySerializer = new DataEntitySerializer();
    
    internal static string DataEntityToJSON(DataEntity dataEntity)
    {
        var serializer = DataEntitySerializer;
        lock (serializer) {
            var json = serializer.WriteDataEntity(dataEntity, out string error);
            if (json == null) {
                return error;
            }
            return json;
        }
    }
    
    // ---------------------------------- Script utils ----------------------------------
    private  static readonly Script[]       EmptyScripts  = Array.Empty<Script>();
    internal const  int                     NoScripts     = 0;  
    
    internal static Script[] GetScripts(Entity entity) {
        if (entity.scriptIndex == NoScripts) {
            return EmptyScripts;
        }
        return entity.archetype.entityStore.GetScripts(entity);
    }
    
    internal static Script GetScript(Entity entity, Type scriptType)
    {
        if (entity.scriptIndex == NoScripts) {
            return null;
        }
        return entity.archetype.entityStore.GetScript(entity, scriptType);
    }
    
    internal static Script AddScript(Entity entity, int scriptIndex, Script script)
    {
        var scriptType = EntityStoreBase.Static.EntitySchema.scripts[scriptIndex];
        return AddScriptInternal(entity, script, scriptType);
    }
    
    private static Script AddNewScript(Entity entity, ScriptType scriptType)
    {
        var script = scriptType.CreateScript();
        return AddScriptInternal(entity, script, scriptType);
    }
    
    private static Script AddScript (Entity entity, Script script) {
        var scriptType = EntityStoreBase.Static.EntitySchema.ScriptTypeByType[script.GetType()];
        return entity.archetype.entityStore.AddScript(entity, script, scriptType);
    }
    
    private static  Script AddScriptInternal(Entity entity, Script script, ScriptType scriptType)
    {
        if (!script.entity.IsNull) {
            throw new InvalidOperationException($"script already added to an entity. current entity id: {script.entity.id}");
        }
        return entity.archetype.entityStore.AddScript(entity, script, scriptType);
    }
    
    internal static Script RemoveScript(Entity entity, int scriptIndex) {
        if (entity.scriptIndex == NoScripts) {
            return null;
        }
        var scriptType  = EntityStoreBase.Static.EntitySchema.scripts[scriptIndex];
        return entity.archetype.entityStore.RemoveScript(entity, scriptType);
    }
    
    private static Script RemoveScriptType(Entity entity, ScriptType scriptType) {
        if (entity.scriptIndex == NoScripts) {
            return null;
        }
        return entity.archetype.entityStore.RemoveScript(entity, scriptType);
    }
    
    #endregion
}