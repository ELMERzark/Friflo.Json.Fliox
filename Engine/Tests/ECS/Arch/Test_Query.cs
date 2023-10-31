using System.Runtime.InteropServices;
using Friflo.Fliox.Engine.ECS;
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
        AreEqual("Signature: [Position]",           sig1.ToString());
        AreEqual("Structs: [Position]",             sig1.Structs.ToString());
        
        var sig2            = Signature.Get<Position, Rotation>();
        AreEqual("Signature: [Position, Rotation]", sig2.ToString());

        int count = 0;
        foreach (var _ in sig2.Structs) {
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
        
        AreEqual("Structs: [Position]",                                                sig1.Structs.ToString());
        AreEqual("Structs: [Position, Rotation]",                                      sig2.Structs.ToString());
        AreEqual("Structs: [Position, Rotation, Scale3]",                              sig3.Structs.ToString());
        AreEqual("Structs: [Position, Rotation, Scale3, MyComponent1]",                sig4.Structs.ToString());
        AreEqual("Structs: [Position, Rotation, Scale3, MyComponent1, MyComponent2]",  sig5.Structs.ToString());
    }
    [Test]
    public static void Test_generic_Query_with_AllTags()
    {
        var store   = new GameEntityStore();
        var query1 =    store.Query<Position>();
        var query2 =    store.Query<Position, Rotation>();
        var query3 =    store.Query<Position, Rotation, Scale3>();
        var query4 =    store.Query<Position, Rotation, Scale3, MyComponent1>();
        var query5 =    store.Query<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        
        AreEqual("Query: [Position]",                                               query1.ToString());
        AreEqual("Query: [Position, Rotation]",                                     query2.ToString());
        AreEqual("Query: [Position, Rotation, Scale3]",                             query3.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1]",               query4.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2]", query5.ToString());
        
        var tags = Tags.Get<TestTag>();
        AreEqual("Query: [Position, #TestTag]",                                                 query1.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, #TestTag]",                                       query2.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, #TestTag]",                               query3.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, #TestTag]",                 query4.AllTags(tags).ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2, #TestTag]",   query5.AllTags(tags).ToString());
    }
    
    [Test]
    public static void Test_Signature_Query()
    {
        var store   = new GameEntityStore();
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
        
        AreEqual("Query: [Position]", query1.ToString());
        AreEqual("Query: [Position, Rotation]", query2.ToString());
        AreEqual("Query: [Position, Rotation, Scale3]", query3.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1]", query4.ToString());
        AreEqual("Query: [Position, Rotation, Scale3, MyComponent1, MyComponent2]", query5.ToString());
        
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
    public static void Test_Query_creation_Perf()
    {
        var store   = new GameEntityStore();
        var sig     = Signature.Get<Position, Rotation>();
        var count   = 10; // 100_000_000 ~ 1.897 ms
        for (int n = 0; n < count; n++) {
            _ = store.Query(sig);
        }
    }
#if COMP_ITER
    [Test]
    public static void Test_Query_ForEach()
    {
        var store   = new GameEntityStore();
        var entity  = store.CreateEntity();
        entity.AddComponent(new Position(1,2,3));
        entity.AddComponent(new Rotation(4,5,6,7));
        
        var entity3  = store.CreateEntity();
        entity3.AddComponent(new Position(1,2,3));
        entity3.AddComponent(new Rotation(8, 8, 8, 8));
        entity3.AddComponent(new Scale3  (7, 7, 7));
        
        var sig     = Signature.Get<Position, Rotation>();
        var query   = store.Query(sig);
        var count   = 0;
        var forEach = query.ForEach((position, rotation) => {
            count++;
            AreEqual(3, position.Value.z);
            rotation.Value.x = 42;
        });
        AreEqual("ForEach: [Position, Rotation]", forEach.ToString());
        forEach.Run();
        AreEqual(2,     count);
        AreEqual(42,    entity.Rotation.x);
    }
    
    [Test]
    public static void Test_Query_ForEach_RO()
    {
        var store   = new GameEntityStore();
        var entity  = store.CreateEntity();
        entity.AddComponent(new Position(1,2,3));
        entity.AddComponent(new Rotation(4,5,6,7));
        
        var sig     = Signature.Get<Position, Rotation>();
        var query   = store.Query(sig).ReadOnly<Position>().ReadOnly<Rotation>();
        var count   = 0;
        var forEach = query.ForEach((position, rotation) => {
            // ReSharper disable once AccessToModifiedClosure
            count++;
            position.Value.x = 42;
            rotation.Value.x = 43;
        });
        _           = query.Archetypes; // update Archetypes for subsequent Mem check
        var start   = GetAllocatedBytes();
        forEach.Run();
        AssertNoAlloc(start);
        AreEqual(1,     count);
        AreEqual(1,     entity.Position.x);
        AreEqual(4,     entity.Rotation.x);
    }
#endif
    
    [Test]
    public static void Test_Query_loop()
    {
        var store   = new GameEntityStore();
        var entity2  = store.CreateEntity();
        entity2.AddComponent(new Position(1,2,3));
        entity2.AddComponent(new Rotation(4,5,6,7));
        
        var entity3  = store.CreateEntity();
        entity3.AddComponent(new Position(1,2,3));
        entity3.AddComponent(new Rotation(8, 8, 8, 8));
        entity3.AddComponent(new Scale3  (7, 7, 7));
        
        var sig     = Signature.Get<Position, Rotation>();
        _           = store.Query(sig); // for one time allocation for Mem check
        var expect  = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? 112 : 128;
        var start   = GetAllocatedBytes();
        var query   = store.Query(sig);
        AssertAlloc(start, expect);
        
        _ = query.Archetypes; // Note: force update of ArchetypeQuery.archetypes[] which resize the array if needed
#if COMP_ITER
        start       = GetAllocatedBytes();
        var count   = 0;
        foreach (var (position, rotation) in query) {
            AreEqual(3, position.Value.z);
            rotation.Value.x = 42;
            count++;
        }
        AssertNoAlloc(start);
        AreEqual(2,  count);
        AreEqual(42, entity2.Rotation.x);
#endif
        var chunkCount   = 0;
        AreEqual("Chunks: [Position, Rotation]", query.Chunks.ToString());
        start = GetAllocatedBytes();
        foreach (var (position, rotation) in query.Chunks) {
            AreEqual(3, position.Values[0].z);
            rotation.Values[0].x = 42;
            chunkCount++;
        }
        AssertNoAlloc(start);
        AreEqual(2,  chunkCount);
        AreEqual(42, entity2.Rotation.x);
    }
    
    [Test]
    public static void Test_Query_Chunks_RO()
    {
        var store   = new GameEntityStore();
        var entity2  = store.CreateEntity();
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
        AreEqual("Chunks: [Position, Rotation]", query.Chunks.ToString());
        var start = GetAllocatedBytes();
        foreach (var (position, rotation) in query.Chunks) {
            AreEqual(1, position.Values[0].x);
            AreEqual(4, rotation.Values[0].x);
            position.Values[0].x = 42;
            rotation.Values[0].x = 43;
            chunkCount++;
        }
        AssertNoAlloc(start);
        AreEqual(2,  chunkCount);
        AreEqual(1, entity2.Position.x);
        AreEqual(4, entity2.Rotation.x);
        AreEqual(1, entity3.Position.x);
        AreEqual(4, entity3.Rotation.x);
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
