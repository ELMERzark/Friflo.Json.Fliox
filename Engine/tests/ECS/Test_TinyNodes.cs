﻿using System;
using System.Diagnostics;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.ECS;

public static class Test_TinyNodes
{
    /// <summary>Similar to <see cref="Test_StructComponent.Test_9_RemoveComponent"/></summary>
    [Test]
    public static void Test_TinyNodes_Components()
    {
        var store   = new TinyEntityStore();
        var type1 = store.GetArchetype(Signature.Get<Position>());
        var type2 = store.GetArchetype(Signature.Get<Position, Rotation>());
        
        var entity1  = store.CreateTinyNode();
        store.AddEntityComponent(entity1, new Position { x = 1 });
        AreEqual(1,     type1.EntityCount);
        AreEqual(1,     store.GetEntityComponentCount(entity1));
        
        store.RemoveEntityComponent<Position>(entity1);
        AreEqual(0,     type1.EntityCount);
        AreEqual(0,     store.GetEntityComponentCount(entity1));
        
        store.AddEntityComponent(entity1, new Position { x = 1 });
        AreEqual(1,     type1.EntityCount);
        AreEqual(1,     store.GetEntityComponentCount(entity1));
        
        store.AddEntityComponent(entity1, new Rotation { x = 2 });
        AreEqual(0,     type1.EntityCount);
        AreEqual(1,     type2.EntityCount);
        AreEqual(2,     store.GetEntityComponentCount(entity1));
        
        store.RemoveEntityComponent<Rotation>(entity1);
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     type2.EntityCount);
        AreEqual(1f,    store.GetEntityComponentValue<Position>(entity1).x);
        AreEqual(1,     store.GetEntityComponentCount(entity1));
        //
        var entity2  = store.CreateTinyNode();
        store.AddEntityComponent(entity2, new Position { x = 1 });    // possible alloc: resize type1.entityIds
        store.RemoveEntityComponent<Position>(entity2);               // note: remove the last id in type1.entityIds => only type1.entityCount--  
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     store.GetEntityComponentCount(entity2));
        
        var start = Mem.GetAllocatedBytes();
        store.AddEntityComponent(entity2, new Position { x = 1 });
        store.RemoveEntityComponent<Position>(entity2);
        Mem.AssertNoAlloc(start);
        
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     store.GetEntityComponentCount(entity2));
    }
    
    [Test]
    public static void Test_TinyNodes_Create_Perf()
    {
        var store   = new TinyEntityStore();
        store.CreateTinyNode(); // load required methods to avoid measuring this in perf loop. 
        
        int count       = 10; // 100_000_000 ~ 413 ms
        var stopwatch   = new Stopwatch();
        store.EnsureTinyNodeCapacity(count);
        stopwatch.Start();
        for (int n = 0; n < count; n++) {
            store.CreateTinyNode();
        }
        Console.WriteLine($"create TinyNode's. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
    }
}