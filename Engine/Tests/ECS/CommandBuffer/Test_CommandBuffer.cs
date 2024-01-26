﻿using System;
using System.Diagnostics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.CommandBuffer;

#pragma warning disable CS0618 // Type or member is obsolete TODO remove

public static class Test_CommandBuffer
{
    [Test]
    public static void Test_CommandBuffer_components()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var entity  = store.CreateEntity(1);
        var ecb     = new EntityCommandBuffer(store);
        
        // --- structural change: add Position
        var pos1 = new Position(1, 1, 1);
        var pos2 = new Position(2, 2, 2);
        ecb.AddComponent(1, pos1);
        ecb.SetComponent(1, pos2);
        //
        ecb.Playback();
        
        AreEqual(pos2.x,            entity.GetComponent<Position>().x);
        AreSame(entity.Archetype,   store.GetArchetype(ComponentTypes.Get<Position>()));
        
        // --- handle remove after add
        ecb.AddComponent   <Position>(1);
        ecb.RemoveComponent<Position>(1);
        
        ecb.Playback();
        
        IsFalse(entity.HasComponent<Position>());
        
        // --- no structural change
        ecb.AddComponent   <Position>(1);
        ecb.RemoveComponent<Position>(1);
        
        ecb.Playback();
        
        IsFalse(entity.HasComponent<Position>());
        
        // --- archetype changes

        entity.AddComponent(new Rotation());
        
        ecb.AddComponent(1, pos1);
        ecb.Playback();
        
        AreEqual(2, entity.Components.Count);
        AreEqual(1, entity.Position.x);
    }
    
    [Test]
    public static void Test_CommandBuffer_IncreaseCommands()
    {
        int count       = 10; // 1_000_000 ~ #PC: 215 ms
        var store       = new EntityStore(PidType.UsePidAsId);
        var ecb         = new EntityCommandBuffer(store);
        var entities    = new Entity[count];
        store.EnsureCapacity(count);
        for (int n = 0; n < count; n++) {
            entities[n] = store.CreateEntity();
        }
        var sw = new Stopwatch();
        sw.Start();
        for (int n = 0; n < count; n++) {
            ecb.AddComponent<Position>(n + 1, new Position(n + 1, 0, 0));    
        }
        ecb.Playback();
        Console.WriteLine($"EntityCommandBuffer.AddComponent() - duration: {sw.ElapsedMilliseconds} ms");
        
        for (int n = 0; n < count; n++) {
            Mem.AreEqual(n + 1, entities[n].Position.x);
        }
    }
    
    [Test]
    public static void Test_CommandBuffer_tags()
    {
        var store   = new EntityStore(PidType.UsePidAsId);
        var ecb     = new EntityCommandBuffer(store);
        
        // TODO not implemented
        ecb.AddTag   <TestTag>(1);
        ecb.RemoveTag<TestTag>(1);
    }
}