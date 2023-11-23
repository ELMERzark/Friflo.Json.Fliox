using System;
using System.Diagnostics;
using System.Text;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;
using static Friflo.Fliox.Engine.ECS.EntityStoreBase.Static;

// ReSharper disable InconsistentNaming
#pragma warning disable CS0649 // Field '...' is never assigned to, and will always have its default value

namespace Tests.ECS.GE;

class PlayerRef {
    public Position position;
}

public static class Test_StructComponent
{
    [Test]
    public static void Test_1_TryGetComponentValue() {
        var store   = new EntityStore();
        
        var player1 = store.CreateEntity();
        IsTrue(player1.AddComponent<Scale3>());
        
        var player2 = store.CreateEntity();
        var myComponent1 = new MyComponent1 { a = 1 };
        IsTrue(player2.AddComponent(myComponent1));
        
        var position = new Position { x = 2 };
        IsTrue(player2.AddComponent(position));

        var count = player2.Archetype.ComponentCount;
        AreEqual(2, count);
        
        var success = player2.TryGetComponent(out Position pos);
        IsTrue(success);
        AreEqual(2, pos.x);
        
        success = player2.TryGetComponent(out MyComponent1 rot);
        IsTrue(success);
        AreEqual(1, rot.a);
        
        success = player2.TryGetComponent(out Scale3 _);
        IsFalse(success);
        
        success = player2.TryGetComponent(out MyComponent2 _);
        IsFalse(success);
        //
        var start = Mem.GetAllocatedBytes();
        player2.TryGetComponent(out  pos);
        player2.TryGetComponent(out rot);
        player2.TryGetComponent(out Scale3 _);
        player2.TryGetComponent(out MyComponent2 _);
        Mem.AssertNoAlloc(start);
    }
    
    [Test]
    public static void Test_1_custom_Entity_Extensions() {
        var store   = new EntityStore();
        var player  = store.CreateEntity();
        player.AddComponent<MyComponent1>();
        player.AddComponent<MyComponent2>();
        
        player.MyComponent1().a = 1;
        AreEqual(1, player.MyComponent1().a);
        
        player.MyComponent2().b = 2;
        AreEqual(2, player.MyComponent2().b);
    }
    
    /// <summary>cover <see cref="EntityStoreBase.GetArchetypeWithout"/></summary>
    [Test]
    public static void Test_2_add_remove_struct_component() {
        var store  = new EntityStore();
        var player = store.CreateEntity();
        IsTrue(player.AddComponent(new MyComponent1()));
        IsTrue(player.AddComponent(new Position()));
        AreEqual(2,     player.Archetype.ComponentCount);

        // remove in same order to force creation of new Archetype based on exiting
        IsTrue(player.RemoveComponent<MyComponent1>());
        AreEqual(1,     player.Archetype.ComponentCount);
        
        // Archetype remains unchanged. component is already removed
        IsFalse(player.RemoveComponent<MyComponent1>());
        AreEqual(1,     player.Archetype.ComponentCount);
    }
    
    [Test]
    public static void Test_2_CreateEntity() {
        var store = new EntityStore();
        for (int n = 0; n < 512; n++) {
            var player1 =store.CreateEntity();
            player1.AddComponent<Position>();
        }
    }
    
    [Test]
    public static void Test_3_AddPosition() {
        var store = new EntityStore();
        var player1 = store.CreateEntity();
        IsTrue (player1.AddComponent(new Position { x = 1,  y = 2 }));
        IsFalse(player1.AddComponent(new Position { x = 10, y = 11 }));
        
        var player2 = store.CreateEntity();
        // var start = Mem.GetAllocatedBytes();
        player2.AddComponent<Position>();
        player2.AddComponent<Position>();

        // Mem.AssertNoAlloc(start);  // todo
        AreEqual(10f, player1.Position.x);
        AreEqual(11f, player1.Position.y);
        
        long count = 10; // 1_000_000_000L ~ 967 ms
        for (var n = 0; n < count; n++) {
            // _ = player1.GetComponentValue<Position>();
            _ = player1.Position;
        }
    }
    
    [Test]
    public static void Test_4_GetArchetype() {
        var store   = new EntityStore();
        AreEqual(1, store.Archetypes.Length);
        
        var type1   = store.GetArchetype(Signature.Get<Position, Rotation>());
        var type2   = store.GetArchetype(Signature.Get<Rotation, Position>());
        AreSame(type1, type2);
        
        type1   = store.GetArchetype(Signature.Get<Position, Rotation, MyComponent1>());
        type2   = store.GetArchetype(Signature.Get<MyComponent1, Position, Rotation>());
        AreSame(type1, type2);
        
        type1       = store.GetArchetype(Signature.Get<Position, Rotation, MyComponent1, MyComponent2>());
        type2       = store.GetArchetype(Signature.Get<MyComponent1, Position, Rotation,  MyComponent2>());
        AreSame(type1, type2);
        
        type1       = store.GetArchetype(Signature.Get<Position, Rotation, MyComponent1, Scale3, MyComponent2>());
        type2       = store.GetArchetype(Signature.Get<Scale3, MyComponent1, Position, Rotation, MyComponent2>());
        AreSame(type1, type2);
        
        AreEqual(5, store.Archetypes.Length);
        AreEqual(0, type1.EntityCount);
        AreEqual(0, type2.EntityCount);
    }
    
