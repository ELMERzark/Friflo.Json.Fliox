using System;
using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using Friflo.Fliox.Engine.ECS.Database;
using Friflo.Json.Fliox;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;

public static class Test_ComponentReader
{
    internal static readonly JsonValue rootComponents =
        new JsonValue("{ \"pos\": { \"x\": 1, \"y\": 1, \"z\": 1 }, \"scl3\": { \"x\": 2, \"y\": 2, \"z\": 2 } }");
    
    internal static readonly JsonValue childComponents =
        new JsonValue("{ \"pos\": { \"x\": 3, \"y\": 3, \"z\": 3 }, \"scl3\": { \"x\": 4, \"y\": 4, \"z\": 4 } }");
    
    [Test]
    public static void Test_ComponentReader_read_components()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = rootComponents, children = new List<long> { 11 } };
        var childNode   = new DatabaseEntity { pid = 11, components = childComponents };
        
        var root        = converter.DatabaseToGameEntity(rootNode, store, out _);
        var child       = converter.DatabaseToGameEntity(childNode, store, out _);
        AssertRootEntity(root);
        AssertChildEntity(child);
        var type = store.GetArchetype(Signature.Get<Position, Scale3>());
        AreEqual(2,     type.EntityCount);
        AreEqual(2,     store.EntityCount);
        
        // --- read root DatabaseEntity again
        root.Position   = default;
        root.Scale3     = default;
        root            = converter.DatabaseToGameEntity(rootNode, store, out _);
        AssertRootEntity(root);
        AreEqual(2,     type.EntityCount);
        AreEqual(2,     store.EntityCount);
        
        // --- read child DatabaseEntity again
        child.Position  = default;
        child.Scale3    = default;
        child           = converter.DatabaseToGameEntity(childNode, store, out _);
        AssertChildEntity(child);
        AreEqual(2,     type.EntityCount);
        AreEqual(2,     store.EntityCount);
    }
    
    /// <summary>test structure change in <see cref="ComponentReader.SetEntityArchetype"/></summary>
    [Test]
    public static void Test_ComponentReader_change_archetype()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var root        = store.CreateEntity(10);
        root.AddComponent(new Scale3(1, 2, 3));
        IsTrue  (root.HasScale3);
        IsFalse (root.HasPosition);
        
        var rootNode    = new DatabaseEntity { pid = 10, components = rootComponents };
        var rootResult  = converter.DatabaseToGameEntity(rootNode, store, out _);  // archetype changes
        AreSame (root, rootResult);
        IsTrue  (root.HasScale3);   // could change behavior and remove all components not present in DatabaseEntity components
        IsTrue  (root.HasPosition);
    }
    
    /// <summary>test structure change in <see cref="ComponentReader.SetEntityArchetype"/></summary>
    [Test]
    public static void Test_ComponentReader_read_components_null()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;

        var node    = new DatabaseEntity { pid = 10, components = default };
        var entity  = converter.DatabaseToGameEntity(node, store, out var error);
        AreEqual(0, entity.Behaviors.Length + entity.Archetype.ComponentCount);
        IsNull  (error);
    }
    
    /// <summary>test structure change in <see cref="ComponentReader.SetEntityArchetype"/></summary>
    [Test]
    public static void Test_ComponentReader_read_components_empty()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var node    = new DatabaseEntity { pid = 10, components = new JsonValue("{}") };
        var entity  = converter.DatabaseToGameEntity(node, store, out var error);
        AreEqual(0, entity.Behaviors.Length + entity.Archetype.ComponentCount);
        IsNull  (error);
    }
    
    [Test]
    public static void Test_ComponentReader_read_tags()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var node    = new DatabaseEntity { pid = 10, tags = new List<string> { nameof(TestTag) } };
        var entity  = converter.DatabaseToGameEntity(node, store, out _);
        AreEqual(0, entity.Behaviors.Length + entity.Archetype.ComponentCount);
        IsTrue  (entity.Tags.Has<TestTag>());
    }
    
    [Test]
    public static void Test_ComponentReader_read_invalid_component()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var json    = new JsonValue("{ \"pos\": [] }");
        var node    = new DatabaseEntity { pid = 10, components = json };
        var entity  = converter.DatabaseToGameEntity(node, store, out var error);
        NotNull(entity);
        AreEqual("component must be an object. was ArrayStart. id: 10, component: 'pos'", error);
    }
    
    [Test]
    public static void Test_ComponentReader_read_invalid_components()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var node    = new DatabaseEntity { pid = 10, components = new JsonValue("123") };
        var entity  = converter.DatabaseToGameEntity(node, store, out var error);
        NotNull(entity);
        AreEqual("expect 'components' == object or null. id: 10. was: ValueNumber", error);
        
        node        = new DatabaseEntity { pid = 10, components = new JsonValue("invalid") };
        entity      = converter.DatabaseToGameEntity(node, store, out error);
        NotNull(entity);
        AreEqual("unexpected character while reading value. Found: i path: '(root)' at position: 1. id: 10", error);
    }
    
    /// <summary>cover <see cref="ComponentReader.Read"/></summary>
    [Test]
    public static void Test_ComponentReader_DatabaseEntity_assertions()
    {
        {
            var store       = new GameEntityStore(PidType.UsePidAsId);
            var converter   = EntityConverter.Default;
        
            var e = Throws<ArgumentNullException>(() => {
                converter.DatabaseToGameEntity(null, store, out _);
            });
            AreEqual("Value cannot be null. (Parameter 'databaseEntity')", e!.Message);
        } {
            var store       = new GameEntityStore(PidType.UsePidAsId);
            var converter   = EntityConverter.Default;
        
            var childNode   = new DatabaseEntity { pid = int.MaxValue + 1L };
            var e = Throws<ArgumentException>(() => {
                converter.DatabaseToGameEntity(childNode, store, out _);
            });
            AreEqual("pid mus be in range [0, 2147483647]. was: {pid} (Parameter 'databaseEntity')", e!.Message);
        }
    }
    
    internal static void AssertRootEntity(GameEntity root) {
        AreEqual(10,    root.Id);
        AreEqual(1,     root.ChildCount);
        AreEqual(11,    root.ChildNodes.Ids[0]);
        AreEqual(2,     root.Archetype.ComponentCount);
        AreEqual(1f,    root.Position.x);
        AreEqual(1f,    root.Position.y);
        AreEqual(1f,    root.Position.z);
        AreEqual(2f,    root.Scale3.x);
        AreEqual(2f,    root.Scale3.y);
        AreEqual(2f,    root.Scale3.z);
    }
    
    internal static void AssertChildEntity(GameEntity child) {
        AreEqual(11,    child.Id);
        AreEqual(0,     child.ChildCount);
        AreEqual(2,     child.Archetype.ComponentCount);
        AreEqual(3f,    child.Position.x);
        AreEqual(3f,    child.Position.y);
        AreEqual(3f,    child.Position.z);
        AreEqual(4f,    child.Scale3.x);
        AreEqual(4f,    child.Scale3.y);
        AreEqual(4f,    child.Scale3.z);
    }
    
    [NUnit.Framework.IgnoreAttribute("remove childIds reallocation")][Test]
    public static void Test_ComponentReader_read_components_Mem()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = rootComponents, children = new List<long> { 11 } };
        var childNode   = new DatabaseEntity { pid = 11, components = childComponents };
        
        var root        = converter.DatabaseToGameEntity(rootNode, store, out _);
        var child       = converter.DatabaseToGameEntity(childNode, store, out _);
        AssertRootEntity(root);
        AssertChildEntity(child);
        var type = store.GetArchetype(Signature.Get<Position, Scale3>());
        AreEqual(2,     type.EntityCount);
        AreEqual(2,     store.EntityCount);
        
        // --- read same DatabaseEntity again
        root.Position   = default;
        root.Scale3     = default;
        var start       = Mem.GetAllocatedBytes();
        root            = converter.DatabaseToGameEntity(rootNode, store, out _);
        Mem.AssertNoAlloc(start);
        AssertRootEntity(root);
        AssertChildEntity(child);
        AreEqual(2,     type.EntityCount);
        AreEqual(2,     store.EntityCount);
    }
    
    [Test]
    public static void Test_ComponentReader_read_components_Perf()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = rootComponents, children = new List<long> { 11 } };
        
        const int count = 10; // 1_000_000 ~ 2.639 ms (bottleneck parsing JSON to structs)
        for (int n = 0; n < count; n++)
        {
            var root = converter.DatabaseToGameEntity(rootNode, store, out _);
            root.DeleteEntity();
        }
    }
    
    private static readonly JsonValue behavior = new JsonValue("{ \"testRef1\": { \"val1\": 2 } }");
    
    [Test]
    public static void Test_ComponentReader_read_behavior()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = behavior, children = new List<long> { 11 } };

        var root        = converter.DatabaseToGameEntity(rootNode, store, out _);
        AreEqual(1,     root.Behaviors.Length);
        var behavior1   = root.GetBehavior<TestBehavior1>();
        AreEqual(2,     behavior1.val1);
        behavior1.val1      = -1;
        
        // --- read same DatabaseEntity again
        converter.DatabaseToGameEntity(rootNode, store, out _);
        var comp2       = root.GetBehavior<TestBehavior1>();
        AreEqual(2,     comp2.val1);
        AreSame(behavior1, comp2);
    }
    
    [Test]
    public static void Test_ComponentReader_read_behavior_Perf()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = behavior, children = new List<long> { 11 } };

        const int count = 10; // 5_000_000 ~ 8.090 ms   todo check degradation from 3.528 ms
        for (int n = 0; n < count; n++) {
            converter.DatabaseToGameEntity(rootNode, store, out _);
        }
    }
    
    private static readonly JsonValue behaviors = new JsonValue(
        "{ \"testRef1\": { \"val1\": 11 }, \"testRef2\": { \"val2\": 22 }, \"testRef3\": { \"val3\": 33 } }");
    
    /// <summary>Cover <see cref="GameEntityStore.AppendBehavior"/></summary>
    [Test]
    public static void Test_ComponentReader_read_multiple_behaviors()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var rootNode    = new DatabaseEntity { pid = 10, components = behaviors };

        var root        = converter.DatabaseToGameEntity(rootNode, store, out _);
        AreEqual(3,     root.Behaviors.Length);
        var behavior1   = root.GetBehavior<TestBehavior1>();
        AreEqual(11,    behavior1.val1);
        var behavior2   = root.GetBehavior<TestBehavior2>();
        AreEqual(22,    behavior2.val2);
        var behavior3   = root.GetBehavior<TestBehavior3>();
        AreEqual(33,    behavior3.val3);
        
        behavior1.val1      = -1;
        behavior2.val2      = -1;
        behavior3.val3      = -1;
        
        // --- read same DatabaseEntity again
        converter.DatabaseToGameEntity(rootNode, store, out _);
        AreEqual(3,     root.Behaviors.Length);
        behavior1       = root.GetBehavior<TestBehavior1>();
        AreEqual(11,    behavior1.val1);
        behavior2       = root.GetBehavior<TestBehavior2>();
        AreEqual(22,    behavior2.val2);
        behavior3    = root.GetBehavior<TestBehavior3>();
        AreEqual(33,    behavior3.val3);
    }
}

