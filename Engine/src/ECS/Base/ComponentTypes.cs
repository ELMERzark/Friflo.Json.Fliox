﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

public sealed class ComponentTypes
{
    // --- public properties
    /// <summary>return all struct component types attributed with <see cref="StructComponentAttribute"/></summary>
    /// <remarks>
    /// <see cref="ComponentType.index"/> is equal to the array index<br/>
    /// <see cref="Structs"/>[0] is always null
    /// </remarks>
    public  ReadOnlySpan<ComponentType>                 Structs             => new (structs);
    /// <summary>return all class component types attributed with <see cref="ClassComponentAttribute"/></summary>
    /// <remarks>
    /// <see cref="ComponentType.index"/> is equal to the array index<br/>
    /// <see cref="Classes"/>[0] is always null
    /// </remarks>
    public  ReadOnlySpan<ComponentType>                 Classes             => new (classes);
    
    public  IReadOnlyDictionary<string, ComponentType>  ComponentTypeByKey  => componentTypeByKey;
    
    // --- private fields
    private  readonly   ComponentType[]                      structs;
    private  readonly   ComponentType[]                      classes;
    private  readonly   Dictionary<Type,   ComponentType>    componentTypeByType;
    private  readonly   Dictionary<string, ComponentType>    componentTypeByKey;
    
    internal ComponentTypes(List<ComponentType> structList, List<ComponentType> classList)
    {
        int count           = structList.Count + classList.Count;
        componentTypeByKey  = new Dictionary<string, ComponentType>(count);
        componentTypeByType = new Dictionary<Type,   ComponentType>(count);
        structs             = new ComponentType[structList.Count + 1];
        classes             = new ComponentType[classList.Count  + 1];
        foreach (var structType in structList) {
            componentTypeByKey. Add(structType.componentKey, structType);
            componentTypeByType.Add(structType.type,         structType);
            structs[structType.index] = structType;
        }
        foreach (var classType in classList) {
            componentTypeByKey. Add(classType.componentKey, classType);
            componentTypeByType.Add(classType.type,         classType);
            classes[classType.index] = classType;
        }
    }
    
    /// <summary>
    /// return <see cref="ComponentType"/> of a component type attributed with
    /// <see cref="StructComponentAttribute"/> or <see cref="ClassComponentAttribute"/> for the given key
    /// </summary>
    public ComponentType GetComponentTypeByKey(string key) {
        componentTypeByKey.TryGetValue(key, out var result);
        return result;
    }
    
    /// <summary>
    /// return <see cref="ComponentType"/> of a struct attributed with <see cref="StructComponentAttribute"/> for the given key
    /// </summary>
    public ComponentType GetStructComponentType<T>() where T : struct
    {
        componentTypeByType.TryGetValue(typeof(T), out var result);
        return result;
    }
    
    /// <summary>
    /// return <see cref="ComponentType"/> of a class attributed with <see cref="ClassComponentAttribute"/> for the given type
    /// </summary>
    public ComponentType GetClassComponentType<T>() where T : ClassComponent
    {
        componentTypeByType.TryGetValue(typeof(T), out var result);
        return result;
    }
}

internal static class ComponentUtils
{
    internal static ComponentTypes RegisterComponentTypes(TypeStore typeStore)
    {
        var types   = GetComponentTypes();
        var structs = new List<ComponentType>(types.Count);
        var classes = new List<ComponentType>(types.Count);
        foreach (var type in types) {
            RegisterComponentType(type, structs, classes, typeStore);
        }
        return new ComponentTypes(structs, classes);
    }
    
    private static void RegisterComponentType(
        Type                type,
        List<ComponentType> structs,
        List<ComponentType> classes,
        TypeStore           typeStore)
    {
        const BindingFlags flags    = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.InvokeMethod;
        var createParams            = new object[] { typeStore };
        foreach (var attr in type.CustomAttributes)
        {
            var attributeType = attr.AttributeType;
            if (attributeType == typeof(StructComponentAttribute))
            {
                var method          = typeof(ComponentUtils).GetMethod(nameof(CreateStructFactory), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var componentType   = (ComponentType)genericMethod.Invoke(null, createParams);
                structs.Add(componentType);
                return;
            }
            if (attributeType == typeof(ClassComponentAttribute))
            {
                var method          = typeof(ComponentUtils).GetMethod(nameof(CreateClassFactory), flags);
                var genericMethod   = method!.MakeGenericMethod(type);
                var componentType   = (ComponentType)genericMethod.Invoke(null, createParams);
                classes.Add(componentType);
                return;
            }
        }
        throw new InvalidOperationException("missing expected attribute");
    }
    
    internal static ComponentType CreateStructFactory<T>(TypeStore typeStore) where T : struct  {
        var structIndex = StructHeap<T>.StructIndex;
        var structKey   = StructHeap<T>.StructKey;
        return new StructComponentType<T>(structKey, structIndex, typeStore);
    }
    
    internal static ComponentType CreateClassFactory<T>(TypeStore typeStore) where T : ClassComponent  {
        var classIndex  = ClassTypeInfo<T>.ClassIndex;
        var classKey    = ClassTypeInfo<T>.ClassKey;
        return new ClassComponentType<T>(classKey, classIndex, typeStore);
    }
    
    // --------------------------- query all struct / class component types ---------------------------
    private static List<Type> GetComponentTypes()
    {
        var componentTypes  = new List<Type>();
        var engineAssembly  = typeof(Utils).Assembly;
        var engineFullName  = engineAssembly.FullName;
        AddComponentTypes(componentTypes, engineAssembly);
        
        var assemblies      = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        { 
            var referencedAssemblies = assembly.GetReferencedAssemblies();
            foreach (var referencedAssembly in referencedAssemblies) {
                if (referencedAssembly.FullName != engineFullName) {
                    continue;
                }
                AddComponentTypes(componentTypes, assembly);
                break;
            }
        }
        return componentTypes;
    }
    
    private static void AddComponentTypes(List<Type> componentTypes, Assembly assembly)
    {
        var types = assembly.GetTypes();
        foreach (var type in types) {
            foreach (var attr in type.CustomAttributes)
            {
                var attributeType = attr.AttributeType;
                if (attributeType == typeof(StructComponentAttribute) ||
                    attributeType == typeof(ClassComponentAttribute))
                {
                    componentTypes.Add(type);
                }
            }
        }
    }
}