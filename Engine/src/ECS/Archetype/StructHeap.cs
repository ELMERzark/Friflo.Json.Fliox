// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using Friflo.Json.Burst;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Mapper;

// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable InconsistentNaming
// ReSharper disable once CheckNamespace
namespace Friflo.Fliox.Engine.ECS;

internal abstract class StructHeap
{
    // --- internal fields
    internal readonly   string      structKey;      //  8
    internal readonly   Bytes       keyBytes;       // 16
    internal readonly   Type        type;           //  8
    internal readonly   int         structIndex;    //  4
#if DEBUG
    private             Archetype   archetype; // only used to provide debug info.
#endif

    public   override   string      ToString() => GetString();
    
    internal abstract   void        SetCapacity         (int capacity);
    internal abstract   void        MoveComponent       (int from, int to);
    internal abstract   void        CopyComponentTo     (int sourcePos, StructHeap target, int targetPos);
    internal abstract   object      GetComponentDebug   (int compIndex);
    internal abstract   Bytes       Write               (ObjectWriter writer, int compIndex);
    internal abstract   void        Read                (ObjectReader reader, int compIndex, JsonValue json);

    internal StructHeap(int structIndex, string structKey, Type type) {
        this.structIndex    = structIndex;
        this.structKey      = structKey;
        keyBytes            = new Bytes(structKey);
        this.type           = type;
    }

    internal string GetString() {
#if DEBUG
        return $"[{type.Name}] heap - Count: {archetype.EntityCount}";
#else
        return $"[{type.Name}] heap";
#endif
    }

    internal void SetArchetype(Archetype archetype) {
#if DEBUG
        this.archetype = archetype;
#endif
    }
}