﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Friflo.Engine.ECS.Relations;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public static class RelationExtensions
{
#region Entity
    /// <summary>
    /// Returns the relation of the <paramref name="entity"/> with the given <paramref name="key"/>.<br/>
    /// Executes in O(N) N: number of entity relations.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The relation is not found at the passed entity.</exception>
    /// <exception cref="NullReferenceException">If the entity is null.</exception>
    public static ref TComponent GetRelation<TComponent, TKey>(this Entity entity, TKey key)
        where TComponent : struct, IRelationComponent<TKey>
    {
        if (entity.archetype == null) throw EntityStoreBase.EntityNullException(entity);
        return ref EntityRelations.GetRelation<TComponent, TKey>(entity.store, entity.Id, key);
    }
    
    /// <summary>
    /// Returns the relation of the <paramref name="entity"/> with the given <paramref name="key"/>.<br/>
    /// Executes in O(N) N: number of entity relations.
    /// </summary>
    /// <exception cref="NullReferenceException">If the entity is null.</exception>
    public static bool TryGetRelation<TComponent, TKey>(this Entity entity, TKey key, out TComponent value)
        where TComponent : struct, IRelationComponent<TKey>
    {
        if (entity.archetype == null) throw EntityStoreBase.EntityNullException(entity);
        return EntityRelations.TryGetRelation(entity.store, entity.Id, key, out value);
    }
    
    /// <summary>
    /// Returns all unique relation components of the passed <paramref name="entity"/>.<br/>
    /// Executes in O(1). In case <typeparamref name="TComponent"/> is a <see cref="ILinkRelation"/> it returns all linked entities.
    /// </summary>
    /// <exception cref="NullReferenceException">If the entity is null.</exception>
    public static RelationComponents<TComponent> GetRelations<TComponent>(this Entity entity)
        where TComponent : struct, IRelationComponent
    {
        if (entity.archetype == null) throw EntityStoreBase.EntityNullException(entity);
        return EntityRelations.GetRelations<TComponent>(entity.store, entity.Id);
    }
    
    /// <summary>
    /// Removes the relation component with the specified <paramref name="key"/> from an entity.<br/>
    /// Executes in O(N) N: number of relations of the specific entity.
    /// </summary>
    /// <exception cref="NullReferenceException">If the entity is null.</exception>
    /// <returns>true if the entity contained a relation of the given type before. </returns>
    public static bool RemoveRelation<T, TKey>(this Entity entity, TKey key)
        where T : struct, IRelationComponent<TKey>
    {
        if (entity.archetype == null) throw EntityStoreBase.EntityNullException(entity); 
        return EntityRelations.RemoveRelation<T, TKey>(entity.store, entity.Id, key);
    }
    
    /// <summary>
    /// Removes the specified link relation <paramref name="target"/> from an entity.<br/>
    /// Executes in O(N) N: number of link relations of the specified entity.
    /// </summary>
    /// <exception cref="NullReferenceException">If the entity is null.</exception>
    /// <returns>true if the entity contained a link relation of the given type before. </returns>
    public static bool RemoveLinkRelation<T>(this Entity entity, Entity target)
        where T : struct, ILinkRelation
    {
        if (entity.archetype == null) throw EntityStoreBase.EntityNullException(entity);
        return EntityRelations.RemoveRelation<T, Entity>(entity.store, entity.Id, target);
    }
    #endregion
    
#region EntityStore
    public static EntityReadOnlyCollection GetEntitiesWithRelations<TComponent>(this EntityStore store)
        where TComponent : struct, IRelationComponent
    {
        var relations = EntityRelations.GetEntityRelations(store, StructInfo<TComponent>.Index);
        return new EntityReadOnlyCollection(store, relations.relationPositions.Keys);
    }
    #endregion
}