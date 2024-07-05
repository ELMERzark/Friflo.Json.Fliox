﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Friflo.Engine.ECS;
using NUnit.Framework;
using Tests.Examples;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantExplicitArrayCreation
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Index {

public static class Test_Index
{
    [Test]
    public static void Test_Index_ValueInRange_ValueStructIndex()
    {
        var store = new EntityStore();
        var values = store.GetAllIndexedComponentValues<IndexedInt, int>();
        for (int n = 1; n <= 10; n++) {
            var entity = store.CreateEntity(n);
            entity.AddComponent(new IndexedInt { value = n });
            AreEqual(n, values.Count);
        }
        var query  = store.Query().ValueInRange<IndexedInt, int>(3, 8);
        AreEqual(6, query.Count);
        AreEqual("{ 3, 4, 5, 6, 7, 8 }", query.Entities.Debug());
    }
    
    [Test]
    public static void Test_Index_ValueInRange_ValueClassIndex()
    {
        var store = new EntityStore();
        var values = store.GetAllIndexedComponentValues<IndexedName, string>();
        for (int n = 1; n <= 10; n++) {
            var entity = store.CreateEntity(n);
            entity.AddComponent(new IndexedName { name = n.ToString() });
            AreEqual(n, values.Count);
        }
        var query  = store.Query().ValueInRange<IndexedName, string>("3", "8");
        AreEqual(6, query.Count);
        AreEqual("{ 3, 4, 5, 6, 7, 8 }", query.Entities.Debug());
    }
    
    [Test]
    public static void Test_Index_Component_Update()
    {
        var store = new EntityStore();

        var entities = new List<Entity>();
        for (int n = 0; n < 10; n++) {
            var entity = store.CreateEntity(new Position(n, 0, 0));
            entities.Add(entity);
        }
        var entity1 = entities[0];
        var entity2 = entities[1];
        var entity3 = entities[2];
        var nameValues  = store.GetAllIndexedComponentValues<IndexedName, string>();
        var intValues   = store.GetAllIndexedComponentValues<IndexedInt, int>();
        
        entity1.AddComponent(new IndexedName   { name   = "find-me1" });    AreEqual("{ find-me1 }",    nameValues.Debug());
        entity2.AddComponent(new IndexedInt    { value  = 123        });    AreEqual("{ 123 }",         intValues.Debug());
        entity3.AddComponent(new IndexedName   { name   = "find-me1" });    AreEqual("{ find-me1 }",    nameValues.Debug());
        entity3.AddComponent(new IndexedInt    { value  = 123        });    AreEqual("{ 123 }",         intValues.Debug());

        AreEqual("{ 1, 3 }",store.GetEntitiesWithComponentValue<IndexedName, string>("find-me1").Debug());
        AreEqual("{ 2, 3 }",store.GetEntitiesWithComponentValue<IndexedInt, int>(123).Debug());
        AreEqual("{ }",     store.GetEntitiesWithComponentValue<IndexedInt, int>(42).Debug());

        var query1  = store.Query<Position,    IndexedName>().  HasValue<IndexedName,   string>("find-me1");
        var query2  = store.Query<IndexedName, IndexedInt>().   HasValue<IndexedName,   string>("find-me1");
        var query3  = store.Query().                            HasValue<IndexedName,   string>("find-me1");
        
        AreEqual(2, query1.Entities.Count);     AreEqual("{ 1, 3 }",        query1.Entities.Debug());
        AreEqual(1, query2.Entities.Count);     AreEqual("{ 3 }",           query2.Entities.Debug());
        AreEqual(2, query3.Entities.Count);     AreEqual("{ 1, 3 }",        query3.Entities.Debug());
        
        // Add same value of indexed component again
        entity1.AddComponent(new IndexedName   { name   = "find-me1" });    AreEqual(1, nameValues.Count);
        AreEqual(2, query1.Entities.Count);     AreEqual("{ 1, 3 }",        query1.Entities.Debug());
        AreEqual(1, query2.Entities.Count);     AreEqual("{ 3 }",           query2.Entities.Debug());
        AreEqual(2, query3.Entities.Count);     AreEqual("{ 1, 3 }",        query3.Entities.Debug());
        
        // Update value of indexed component
        entity1.AddComponent(new IndexedName   { name   = "find-me2" });    AreEqual(2, nameValues.Count);
        AreEqual(1, query1.Entities.Count);     AreEqual("{ 3 }",           query1.Entities.Debug());
        AreEqual(1, query2.Entities.Count);     AreEqual("{ 3 }",           query2.Entities.Debug());
        AreEqual(1, query3.Entities.Count);     AreEqual("{ 3 }",           query3.Entities.Debug());

        // --- change queries
        query1  = store.Query<Position,    IndexedName>().  HasValue<IndexedName,   string>("find-me2");
        query2  = store.Query<IndexedName, IndexedInt>().   HasValue<IndexedName,   string>("find-me2");
        query3  = store.Query().                            HasValue<IndexedName,   string>("find-me2");
        
        AreEqual(1, query1.Entities.Count);     AreEqual("{ 1 }",           query1.Entities.Debug());
        AreEqual(0, query2.Entities.Count);     AreEqual("{ }",             query2.Entities.Debug());
        AreEqual(1, query3.Entities.Count);     AreEqual("{ 1 }",           query3.Entities.Debug());
    }
    
    [Test]
    public static void Test_Index_indexed_Entity()
    {
        var store   = new EntityStore();
        var entity1 = store.CreateEntity(1);
        var entity2 = store.CreateEntity(2);
        var entity3 = store.CreateEntity(3);
        
        var target4 = store.CreateEntity(4);
        var target5 = store.CreateEntity(5);
        var target6 = store.CreateEntity(6);
        
        var values = store.GetAllLinkedEntities<LinkComponent>();
        
        entity1.AddComponent(new LinkComponent { entity = target4 });   AreEqual(1, values.Count);
        entity2.AddComponent(new LinkComponent { entity = target5 });   AreEqual(2, values.Count);
        entity3.AddComponent(new LinkComponent { entity = target5 });   AreEqual(2, values.Count);

        int count = 0;
        foreach (var entity in values) {
            switch (count++) {
                case 0: AreEqual(4, entity.Id); break;
                case 1: AreEqual(5, entity.Id); break;
            } 
        }
        AreEqual(2, count);
        
        var query1  = store.Query().HasValue<LinkComponent,   Entity>(target4);
        var query2  = store.Query().HasValue<LinkComponent,   Entity>(target4).
                                    HasValue<LinkComponent,   Entity>(target5);
        
        AreEqual("{ 1 }",       query1.Entities.Debug());
        AreEqual("{ 1, 2, 3 }", query2.Entities.Debug());
        
        AreEqual("{ 1 }",       target4.GetEntityReferences<LinkComponent>().Debug());
        AreEqual("{ 2, 3 }",    target5.GetEntityReferences<LinkComponent>().Debug());
        
        entity2.AddComponent(new LinkComponent { entity = target6 });   AreEqual(3, values.Count);
        AreEqual("{ 3 }",       target5.GetEntityReferences<LinkComponent>().Debug());
        
        entity2.AddComponent(new LinkComponent { entity = target6 });   AreEqual(3, values.Count);
        AreEqual("{ 3 }",       target5.GetEntityReferences<LinkComponent>().Debug());
        
        entity3.RemoveComponent<LinkComponent>();                       AreEqual(2, values.Count);
        AreEqual("{ }",         target5.GetEntityReferences<LinkComponent>().Debug());
    }
    
    [Test]
    public static void Test_Index_EntityReferences()
    {
        var store   = new EntityStore();
        var entity1 = store.CreateEntity(1);
        var entity2 = store.CreateEntity(2);
        var entity3 = store.CreateEntity(3);
        
        var target4 = store.CreateEntity(4);
        var target5 = store.CreateEntity(5);
       
        entity1.AddComponent(new LinkComponent { entity = target4, data = 100 });
        entity2.AddComponent(new LinkComponent { entity = target5, data = 101  });
        entity3.AddComponent(new LinkComponent { entity = target5, data = 102  });

        var refs4    = target4.GetEntityReferences<LinkComponent>();
        AreSame (store,         refs4.Store);
        AreEqual(1,             refs4.Count);
        AreEqual(1,             refs4.Entities.Count);
        AreEqual("Source: 1",   refs4[0].ToString());
        AreEqual(1,             refs4[0].Entity.Id);
        AreEqual(100,           refs4[0].Component.data);
        
        var refs5    = target5.GetEntityReferences<LinkComponent>();
        AreEqual("Source: 2",   refs5[0].ToString());
        AreEqual(2,             refs5[0].Entity.Id);
        AreEqual(101,           refs5[0].Component.data);
        AreEqual("Source: 3",   refs5[1].ToString());
        AreEqual(3,             refs5[1].Entity.Id);
        AreEqual(102,           refs5[1].Component.data);

        int count = 0;
        foreach (var reference in refs5) {
            switch (count++) {
                case 0: AreEqual(101, reference.Component.data);    break;
                case 1: AreEqual(102, reference.Component.data);    break;
            }
        }
        AreEqual(2, count);
    }
    
    [Test]
    public static void Test_Index_support_null()
    {
        var store   = new EntityStore();
        var entity1 = store.CreateEntity();
        var entity2 = store.CreateEntity();
        entity1.AddComponent(new IndexedName { name = null });
        entity2.AddComponent(new IndexedName { name = null });
        
        var start = Mem.GetAllocatedBytes();
        var result = store.GetEntitiesWithComponentValue<IndexedName, string>(null);
        Mem.AssertNoAlloc(start);
        
        AreEqual(2, result.Count);
        AreEqual(1, result[0].Id);
        AreEqual(2, result[1].Id);
        
        entity2.RemoveComponent<IndexedName>();
        result = store.GetEntitiesWithComponentValue<IndexedName, string>(null);
        AreEqual(1, result.Count);
    }
    
    [Test]
    public static void Test_Index_ValueStructIndex()
    {
        var store   = new EntityStore();
        var entity1 = store.CreateEntity();
        entity1.AddComponent(new IndexedInt { value = 123 });
        entity1.AddComponent(new IndexedInt { value = 123 }); // add same component value again
        var result = store.GetEntitiesWithComponentValue<IndexedInt, int>(123);
        AreEqual(1, result.Count);
        
        entity1.AddComponent(new IndexedInt { value = 456 });
        result = store.GetEntitiesWithComponentValue<IndexedInt, int>(456);
        AreEqual(1, result.Count);
        result = store.GetEntitiesWithComponentValue<IndexedInt, int>(123);
        AreEqual(0, result.Count);
    }
    
    [Test]
    public static void Test_Index_exceptions()
    {
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        entity.DeleteEntity();
        var expect = "entity is null. id: 1";
        
        var nre = Throws<NullReferenceException>(() => {
            entity.GetEntityReferences<AttackComponent>();
        });
        AreEqual(expect, nre!.Message);
    }
    
    [Test]
    public static void Test_Index_Perf()
    {
        int count       = 100;
        // 1_000_000  #PC    Test_Index_Allocation - count: 1000000 duration: 176 ms
        var store       = new EntityStore();
        var entities    = new List<Entity>();
        var values      = store.GetAllIndexedComponentValues<IndexedInt, int>();
        for (int n = 1; n <= count; n++) {
            entities.Add(store.CreateEntity());
        }
        for (int n = 0; n < count; n++) {
            entities[n].AddComponent(new IndexedInt { value = n });
        }
        for (int n = 0; n < count; n++) {
            entities[n].AddComponent(new IndexedInt { value = n + count });
        }
        AreEqual(count, values.Count);
        var sw = new Stopwatch();
        sw.Start();
        var start = Mem.GetAllocatedBytes();
        for (int n = 0; n < count; n++) {
            entities[n].AddComponent(new IndexedInt { value = n });
        }
        Mem.AssertNoAlloc(start);
        Console.WriteLine($"Test_Index_Perf - count: {count} duration: {sw.ElapsedMilliseconds} ms");
        AreEqual(count, values.Count);
    }
    
    [Test]
    public static void Test_Index_Perf_Reference()
    {
        int count       = 100;
        // 1_000_000  #PC    Test_Index_Perf_Reference - count: 1000000 duration: 18 ms
        var store       = new EntityStore();
        var entities    = new List<Entity>();
        for (int n = 1; n <= count; n++) {
            entities.Add(store.CreateEntity());
        }
        for (int n = 0; n < count; n++) {
            entities[n].AddComponent(new General.MyComponent { value = n });
        }
        var sw = new Stopwatch();
        sw.Start();
        var start = Mem.GetAllocatedBytes();
        for (int n = 0; n < count; n++) {
            entities[n].AddComponent(new  General.MyComponent { value = n });
        }
        Mem.AssertNoAlloc(start);
        Console.WriteLine($"Test_Index_Perf_Reference - count: {count} duration: {sw.ElapsedMilliseconds} ms");
    }
}

}
