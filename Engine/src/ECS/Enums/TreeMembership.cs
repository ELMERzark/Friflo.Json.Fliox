﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using static Friflo.Fliox.Engine.ECS.StoreOwnership;

// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

/// <summary>Describe the membership of a <see cref="GameEntity"/> to the tree of an <see cref="EntityStore"/></summary>
/// <remarks>Requirement: The entity must be <see cref="attached"/> to an <see cref="EntityStore"/></remarks>
public enum TreeMembership
{
    /// <summary>The entity is not member of the <see cref="EntityStore"/> tree</summary>
    floating    = 0,
    /// <summary>The entity is member of the <see cref="EntityStore"/> tree</summary>
    treeNode    = 1,
}

