﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.


using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable once CheckNamespace
namespace Friflo.Engine.ECS;

public readonly struct Chunks<T1, T2>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    public              int             Length => chunk1.Length;
    public readonly     Chunk<T1>       chunk1;     //  16
    public readonly     Chunk<T2>       chunk2;     //  16
    public readonly     ChunkEntities   entities;   //  24

    public override     string          ToString() => entities.GetChunksString();

    internal Chunks(Chunk<T1> chunk1, Chunk<T2> chunk2, ChunkEntities entities) {
        this.chunk1     = chunk1;
        this.chunk2     = chunk2;
        this.entities   = entities;
    }
    
    public void Deconstruct(out Chunk<T1> chunk1, out Chunk<T2> chunk2, out ChunkEntities entities) {
        chunk1      = this.chunk1;
        chunk2      = this.chunk2;
        entities    = this.entities;
    }
}

public readonly struct QueryChunks<T1, T2> : IEnumerable <Chunks<T1,T2>>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly ArchetypeQuery<T1, T2> query;

    public  override string         ToString() => query.GetQueryChunksString();

    internal QueryChunks(ArchetypeQuery<T1, T2> query) {
        this.query = query;
    }
    
    // --- IEnumerable<>
    [ExcludeFromCodeCoverage]
    IEnumerator<Chunks<T1,T2>>
    IEnumerable<Chunks<T1,T2>>.GetEnumerator() => new ChunkEnumerator<T1, T2> (query);
    
    // --- IEnumerable
    [ExcludeFromCodeCoverage]
    IEnumerator     IEnumerable.GetEnumerator() => new ChunkEnumerator<T1, T2> (query);
    
    // --- IEnumerable
    public ChunkEnumerator<T1, T2> GetEnumerator() => new (query);
}

public struct ChunkEnumerator<T1, T2> : IEnumerator<Chunks<T1,T2>>
    where T1 : struct, IComponent
    where T2 : struct, IComponent
{
    private readonly    T1[]                    copyT1;         //  8
    private readonly    T2[]                    copyT2;         //  8
    private readonly    int                     structIndex1;   //  4
    private readonly    int                     structIndex2;   //  4
    //
    private readonly    Archetypes              archetypes;     // 16
    //
    private             int                     archetypePos;   //  4
    private             Chunks<T1, T2>          chunks;         // 46
    
    
    internal  ChunkEnumerator(ArchetypeQuery<T1, T2> query)
    {
        copyT1          = query.copyT1;
        copyT2          = query.copyT2;
        structIndex1    = query.signatureIndexes.T1;
        structIndex2    = query.signatureIndexes.T2;
        archetypes      = query.GetArchetypes();
        archetypePos    = -1;
    }
    
    /// <summary>return Current by reference to avoid struct copy and enable mutation in library</summary>
    public readonly Chunks<T1,T2> Current   => chunks;
    
    // --- IEnumerator
    [ExcludeFromCodeCoverage]
    public void Reset() {
        archetypePos    = -1;
        chunks          = default;
    }

    [ExcludeFromCodeCoverage]
    object IEnumerator.Current  => chunks;
    
    // --- IEnumerator
    public bool MoveNext()
    {
        Archetype archetype;
        // --- skip archetypes without entities
        do {
           if (archetypePos >= archetypes.last) {  // last = length - 1
               return false;
           }
           archetype    = archetypes.array[++archetypePos];
        }
        while (archetype.entityCount == 0);
        
        // --- set chunks of new archetype
        var heapMap     = archetype.heapMap;
        var chunks1     = (StructHeap<T1>)heapMap[structIndex1];
        var chunks2     = (StructHeap<T2>)heapMap[structIndex2];
        var count       = archetype.entityCount;

        var chunk1      = new Chunk<T1>(chunks1.components, copyT1, count);
        var chunk2      = new Chunk<T2>(chunks2.components, copyT2, count);
        var entities    = new ChunkEntities(archetype, count);
        chunks          = new Chunks<T1, T2>(chunk1, chunk2, entities);
        return true;  
    }
    
    // --- IDisposable
    public void Dispose() { }
}