    [Test]
    public static void Test_5_AddPositionRotation() {
        var store       = new EntityStore();
        var posType     = store.GetArchetype(Signature.Get<Position>());
        var posRotType  = store.GetArchetype(Signature.Get<Position, Rotation>());
        AreEqual(1,                             posType.Structs.Count);
        AreEqual(2,                             posRotType.Structs.Count);
        
        var player1  = store.CreateEntity();
        AreEqual("[]",      player1.Archetype.ToString());
        
        var position = new Position { x = 1 };
        player1.AddComponent(position);
        AreEqual(1f,        player1.Position.x);
        AreEqual(1,         posType.EntityCount);
        AreEqual(1,         posType.EntityIds.Length);
        AreEqual("[Position]  Count: 1", posType.ToString());
        
        
        player1.AddComponent<Rotation>(); // changes Archetype of player1
        AreEqual(0,         posType.EntityCount);
        AreEqual(1f,        player1.Position.x);
        
        var player2 = store.CreateEntity();
        player2.AddComponent<Position>();
        AreEqual(1,         posType.EntityCount);
        
        player2.AddComponent<Rotation>();   // changes Archetype of player2
        AreEqual(0,         posType.EntityCount);
        AreEqual(2,         posRotType.EntityCount);
        AreEqual(2,         store.EntityCount);
        var node = store.Nodes[1];          // check node fields are reset to default values
        AreSame (player1,   node.Entity);
        AreEqual(0,         node.ChildCount);
        AreEqual(0,         node.ChildIds.Length);
        AreEqual(NoParentId,node.ParentId);
        AreEqual("Count: 2", store.ToString());
        
        long count = 10; // 1_000_000_000L ~ 969 ms
        for (var n = 0; n < count; n++) {
            // _ = player1.GetComponentValue<Position>();
            _ = player1.Position;
        }
    }
    
    [Test]
    public static void Test_HasPosition_Perf() {
        var store   = new EntityStore();
        var player  = store.CreateEntity();
        player.AddComponent(new Position());
        IsTrue(player.HasPosition);
        
        const long count = 10; // 10_000_000_000L ~ 2.374 ms
        for (long n = 0; n < count; n++) {
            _ = player.HasPosition;
        }
    }
    
    [Test]
    public static void Test_GetComponent_Perf() {
        var store   = new EntityStore();
        var player  = store.CreateEntity();
        player.AddComponent(new MyComponent1());
        IsTrue(player.HasComponent<MyComponent1>());
        
        const long count = 10; // 10_000_000_000L ~ 5.556 ms
        for (long n = 0; n < count; n++) {
            _ = player.HasComponent<MyComponent1>();
        }
    }
    
