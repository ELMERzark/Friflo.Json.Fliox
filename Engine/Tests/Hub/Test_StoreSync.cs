﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Friflo.Fliox.Engine.ECS;
using Friflo.Fliox.Engine.ECS.Serialize;
using Friflo.Fliox.Engine.Hub;
using Friflo.Json.Fliox;
using Friflo.Json.Fliox.Hub.Client;
using Friflo.Json.Fliox.Hub.Host;
using Friflo.Json.Fliox.Hub.Host.Event;
using NUnit.Framework;
using Tests.ECS;
using Tests.ECS.Serialize;
using static NUnit.Framework.Assert;

// ReSharper disable MethodHasAsyncOverload
// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.Hub;

public static class Test_StoreSync
{
    private static StoreClient CreateClient() {
        var database    = new MemoryDatabase("test");
        var hub         = new FlioxHub(database);
        return new StoreClient(hub);
    }
    
    [Test]
    public static async Task Test_DataSync_load_entities()
    {
        var client  = CreateClient();
        var rootNode    = new DataEntity { pid = 10L, components = Test_ComponentReader.RootComponents, children = new List<long> { 11 } };
        var childNode   = new DataEntity { pid = 11L, components = Test_ComponentReader.ChildComponents };
        
        client.entities.Upsert(rootNode);
        client.entities.Upsert(childNode);
        client.SyncTasksSynchronous();
        
        var store   = new EntityStore(PidType.UsePidAsId);
        var sync    = new StoreSync(store, client);
        AreSame(store, sync.Store); // ensure API available
        
        // load entities via client sync
        for (int n = 0; n < 2; n++) {
            var errors      = sync.LoadEntities();
            AreEqual(0, errors.Count);
            
            var root        = store.GetEntityById(10);
            var child       = store.GetEntityById(11);
            Test_ComponentReader.AssertRootEntity(root, 2);
            Test_ComponentReader.AssertChildEntity(child);
            var type = store.GetArchetype(Signature.Get<Position, Scale3>());
            AreEqual(2,     type.EntityCount);
            AreEqual(2,     store.EntityCount);
        }
        
        // clear entities in store
        store.GetEntityById(10).DeleteEntity();
        store.GetEntityById(11).DeleteEntity();
        AreEqual(0,     store.EntityCount);
        
        // load entities via client async
        for (int n = 0; n < 2; n++) {
            var errors      = await sync.LoadEntitiesAsync();
            AreEqual(0, errors.Count);
            
            var root        = store.GetEntityById(10);
            var child       = store.GetEntityById(11);
            Test_ComponentReader.AssertRootEntity(root, 2);
            Test_ComponentReader.AssertChildEntity(child);
            var type = store.GetArchetype(Signature.Get<Position, Scale3>());
            AreEqual(2,     type.EntityCount);
            AreEqual(2,     store.EntityCount);
        }
    }
    
    [Test]
    public static void Test_DataSync_load_entities_error()
    {
        var client      = CreateClient();
        var components  = new JsonValue("{ \"pos\": { \"x\": true }}");
        var rootNode    = new DataEntity { pid = 10L, components = components, children = new List<long> { 11 } };
        
        client.entities.Upsert(rootNode);
        client.SyncTasksSynchronous();
        
        var store   = new EntityStore(PidType.UsePidAsId);
        var sync    = new StoreSync(store, client);
        AreSame(store, sync.Store); // ensure API available
        
        // load entities via client sync
        for (int n = 0; n < 2; n++) {
            var errors      = sync.LoadEntities();
            AreEqual(1, errors.Count);
            AreEqual("entity: 10 - 'components[pos]' - Cannot assign bool to float. got: true path: 'x' at position: 9", errors[0]);
            
            var root = store.GetEntityById(10);
            IsTrue(         root.HasPosition);
            var type = store.GetArchetype(Signature.Get<Position>());
            AreEqual(1,     type.EntityCount);
            AreEqual(1,     store.EntityCount);
        }
    }
    
