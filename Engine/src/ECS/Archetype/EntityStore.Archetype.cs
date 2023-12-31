﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

// Hard rule: this file MUST NOT use type: Entity

using System;
using System.Collections.Generic;

// ReSharper disable ArrangeTrailingCommaInMultilineLists
// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public partial class EntityStoreBase
{
    // -------------------------------------- get archetype --------------------------------------
#region get archetype
    private static Archetype GetArchetype(EntityStoreBase store, in Tags tags, int structIndex)
    {
        var searchKey = store.searchKey;
        searchKey.SetTagsWith(tags, structIndex);
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config  = GetArchetypeConfig(store);
        var schema  = Static.EntitySchema;
        var types   = new SignatureIndexes(1,
            T1: schema.CheckStructIndex(null, structIndex)
        );
        var archetype = Archetype.CreateWithSignatureTypes(config, types, tags);
        AddArchetype(store, archetype);
        return archetype;
    }
    
    internal static ArchetypeConfig GetArchetypeConfig(EntityStoreBase store) {
        return new ArchetypeConfig (store, store.archsCount);
    }
    
    private static Archetype GetArchetypeWithSignature(EntityStoreBase store, in SignatureIndexes indexes, in Tags tags)
    {
        var searchKey = store.searchKey;
        searchKey.SetSignatureTags(indexes, tags);
        if (store.archSet.TryGetValue(searchKey, out var archetypeKey)) {
            return archetypeKey.archetype;
        }
        var config      = GetArchetypeConfig(store);
        var archetype   = Archetype.CreateWithSignatureTypes(config, indexes, tags);
        AddArchetype(store, archetype);
        return archetype;
    }
    
    public Archetype FindArchetype(in ComponentTypes componentTypes, in Tags tags) {
        searchKey.componentTypes    = componentTypes;
        searchKey.tags              = tags;
        searchKey.CalculateHashCode();
        archSet.TryGetValue(searchKey, out var actualValue);
        return actualValue?.archetype;
    }
    
    public Archetype GetArchetype(in Tags tags)
    {
        return GetArchetypeWithSignature(this, default, tags);
    }

    public Archetype GetArchetype<T1>(in Signature<T1> signature, in Tags tags = default)
        where T1 : struct, IComponent
    {
        return GetArchetypeWithSignature(this, signature.signatureIndexes, tags);
    }
    
    public Archetype GetArchetype<T1, T2>(in Signature<T1, T2> signature, in Tags tags = default)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
    {
        return GetArchetypeWithSignature(this, signature.signatureIndexes, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3>(in Signature<T1, T2, T3> signature, in Tags tags = default)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
    {
        return GetArchetypeWithSignature(this, signature.signatureIndexes, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3, T4>(in Signature<T1, T2, T3, T4> signature, in Tags tags = default)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
    {
        return GetArchetypeWithSignature(this, signature.signatureIndexes, tags);
    }
    
    public Archetype GetArchetype<T1, T2, T3, T4, T5>(in Signature<T1, T2, T3, T4, T5> signature, in Tags tags = default)
        where T1 : struct, IComponent
        where T2 : struct, IComponent
        where T3 : struct, IComponent
        where T4 : struct, IComponent
        where T5 : struct, IComponent
    {
        return GetArchetypeWithSignature(this, signature.signatureIndexes, tags);
    }
    #endregion
    
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
}
