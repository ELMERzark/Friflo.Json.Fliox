using System;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;
using NUnit.Framework;
using static Tests.Utils.Mem;

// ReSharper disable CompareOfFloatsByEqualityOperator
// ReSharper disable StringLiteralTypo
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Arch;

public static class Test_Query
{
    [Test]
    public static void Test_SignatureTypes()
    {
        var sig1            = Signature.Get<Position>();
        AreEqual("Signature: [Position]",   sig1.ToString());
        AreEqual("Components: [Position]",  sig1.ComponentTypes.ToString());
        
        var sig2            = Signature.Get<Position, Rotation>();
        AreEqual("Signature: [Position, Rotation]", sig2.ToString());

        int count = 0;
        foreach (var _ in sig2.ComponentTypes) {
            count++;    
        }
        AreEqual(2, count);
    }
    
    [Test]
    public static void Test_Signature_Get_Mem()
    {
        Signature.Get<Position>();  // force one time allocation
        
        var start   = GetAllocatedBytes();
        
        var sig1 = Signature.Get<Position>();
        var sig2 = Signature.Get<Position, Rotation>();
        var sig3 = Signature.Get<Position, Rotation, Scale3>();
        var sig4 = Signature.Get<Position, Rotation, Scale3, MyComponent1>();
        var sig5 = Signature.Get<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        
        AssertNoAlloc(start);
        
        AreEqual("Components: [Position]",                                                sig1.ComponentTypes.ToString());
        AreEqual("Components: [Position, Rotation]",                                      sig2.ComponentTypes.ToString());
        AreEqual("Components: [Position, Rotation, Scale3]",                              sig3.ComponentTypes.ToString());
        AreEqual("Components: [Position, Rotation, Scale3, MyComponent1]",                sig4.ComponentTypes.ToString());
        AreEqual("Components: [Position, Rotation, Scale3, MyComponent1, MyComponent2]",  sig5.ComponentTypes.ToString());
    }
    [Test]
    public static void Test_generic_Query_with_AllTags()
    {
        var store  =    new EntityStore();
        var query1 =    store.Query<Position>();
        var query2 =    store.Query<Position, Rotation>();
        var query3 =    store.Query<Position, Rotation, Scale3>();
        var query4 =    store.Query<Position, Rotation, Scale3, MyComponent1>();
        var query5 =    store.Query<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        
        AreEqual("Query: [Position]  EntityCount: 0",                                               query1.ToString());
        AreEqual("Query: [Position, Rotation]  EntityCount: 0",                                     query2.ToString());
        AreEqual("Query: [Position, Rotation, Scale3]  EntityCount: 0",                             query3.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1]  EntityCount: 0",               query4.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2]  EntityCount: 0", query5.ToString());
        
        var tags = Tags.Get<TestTag>();
        AreEqual("Query: [Position, #TestTag]  EntityCount: 0",                                                 query1.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, #TestTag]  EntityCount: 0",                                       query2.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, #TestTag]  EntityCount: 0",                               query3.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, #TestTag]  EntityCount: 0",                 query4.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2, #TestTag]  EntityCount: 0",   query5.AllTags(tags).ToString());
    }
    
    [Test]
    public static void Test_Signature_Query()
    {
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        
        var sig1 = Signature.Get<Position>();
        var sig2 = Signature.Get<Position, Rotation>();
        var sig3 = Signature.Get<Position, Rotation, Scale3>();
        var sig4 = Signature.Get<Position, Rotation, Scale3, MyComponent1>();
        var sig5 = Signature.Get<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        //
        var query1 =    store.Query(sig1);
        var query2 =    store.Query(sig2);
        var query3 =    store.Query(sig3);
        var query4 =    store.Query(sig4);
        var query5 =    store.Query(sig5);
        
        AreEqual("Query: [Position]  EntityCount: 0",                                               query1.ToString());
        AreEqual("Query: [Position, Rotation]  EntityCount: 0",                                     query2.ToString());
        AreEqual("Query: [Position, Rotation, Scale3]  EntityCount: 0",                             query3.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1]  EntityCount: 0",               query4.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2]  EntityCount: 0", query5.ToString());
        