    [Test]
    public static async Task Test_DataSync_store_entities()
    {
        var client      = CreateClient();
        var store       = new EntityStore(PidType.UsePidAsId);
        var sync        = new StoreSync(store, client);

        var entity  = store.CreateEntity(10);
        entity.AddComponent(new Position { x = 1, y = 2, z = 3 });
        entity.AddScript(new TestScript1 { val1 = 10 });
        entity.AddTag<TestTag>();
        
        var child   = store.CreateEntity(11);
        store.ChildEntitiesChanged = (object _, in ChildEntitiesChangedArgs args) => {
            AreEqual("entity: 10 - Add ChildIds[0] = 11", args.ToString());
        };
        entity.AddChild(child);
        AreEqual(2, store.EntityCount);
        
        // --- store entities via client sync
        for (int n = 0; n < 2; n++)
        {
            sync.StoreEntities();

            var data10 = client.entities.Local[10];
            var data11 = client.entities.Local[11];
            
            AreEqual(10,    data10.pid);
            AreEqual(1,     data10.children.Count);
            AreEqual(11,    data10.children[0]);
            AreEqual("{\n        \"pos\": {\"x\":1,\"y\":2,\"z\":3},\n        \"script1\": {\"val1\":10}\n    }", data10.components.AsString());
            
            AreEqual(11,    data11.pid);
            IsNull  (data11.children);
            IsTrue  (data11.components.IsNull());
        }
        // --- store entities via client async
        sync.ClearData();
        AreEqual(0, client.entities.Local.Count);
        for (int n = 0; n < 2; n++)
        {
            await sync.StoreEntitiesAsync();

            var data10 = client.entities.Local[10];
            var data11 = client.entities.Local[11];
            
            AreEqual(10,    data10.pid);
            AreEqual(1,     data10.children.Count);
            AreEqual(11,    data10.children[0]);
            AreEqual("{\n        \"pos\": {\"x\":1,\"y\":2,\"z\":3},\n        \"script1\": {\"val1\":10}\n    }", data10.components.AsString());
            
            AreEqual(11,    data11.pid);
            IsNull  (data11.children);
            IsTrue  (data11.components.IsNull());
        }
    }
    
    [Test]
    public static void Test_DataSync_constructor_params()
    {
        var e = Throws<ArgumentNullException>(() => {
            _ = new StoreSync(null, null);
        });
        AreEqual("Value cannot be null. (Parameter 'store')", e!.Message);
        
        var store = new EntityStore();
        e = Throws<ArgumentNullException>(() => {
            _ = new StoreSync(store, null);
        });
        AreEqual("Value cannot be null. (Parameter 'client')", e!.Message);
    }
    
    private static FlioxHub Prepare_SubscribeDatabaseChanges(out StoreSync sync, out EventProcessorQueue processor)
    {
        var schema          = DatabaseSchema.Create<StoreClient>();
        var database        = new MemoryDatabase("test", schema);
        var hub             = new FlioxHub(database);
        hub.UsePubSub();    // need currently called before SetupSubscriptions()
        hub.EventDispatcher = new EventDispatcher(EventDispatching.Send);
        var client          = new StoreClient(hub);
        var store           = new EntityStore(PidType.UsePidAsId);
        sync                = new StoreSync(store, client);
        processor           = new EventProcessorQueue();
        client.SetEventProcessor(processor);
        return hub;
    }
    
    /// <summary>Cover <see cref="StoreSync.SubscribeDatabaseChanges"/></summary>
    [Test]
    public static void Test_DataSync_SubscribeDatabaseChanges()
    {
        var hub     = Prepare_SubscribeDatabaseChanges(out var sync, out var processor);
        var client  = new StoreClient(hub);
        var store   = sync.Store;
        sync.SubscribeDatabaseChanges();
        
        var rootNode    = new DataEntity { pid = 10L, components = Test_ComponentReader.RootComponents, children = new List<long> { 11 } };
        var childNode   = new DataEntity { pid = 11L, components = Test_ComponentReader.ChildComponents };
        
        client.entities.Upsert(rootNode);
        client.entities.Upsert(childNode);
        client.SyncTasksSynchronous();
        processor.ProcessEvents();
        
        AreEqual(2, store.EntityCount);
        var root        = store.GetEntityById(10);
        var child       = store.GetEntityById(11);
        Test_ComponentReader.AssertRootEntity(root, 2);
        Test_ComponentReader.AssertChildEntity(child);
        
        client.entities.Delete(10L);
        client.entities.Delete(11L);
        client.SyncTasksSynchronous();
        processor.ProcessEvents();
        AreEqual(0, store.EntityCount);
    }
    
    /// <summary>Cover <see cref="StoreSync.SubscribeDatabaseChangesAsync"/></summary>
    [Test]
    public static async Task Test_DataSync_SubscribeDatabaseChangesAsync()
    {
        var hub     = Prepare_SubscribeDatabaseChanges(out var sync, out var processor);
        var client  = new StoreClient(hub);
        var store   = sync.Store;
        await sync.SubscribeDatabaseChangesAsync();
        
        var rootNode    = new DataEntity { pid = 10L, components = Test_ComponentReader.RootComponents, children = new List<long> { 11 } };
        var childNode   = new DataEntity { pid = 11L, components = Test_ComponentReader.ChildComponents };
        
        client.entities.Upsert(rootNode);
        client.entities.Upsert(childNode);
        await client.SyncTasks();
        processor.ProcessEvents();
        
        AreEqual(2, store.EntityCount);
        var root        = store.GetEntityById(10);
        var child       = store.GetEntityById(11);
        Test_ComponentReader.AssertRootEntity(root, 2);
        Test_ComponentReader.AssertChildEntity(child);
        
        client.entities.Delete(10L);
        client.entities.Delete(11L);
        await client.SyncTasks();
        processor.ProcessEvents();
        AreEqual(0, store.EntityCount);
    }
}