﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

// ReSharper disable once InconsistentNaming
public static class NativeAOT
{
    private static readonly List<SchemaType>    Types       = new();
    private static readonly TypeStore           TypeStore   = EntityStoreBase.Static.TypeStore;
    private static readonly SchemaTypes         SchemaTypes = new SchemaTypes();
    
    internal static EntitySchema CreateSchema()
    {
        Console.WriteLine("NativeAOT.CreateSchema() - begin");
        RegisterComponent<EntityName>();
        RegisterComponent<Position>();
        RegisterComponent<Rotation>();
        RegisterComponent<Scale3>();
        RegisterComponent<Transform>();
        RegisterComponent<TreeNode>();
        RegisterComponent<UniqueEntity>();
        RegisterComponent<Unresolved>();
        
        RegisterTag<Disabled>();
        
        var dependant   = new EngineDependant (null, Types);
        var dependants  = new List<EngineDependant> { dependant };
        var schema      =  new EntitySchema(dependants, SchemaTypes);
        Console.WriteLine("NativeAOT.CreateSchema() - end");
        return schema;
    }
    
    private static void RegisterComponent<T>() where T : struct, IComponent 
    {
        var components      = SchemaTypes.components;
        var structIndex     = components.Count + 1;
        var componentType   = SchemaUtils.CreateComponentType<T>(TypeStore, structIndex);
        components.Add(componentType);
        Types.Add(componentType);
    }
    
    private static void RegisterTag<T>()  where T : struct, ITag 
    {
        var tags            = SchemaTypes.tags;
        var tagIndex        = tags.Count + 1;
        var tagType         = SchemaUtils.CreateTagType<T>(tagIndex);
        tags.Add(tagType);
        Types.Add(tagType);
    }
    
    private static void RegisterScript<T>()  where T : Script, new()
    {
        var scripts         = SchemaTypes.scripts;
        var scriptIndex     = scripts.Count + 1;
        var scriptType      = SchemaUtils.CreateScriptType<T>(TypeStore, scriptIndex);
        scripts.Add(scriptType);
        Types.Add(scriptType);
    }
}