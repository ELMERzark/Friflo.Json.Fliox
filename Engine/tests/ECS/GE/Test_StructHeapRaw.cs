using System;
using System.Diagnostics;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;


// ReSharper disable ConvertToConstant.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable
// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;

public static class Test_StructHeapRaw
{
    [Test]
    public static void Test_StructHeapRaw_increase_entity_capacity()
    {
        var store       = new RawEntityStore();
        var arch        = store.GetArchetype(Signature.Get<Position>());
        int count       = 16384; // 16384 ~ 6 ms    8388608 ~ 384 ms
        var ids         = new int[count];
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int n = 0; n < count; n++)
        {
            var id  = store.CreateEntity(arch);
            ids[n]  = id;
            var entityArch = store.GetEntityArchetype(id);
            if (arch != entityArch)             Mem.FailAreEqual(arch, entityArch);
            if (n + 1 != arch.EntityCount)      Mem.FailAreEqual(n + 1, arch.EntityCount);
            ref var pos = ref store.GetEntityComponent<Position>(id);
            if (default != pos)                 Mem.FailAreEqual(default, pos);
            pos.x = n;
        }
        Console.WriteLine($"CreateEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        Mem.AreEqual(count, arch.Capacity);
        for (int n = 0; n < count; n++) {
            var x = (int)store.GetEntityComponent<Position>(ids[n]).x;
            if (n != x)                         Mem.FailAreEqual(n, x);
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_shrink_entity_capacity()
    {
        var store       = new RawEntityStore();
        var arch        = store.GetArchetype(Signature.Get<Position>());
        int count       = 16384; // 16384 ~ 0-1 ms     8388608 ~ 192 ms
        var ids         = new int[count];
        for (int n = 0; n < count; n++)
        {
            var id = store.CreateEntity(arch);
            ids[n] = id;
            store.GetEntityComponent<Position>(id).x = n;
        }
        // --- delete majority of entities
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        const int remaining = 500;
        for (int n = remaining; n < count; n++)
        {
            store.DeleteEntity(ids[n]);
            var expectCount = count + remaining - n - 1;
            if (expectCount != arch.EntityCount)    Mem.FailAreEqual(expectCount, arch.EntityCount);
        }
        Console.WriteLine($"DeleteEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        Mem.AreEqual(1024, arch.Capacity);
        for (int n = 0; n < remaining; n++) {
            Mem.AreEqual(n, store.GetEntityComponent<Position>(ids[n]).x);
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_CreateEntity_Perf()
    {
        for (int o = 0; o < 1; o++) {
            var store   = new RawEntityStore();
            var arch1   = store.GetArchetype(Signature.Get<ByteComponent>());
            _ = store.CreateEntity(arch1); // warmup
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int count       = 1000;
            //   1_000_000 ~   24 ms
            //  10_000_000 ~  276 ms
            // 100_000_000 ~ 1862 ms
            // 500_000_000 ~ 9257 ms
            for (int n = 0; n < count; n++) {
                _ = store.CreateEntity(arch1);
            }
            Console.WriteLine($"CreateEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
            Mem.AreEqual(count + 1, arch1.EntityCount);
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_DeleteEntity_Perf()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position>());
        int count   = 10; // 10_000_000 ~ 217 ms
        for (int n = 0; n < count; n++) {
            _ = store.CreateEntity(arch1);
        }
        Mem.AreEqual(count, arch1.EntityCount);
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        for (int n = 1; n <= count; n++) {
            store.DeleteEntity(n);
        }
        Console.WriteLine($"DeleteEntity() - raw. count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        Mem.AreEqual(0, arch1.EntityCount);
    }
    
    [Test]
    public static void Test_StructHeapRaw_for_array_Reference()
    {
        var positions = new Position[Count];
        for (int n = 0; n < Count; n++) {
            positions[n].x = n;
        }
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var i = 0;
        // 10_000_000 ~ 9 ms
        for (int n = 0; n < Count; n++) {
            var x = (int)positions[n].x;
            if (x != n)             Mem.FailAreEqual(x, n);
            i++;
        }
        Mem.AreEqual(Count, i);
        Console.WriteLine($"Iterate_Ref. count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms.");
    }
    
    /// <summary>
    /// Test cases that need to be covered: <br/>
    /// 0 <br/>
    /// 1 <br/>
    /// <see cref="StructInfo.ChunkSize"/> - 1<br/>
    /// <see cref="StructInfo.ChunkSize"/> <br/>
    /// <see cref="StructInfo.ChunkSize"/> + 1 <br/>
    /// 2 * <see cref="StructInfo.ChunkSize"/> - 1<br/>
    /// 2 * <see cref="StructInfo.ChunkSize"/> <br/>
    /// 2 * <see cref="StructInfo.ChunkSize"/> + 1 <br/>
    /// </summary>
    private const int QueryCount = 2 * StructInfo.ChunkSize + 1;
    
#if COMP_ITER
    [Test]
    public static void Test_StructHeapRaw_Query_foreach()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position, Rotation>());
        var query   = store.Query(Signature.Get<Position, Rotation>());
        for (int count = 0; count < QueryCount; count++) {
            int n = 0;
            foreach (var (position, rotation) in query) {
                var x = (int)position.Value.x;
                if (x != n)         Mem.FailAreEqual(x, n);
                n++;
            }
            if (count != n)         Mem.FailAreEqual(count, n);
            var id = store.CreateEntity(arch1);
            store.GetEntityComponent<Position>(id).x = count;
        }
        Mem.AreEqual(QueryCount, arch1.EntityCount);
    }
    
    [Test]
    public static void Test_StructHeapRaw_Query_ForEach()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position, Rotation>());
        var query   = store.Query(Signature.Get<Position, Rotation>());
        for (int count = 0; count < QueryCount; count++) {
            int n = 0;
            var forEach     = query.ForEach((position, rotation) => {
                var x = (int)position.Value.x;
                if (x != n)         Mem.FailAreEqual(x, n);
                n++;
            });
            forEach.Run();
            Mem.AreEqual(count, n);
            var id = store.CreateEntity(arch1);
            store.GetEntityComponent<Position>(id).x = count;
        }
        Mem.AreEqual(QueryCount, arch1.EntityCount);
    }
#endif
    
    [Test]
    public static void Test_StructHeapRaw_Query_Chunks()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<Position, Rotation>());
        var query   = store.Query(Signature.Get<Position, Rotation>());
        for (int count = 0; count < QueryCount; count++) {
            int n = 0;
            foreach (var (positionChunk, rotationChunk) in query.Chunks) {
                foreach (var position in positionChunk.Values) {
                    var x = (int)position.x;
                    if (x != n)     Mem.FailAreEqual(x, n);
                    n++;
                }
            }
            Mem.AreEqual(count, n);
            var id = store.CreateEntity(arch1);
            store.GetEntityComponent<Position>(id).x = count;
        }
        Mem.AreEqual(QueryCount, arch1.EntityCount);
    }
    
    /// use greater than <see cref="StructInfo.ChunkSize"/> for coverage
    private const int Count = 10_000; // 10_000  /  10_000_000
    
    [Test]
    public static void Test_StructHeapRaw_Query_Perf()
    {
        var store   = new RawEntityStore();
        var arch1   = store.GetArchetype(Signature.Get<MyComponent1, MyComponent2>());
         // 10_000_000
        //      CreateEntity()              ~  390 ms
        //      for GetEntityComponent<>()  ~   52 ms (good performance only, because archetypes remain unchanged after e 
        //      foreach Query()             ~  144 ms
        //      Query.ForEach()             ~   98 ms
        //      foreach Query.Chunks        ~    6 ms
        var query   = store.Query(Signature.Get<MyComponent1, MyComponent2>());
#if COMP_ITER
        foreach (var (position, rotation) in query) { }     // force one time allocation
        query.ForEach((position, rotation) => {}).Run();    // warmup
#endif
        foreach (var _ in query.Chunks) { }                 // warmup
        {
            // _ = store.CreateEntity(arch1); // warmup
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int n = 0; n < Count; n++) {
                var id = store.CreateEntity(arch1);
                store.GetEntityComponent<MyComponent1>(id).a = n;
            }
            Console.WriteLine($"CreateEntity() - raw. count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms");
            Mem.AreEqual(Count, arch1.EntityCount);
        } {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int n = 1; n < Count; n++) {
                ref var component1 = ref store.GetEntityComponent<MyComponent1>(n);                
                var x = component1.a;
                if (x != n - 1)     Mem.FailAreEqual(x, n - 1);
            }
            Console.WriteLine($"for GetEntityComponent<>(). count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        }
#if COMP_ITER
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int n           = 0;
            var memStart    = Mem.GetAllocatedBytes();
            foreach (var (component1, component2) in query) {
                var x = component1.Value.a;
                if (x != n)         Mem.FailAreEqual(x, n);
                n++;
            }
            Mem.AssertNoAlloc(memStart);
            Mem.AreEqual(Count, n);
            Console.WriteLine($"foreach Query(). count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms");
        } {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int n           = 0;
            var memStart    = Mem.GetAllocatedBytes();
            var forEach     = query.ForEach((component1, component2) => {
                var x = component1.Value.a;
                if (x != n)         Mem.FailAreEqual(x, n);
                n++;
            });
            forEach.Run();
            var diff = Mem.GetAllocatedBytes() - memStart;
            Mem.AreEqual(Count, n);
            Console.WriteLine($"Query.ForEach(). count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms.  Alloc: {diff}");
        }
#endif
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            int n           = 0;
            var memStart    = Mem.GetAllocatedBytes();
            foreach (var (component1Chunk, component2Chunk) in query.Chunks) {
                foreach (var component1 in component1Chunk.Values) {
                    var x = component1.a;
                    if (x !=n)      Mem.FailAreEqual(x, n);
                    n++;
                }
            }
            var diff = Mem.GetAllocatedBytes() - memStart;
            Mem.AreEqual(Count, n);
            Console.WriteLine($"foreach Query.Chunks. count: {Count}, duration: {stopwatch.ElapsedMilliseconds} ms.  Alloc: {diff}");
        }
    }
    
    [Test]
    public static void Test_StructHeapRaw_DeleteEntity_twice()
    {
        var store   = new RawEntityStore();
        var arch    = store.GetArchetype(Signature.Get<Position>());
        var entity  = store.CreateEntity(arch);
        Mem.AreEqual(1,     store.EntityCount);
        Mem.AreEqual(1,     arch.EntityCount);
        store.DeleteEntity(entity);
        Mem.AreEqual(0,     store.EntityCount);
        Mem.AreEqual(0,     arch.EntityCount);
        store.DeleteEntity(entity);
        Mem.AreEqual(0,     store.EntityCount);
        Mem.AreEqual(0,     arch.EntityCount);
    }
    
    [Test]
    public static void Test_StructHeapRaw_invalid_store()
    {
        var store1      = new RawEntityStore();
        var store2      = new RawEntityStore();
        var arch1       = store1.GetArchetype(Signature.Get<Position>());
        var e = Assert.Throws<ArgumentException>(() => {
            store2.CreateEntity(arch1);
        });
        Mem.AreEqual("entity is owned by a different store (Parameter 'archetype')", e!.Message);
    }
}

