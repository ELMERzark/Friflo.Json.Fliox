using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Arch;

public static class Test_ArchetypeStructs
{
    [Test]
    public static void Test_ArchetypeStructs_basics()
    {
        var twoStructs = ArchetypeStructs.Get<Position, Rotation>();
        AreEqual("Structs: [Position, Rotation]",  twoStructs.ToString());
        
        var structs    = new ArchetypeStructs();
        AreEqual("Structs: []",                    structs.ToString());
        IsFalse(structs.Has<Position>());
        IsFalse(structs.HasAll(twoStructs));
        IsFalse(structs.HasAny(twoStructs));
        
        structs.Add<Position>();
        IsTrue (structs.Has<Position>());
        IsFalse(structs.HasAll(twoStructs));
        IsTrue (structs.HasAny(twoStructs));
        
        AreEqual("Structs: [Position]",            structs.ToString());
        
        structs.Add<Rotation>();
        AreEqual("Structs: [Position, Rotation]",  structs.ToString());
        IsTrue (structs.Has<Position, Rotation>());
        IsFalse(structs.Has<Position, Rotation, Scale3>());
        IsTrue (structs.HasAll(twoStructs));
        IsTrue (structs.HasAny(twoStructs));

        var copy = new ArchetypeStructs();
        copy.Add(structs);
        AreEqual("Structs: [Position, Rotation]",  copy.ToString());
        
        copy.Remove<Position>();
        AreEqual("Structs: [Rotation]",            copy.ToString());
        
        copy.Remove(structs);
        AreEqual("Structs: []",                    copy.ToString());
    }
    
    [Test]
    public static void Test_ArchetypeStructs_Get()
    {
        var schema = EntityStore.GetComponentSchema();
        AreEqual(3, schema.EngineDependants.Length);
        var engine = schema.EngineDependants[0];
        AreEqual("Engine.ECS.dll",  engine.Assembly.ManifestModule.Name);
        AreEqual("Engine.ECS.dll",  engine.ToString());
        AreEqual(6,                 engine.Types.Length);
        foreach (var type in engine.Types) {
            AreSame(engine.Assembly, type.type.Assembly);
        }
        
        var testStructType  = schema.ComponentTypeByType[typeof(Position)];
        
        var struct1    = ArchetypeStructs.Get<Position>();
        AreEqual("Structs: [Position]", struct1.ToString());
        int count1 = 0;
        foreach (var structType in struct1) {
            AreSame(testStructType, structType);
            count1++;
        }
        AreEqual(1, count1);
        
        var count2 = 0;
        var struct2 = ArchetypeStructs.Get<Position, Rotation>();
        AreEqual("Structs: [Position, Rotation]", struct2.ToString());
        foreach (var _ in struct2) {
            count2++;
        }
        AreEqual(2, count2);
        
        AreEqual(struct2, ArchetypeStructs.Get<Position, Rotation>());
    }
    
    [Test]
    public static void Test_ArchetypeStructs_Get_Mem()
    {
        var struct1    = ArchetypeStructs.Get<Position>();
        foreach (var _ in struct1) { }
        
        // --- 1 struct
        var start   = Mem.GetAllocatedBytes();
        int count1 = 0;
        foreach (var _ in struct1) {
            count1++;
        }
        Mem.AssertNoAlloc(start);
        AreEqual(1, count1);
        
        // --- 2 structs
        start       = Mem.GetAllocatedBytes();
        var struct2 = ArchetypeStructs.Get<Position, Rotation>();
        var count2 = 0;
        foreach (var _ in struct2) {
            count2++;
        }
        Mem.AssertNoAlloc(start);
        AreEqual(2, count2);
    }
    
    [Test]
    public static void Test_ArchetypeStructs_Enumerator_Reset()
    {
        var structs    = ArchetypeStructs.Get<Position>();
        var enumerator = structs.GetEnumerator();
        while (enumerator.MoveNext()) { }
        enumerator.Reset();
        int count = 0;
        while (enumerator.MoveNext()) {
            count++;
        }
        AreEqual(1, count);
        enumerator.Dispose();
    }
    
    [Test]
    public static void Test_ArchetypeStructs_generic_IEnumerator()
    {
        IEnumerable<ComponentType> tags = ArchetypeStructs.Get<Position>();
        int count = 0;
        foreach (var _ in tags) {
            count++;
        }
        AreEqual(1, count);
    }
    
    [Test]
    public static void Test_ArchetypeStructs_Tags()
    {
        var store       = new GameEntityStore();
        var type        = store.GetArchetype(Tags.Get<TestTag2, TestTag3>());
        var tags        = type.Tags;
        AreEqual(2, tags.Count);
        var enumerator =  tags.GetEnumerator();
        IsTrue(enumerator.MoveNext());
        AreEqual(typeof(TestTag2), enumerator.Current!.type);
        
        IsTrue(enumerator.MoveNext());
        AreEqual(typeof(TestTag3), enumerator.Current!.type);
        
        IsFalse(enumerator.MoveNext());
        enumerator.Dispose();
    }
    
    [Test]
    public static void Test_ArchetypeStructs_lookup_structs_and_tags_Perf()
    {
        var store   = new GameEntityStore();
        var type1   = store.GetArchetype(Signature.Get<Position>());
        var result  = store.FindArchetype(type1.Structs, type1.Tags);
        AreEqual(1, type1.Structs.Count);
        AreSame (type1, result);
        
        var start   = Mem.GetAllocatedBytes();
        var structs = type1.Structs;
        var tags    = type1.Tags;
        var count   = 10; // 100_000_000 ~ 1.707 ms
        for (int n = 0; n < count; n++)
        {
            store.FindArchetype(structs, tags);
        }
        Mem.AssertNoAlloc(start);
    }
}