        AreEqual(0, query1.Archetypes.Length);
        AreEqual(0, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        AreEqual(0, query5.Archetypes.Length);
        
        entity.AddComponent<Position>();
        AreEqual(1, query1.Archetypes.Length);
        AreEqual(0, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        AreEqual(0, query5.Archetypes.Length);
        
        entity.AddComponent<Rotation>();
        AreEqual(2, query1.Archetypes.Length);
        AreEqual(1, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        AreEqual(0, query5.Archetypes.Length);
        
        entity.AddComponent<Scale3>();
        AreEqual(3, query1.Archetypes.Length);
        AreEqual(2, query2.Archetypes.Length);
        AreEqual(1, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        AreEqual(0, query5.Archetypes.Length);
        
        entity.AddComponent<MyComponent1>();
        AreEqual(4, query1.Archetypes.Length);
        AreEqual(3, query2.Archetypes.Length);
        AreEqual(2, query3.Archetypes.Length);
        AreEqual(1, query4.Archetypes.Length);
        AreEqual(0, query5.Archetypes.Length);
        
        entity.AddComponent<MyComponent2>();
        AreEqual(5, query1.Archetypes.Length);
        AreEqual(4, query2.Archetypes.Length);
        AreEqual(3, query3.Archetypes.Length);
        AreEqual(2, query4.Archetypes.Length);
        AreEqual(1, query5.Archetypes.Length);
    }
    
    [Test]
    public static void Test_Signature_Query_ReadOnly()
    {
        var store   = new EntityStore();
        
        var sig1 = Signature.Get<Position>();
        var sig2 = Signature.Get<Position, Rotation>();
        var sig3 = Signature.Get<Position, Rotation, Scale3>();
        var sig4 = Signature.Get<Position, Rotation, Scale3, MyComponent1>();
        var sig5 = Signature.Get<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        //
        var query1 =    store.Query(sig1);
        var query2 =    store.Query(sig2);
        var query3 =    store.Query(sig3);
        var query4 =    store.Query(sig4);
        var query5 =    store.Query(sig5);
        
        query1.ReadOnly<Position>();
        //
        query2.ReadOnly<Position>();
        query2.ReadOnly<Rotation>();
        //
        query3.ReadOnly<Position>();
        query3.ReadOnly<Rotation>();
        query3.ReadOnly<Scale3>();
        //
        query4.ReadOnly<Position>();
        query4.ReadOnly<Rotation>();
        query4.ReadOnly<Scale3>();
        query4.ReadOnly<MyComponent1>();
        //
        query5.ReadOnly<Position>();
        query5.ReadOnly<Rotation>();
        query5.ReadOnly<Scale3>();
        query5.ReadOnly<MyComponent1>();
        query5.ReadOnly<MyComponent2>();
        //
        var e = Assert.Throws<ArgumentException>(() => { query1.ReadOnly<Transform>(); });
        AreEqual("Query does not contain Component type: Transform", e!.Message);
        
        e = Assert.Throws<ArgumentException>(() => { query2.ReadOnly<Transform>(); });
        AreEqual("Query does not contain Component type: Transform", e!.Message);

        e = Assert.Throws<ArgumentException>(() => { query3.ReadOnly<Transform>(); });
        AreEqual("Query does not contain Component type: Transform", e!.Message);
        
        e = Assert.Throws<ArgumentException>(() => { query4.ReadOnly<Transform>(); });
        AreEqual("Query does not contain Component type: Transform", e!.Message);

        e = Assert.Throws<ArgumentException>(() => { query5.ReadOnly<Transform>(); });
        AreEqual("Query does not contain Component type: Transform", e!.Message);
    }
    
    [Test]
    public static void Test_Query_creation_Perf()
    {
        var store   = new EntityStore();
        var sig     = Signature.Get<Position, Rotation>();
        var count   = 10; // 100_000_000 ~ #PC: 1.897 ms
        for (int n = 0; n < count; n++) {
            _ = store.Query(sig);
        }
    }
    
    [Test]
    public static void Test_Query_loop()
    {
        var store   = new EntityStore();
        var entity2 = store.CreateEntity();
        entity2.AddComponent(new Position(1,2,3));
        entity2.AddComponent(new Rotation(4,5,6,7));
        
        var entity3  = store.CreateEntity();
        entity3.AddComponent(new Position(1,2,3));
        entity3.AddComponent(new Rotation(8, 8, 8, 8));
        entity3.AddComponent(new Scale3  (7, 7, 7));
        
        var sig     = Signature.Get<Position, Rotation>();
        _           = store.Query(sig); // for one time allocation for Mem check
        var expect  = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 144 : 160;
        var start   = GetAllocatedBytes();
        var query   = store.Query(sig);
        AssertAlloc(start, expect);
        
        _ = query.Archetypes; // Note: force update of ArchetypeQuery.archetypes[] which resize the array if needed

        var chunkCount   = 0;
        AreEqual("QueryChunks[2]  Components: [Position, Rotation]", query.Chunks.ToString());
        foreach (var (_, _, _) in query.Chunks) { } // force one time allocations
        start = GetAllocatedBytes();
        foreach (var (position, rotation, _) in query.Chunks) {
            AreEqual(3, position[0].z);
            rotation[0].x = 42;
            chunkCount++;
        }
        AssertNoAlloc(start);
        AreEqual(2,  chunkCount);
        AreEqual(42, entity2.Rotation.x);
        
        chunkCount = 0;
        foreach (var (_, rotation, _) in query.Chunks) {
            switch (chunkCount++) {
                case 0:
                    rotation[0].x = 99;
                    break;
            }
        }
        AreEqual(2,  chunkCount);
        AreEqual(99, entity2.Rotation.x);
    }
    
    [Test]
    public static void Test_Query_Chunks_RO()
    {
        var store   = new EntityStore();
        var entity2 = store.CreateEntity();
        entity2.AddComponent(new Position(1, 1, 1));
        entity2.AddComponent(new Rotation(4, 2, 2, 2));
        
        var entity3  = store.CreateEntity();
        entity3.AddComponent(new Position(1, 3, 3));
        entity3.AddComponent(new Rotation(4, 4, 4, 4));
        entity3.AddComponent(new Scale3  (7, 7, 7));
        
        var sig     = Signature.Get<Position, Rotation>();
        var query   = store.Query(sig).ReadOnly<Position>().ReadOnly<Rotation>();
        _ = query.Archetypes; // Note: force update of ArchetypeQuery.archetypes[] which resize the array if needed

        var chunkCount   = 0;
        AreEqual("QueryChunks[2]  Components: [Position, Rotation]", query.Chunks.ToString());
        foreach (var (_, _, _) in query.Chunks) { } // force one time allocations
        var start = GetAllocatedBytes();
        foreach (var (position, rotation, _) in query.Chunks) {
            AreEqual(1, position[0].x);
            AreEqual(4, rotation[0].x);
            position[0].x = 42;
            rotation[0].x = 43;
            chunkCount++;
        }
        AssertNoAlloc(start);
        AreEqual(2,  chunkCount);
        AreEqual(1, entity2.Position.x);
        AreEqual(4, entity2.Rotation.x);
        AreEqual(1, entity3.Position.x);
        AreEqual(4, entity3.Rotation.x);
    }
    
    [Test]
    public static void Test_Query_Archetype_Entities()
    {
        var store   = new EntityStore();
        var archetype = store.GetArchetype(Signature.Get<Position>());
        for (int n = 0; n < 10; n++) {
            archetype.CreateEntity();
        }
        AreEqual(10, archetype.Entities.Count);
        int count = 0;
        foreach (Entity _ in archetype.Entities) {
            count++;
        }
        AreEqual(10, count);
    }
    
    // [Test]
    public static void Test_Position_array_Perf() {
        var positions = new Position[10_000_000];
        for (int n = 0; n < 100; n++) {
            foreach (var position in positions) {
                _ = position;
            }
        }
    }
}