    [Test]
    public static void Test_GetPositionField() {
        var player = new PlayerRef();
        long count = 10; // 1_000_000_000L ~ 386 ms
        for (var n = 0; n < count; n++) {
            _ = player.position;
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete

    [Test]
    public static void Test_6_AddRotation() {
        var store   = new EntityStore();
        var type    = store.GetArchetype(Signature.Get<Rotation, Scale3>());
        
        var player = store.CreateEntity();
        AreEqual(0,     player.Components_.Length);
        
        var rotation = new Rotation { x = 1, y = 2 };
        player.AddComponent(rotation);
        var scale    = new Scale3   { x = 3, y = 4 };
        player.AddComponent(scale);
        AreEqual(1,     type.EntityCount);
        AreEqual(1f,    player.Rotation.x);
        AreEqual(2f,    player.Rotation.y);
        AreEqual(3f,    player.Scale3.x);
        AreEqual(4f,    player.Scale3.y);
        
        var components  =       player.Components_;
        AreEqual(2,             components.Length);
        AreEqual("1, 2, 0, 0",  components[0].ToString());
        AreEqual("3, 4, 0",     components[1].ToString());
    }
#pragma warning restore CS0618 // Type or member is obsolete
    
    /// <summary>Test
    /// <see cref="Archetype.MoveEntityTo"/>
    /// <see cref="StructHeap{T}.MoveComponent"/>
    /// </summary>
    [Test]
    public static void Test_7_MoveComponent() {
        var store   = new EntityStore();
        AreEqual(1, store.Archetypes.Length);
        
        var player1 = store.CreateEntity();
        var position1 = new Position { x = 1 };
        player1.AddComponent(position1);
        
        var player2 = store.CreateEntity();
        var position2 = new Position { x = 2 };
        player2.AddComponent(position2);
        
        var rotation1 = new Rotation { x = 3 };
        player1.AddComponent(rotation1); // adding Rotation changes Archetype
        
        AreEqual(1f, player1.Position.x);
        AreEqual(2f, player2.Position.x);
    }

    [Test]
    public static void Test_8_ModifyComponent() {
        var store = new EntityStore();
        var player = store.CreateEntity();
        player.AddComponent<Position>();
        // set via GetComponent<>()
        ref var pos = ref player.GetComponent<Position>();
        pos.x = 1;
        // read via Property
        var p2 = player.Position;
        AreEqual(1, p2.x);
    }
    
    [Test]
    public static void Test_9_TestMissingAttribute() {
        var store = new EntityStore();
        var player = store.CreateEntity();
        var e1 = Throws<InvalidOperationException>(() => {
            player.AddComponent<MyInvalidComponent>();
        });
        AreEqual("Missing attribute [StructComponent(\"<key>\")] on type: Tests.ECS.MyInvalidComponent", e1!.Message);
        
        var e2 = Throws<NullReferenceException>(() => {
            player.GetComponent<MyInvalidComponent>();
        });
        AreEqual("Object reference not set to an instance of an object.", e2!.Message);
        
        // throws currently no exception
        player.RemoveComponent<MyInvalidComponent>();
    }
    
    /// <summary>Similar to <see cref="Raw.Test_RawEntities.Test_RawEntities_Components"/></summary>
    [Test]
    public static void Test_9_RemoveComponent() {
        var store   = new EntityStore();
        var type1 = store.GetArchetype(Signature.Get<Position>());
        var type2 = store.GetArchetype(Signature.Get<Position, Rotation>());
        
        var entity1  = store.CreateEntity();
        entity1.AddComponent(new Position { x = 1 });
        AreEqual(1,     type1.EntityCount);
        AreEqual(1,     entity1.Archetype.ComponentCount);
        
        entity1.RemoveComponent<Position>();
        AreEqual(0,     type1.EntityCount);
        AreEqual(0,     entity1.Archetype.ComponentCount);
        
        entity1.AddComponent(new Position { x = 1 });
        AreEqual(1,     type1.EntityCount);
        AreEqual(1,     entity1.Archetype.ComponentCount);
        
        entity1.AddComponent(new Rotation { x = 2 });
        AreEqual(0,     type1.EntityCount);
        AreEqual(1,     type2.EntityCount);
        AreEqual(2,     entity1.Archetype.ComponentCount);
        
        entity1.RemoveComponent<Rotation>();
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     type2.EntityCount);
        AreEqual(1f,    entity1.Position.x);
        AreEqual(1,     entity1.Archetype.ComponentCount);
        //
        var entity2  = store.CreateEntity();
        entity2.AddComponent(new Position { x = 1 });   // possible alloc: resize type1.entityIds
        entity2.RemoveComponent<Position>();            // note: remove the last id in type1.entityIds => only type1.entityCount--  
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     entity2.Archetype.ComponentCount);
        
        var start = Mem.GetAllocatedBytes();
        entity2.AddComponent(new Position { x = 1 });
        entity2.RemoveComponent<Position>();
        Mem.AssertNoAlloc(start);
        
        AreEqual(1,     type1.EntityCount);
        AreEqual(0,     entity2.Archetype.ComponentCount);
    }
    
    [Test]
    public static void Test_9_Add_Remove_Component_Perf() {
        var store   = new EntityStore();
        var posType = store.GetArchetype(Signature.Get<Position>());
        store.CreateEntity().AddComponent<Position>();
        store.CreateEntity().AddComponent<Position>();
        store.CreateEntity().AddComponent<Position>();
        
        var entity  = store.CreateEntity();
        entity.AddComponent<Position>();    // force resize type1.entityIds
        
        var start = Mem.GetAllocatedBytes();
        int count = 10; // 100_000_000 ~ 6.300 ms
        for (var n = 0; n < count; n++) {
            entity.AddComponent<Position>();
            entity.RemoveComponent<Position>();
        }
        Mem.AssertNoAlloc(start);
        AreEqual(3, posType.EntityCount);
    }
    
    [Test]
    public static void Test_9_Set_Name() {
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        IsFalse(entity.HasName);
        IsFalse(entity.HasPosition);
        IsFalse(entity.HasRotation);
        IsFalse(entity.HasScale3);
        IsFalse(entity.HasComponent<EntityName>());
        AreEqual("id: 1  []",           entity.ToString());
        
        entity.AddComponent(new EntityName("Hello"));
        AreEqual("EntityName: 'Hello'", entity.GetComponent<EntityName>().ToString());
        IsTrue(entity.HasName);
        IsTrue(entity.HasComponent<EntityName>());
        AreEqual("id: 1  \"Hello\"",    entity.ToString());
        
        AreEqual("Hello",               entity.Name.value);
        AreEqual("Hello",               Encoding.UTF8.GetString(entity.Name.Utf8));
        
        entity.Name.value = null;
        AreEqual("id: 1  [EntityName]", entity.ToString());
    }
    
    [Test]
    public static void Test_StructComponent_EntityStore_creation_Perf() {
        _ = new EntityStore();
        var stopwatch =  new Stopwatch();
        stopwatch.Start();
        int count = 10; // 1_000_000 ~ 454 ms
        for (int n = 0; n < count; n++) {
            _ = new EntityStore();
        }
        Console.WriteLine($"EntityStore count: {count}, duration: {stopwatch.ElapsedMilliseconds} ms");
    }
}

