﻿// Copyright (c) Ullrich Praetz - https://github.com/friflo. All rights reserved.
// See LICENSE file in the project root for full license information.

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

/// <summary>
/// Is used to define a component type having a single indexed field / property.<br/>
/// </summary>
/// <remarks>
/// This component type enables:
/// <list type="bullet">
///   <item>
///     Return all entities with a component field of a specific value. <br/>
///     See <see cref="EntityStore.GetEntitiesWithComponentValue{TIndexedComponent,TValue}"/>.
///   </item>
///   <item>
///     Return a collection of all unique component values.<br/>
///     See <see cref="EntityStore.GetIndexedComponentValues{TIndexedComponent,TValue}"/>.
///   </item>
///   <item>
///     Filter entites in a query having a specific component value.<br/>
///     See <see cref="ArchetypeQuery.HasValue{TComponent,TValue}"/>.
///   </item>
///   <item>
///     Filter entities in a query with a component value in a specific range.<br/>
///     See <see cref="ArchetypeQuery.ValueInRange{TComponent,TValue}"/>.
///   </item>
/// </list>
/// </remarks>
internal interface IIndexedComponent<out TValue> : IComponent
{
    TValue GetIndexedValue();
}

/// <summary>
/// Is used to define a component type having a link to another <see cref="Entity"/>.<br/>
/// </summary>
/// <remarks>
/// This component type enables:
/// <list type="bullet">
///   <item>
///     Return all entities having a <see cref="ILinkComponent"/> to a specific entity.<br/>
///     See <see cref="IndexExtensions.GetLinkedEntities{TComponent}"/>
///   </item>
/// </list>
/// </remarks>
internal interface ILinkComponent : IIndexedComponent<Entity> { }