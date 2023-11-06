using System;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;
using static Friflo.Fliox.Engine.ECS.StoreOwnership;
using static Friflo.Fliox.Engine.ECS.TreeMembership;
using static Friflo.Fliox.Engine.ECS.NodeFlags;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable InconsistentNaming
namespace Tests.ECS.GE;

public static class Test_Entity_Tree
{
    [Test]
    public static void Test_CreateEntity_UseRandomPids() {
        var store   = new GameEntityStore();
        store.SetRandomSeed(0);
        var entity  = store.CreateEntity();
        AreEqual(1,             entity.Id);
        AreEqual(1559595546L,   store.Nodes[entity.Id].Pid);
        AreEqual(1,             store.PidToId(1559595546L));
        AreEqual(1,             store.GetNodeByPid(1559595546L).Id);
    }
    
    [Test]
    public static void Test_CreateEntity_UsePidAsId() {
        var store   = new GameEntityStore(PidType.UsePidAsId);
        var entity  = store.CreateEntity();
        AreEqual(1,     entity.Id);
        AreEqual(1,     store.Nodes[entity.Id].Pid);
        AreEqual(1,     store.PidToId(1L));
        AreEqual(1,     store.GetNodeByPid(1L).Pid);
    }
    
    [Test]
    public static void Test_AddChild() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        root.AddComponent(new EntityName("root"));
       
        IsNull  (root.Parent);
        AreEqual(0,         root.ChildNodes.Ids.Length);
        AreEqual(0,         root.ChildCount);
        AreEqual(attached,  root.StoreOwnership);
        foreach (ref var _ in root.ChildNodes) {
            Fail("expect empty child nodes");
        }
        
        // -- add child
        var child = store.CreateEntity(4);
        AreEqual(4,         child.Id);
        child.AddComponent(new EntityName("child"));
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 4", args.ToString()));
        root.AddChild(child);
        
        AreSame (root,      child.Parent);
        AreEqual(attached,  child.StoreOwnership);
        var childNodes =    root.ChildNodes;
        AreEqual(1,         childNodes.Ids.Length);
        AreEqual(4,         childNodes.Ids[0]);
        AreEqual(1,         root.ChildCount);
        AreSame (child,     root.ChildNodes[0]);
        int count = 0;
        foreach (ref var node in root.ChildNodes) {
            count++;
            AreSame(child, node.Entity);
        }
        AreEqual(1,         count);
        
        // -- add same child again
        root.AddChild(child);       // event handler is not called
        AreEqual(1,                                 childNodes.Ids.Length);
        var rootNode = store.Nodes[1];
        AreEqual(1,                                 rootNode.ChildCount);
        AreEqual(1,                                 rootNode.ChildIds.Length);
        AreEqual("id: 1  \"root\"  ChildCount: 1  flags: Created",  rootNode.ToString());
        AreSame (child,                             childNodes[0]);
        
        // --- copy child GameEntity's to array
        var array = new GameEntity[childNodes.Length];
        childNodes.ToArray(array);
        AreSame(child, array[0]);
        
#pragma warning disable CS0618 // Type or member is obsolete
        AreEqual(1,                                 childNodes.Entities_.Length);
#pragma warning restore CS0618 // Type or member is obsolete
    }
    
    [Test]
    public static void Test_InsertChild() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        root.AddComponent(new EntityName("root"));
       
        IsNull  (root.Parent);
        AreEqual(0,         root.ChildNodes.Ids.Length);
        AreEqual(0,         root.ChildCount);
        AreEqual(attached,  root.StoreOwnership);
        foreach (ref var _ in root.ChildNodes) {
            Fail("expect empty child nodes");
        }
        var child4 = store.CreateEntity(4);
        {
            // -- insert new child (id: 4)
            AreEqual(4,         child4.Id);
            SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 4", args.ToString()));
            root.InsertChild(0, child4);
            
            AreSame (root,      child4.Parent);
            AreEqual(attached,  child4.StoreOwnership);
            var childNodes =    root.ChildNodes;
            AreEqual(1,         childNodes.Ids.Length);
            AreEqual(4,         childNodes.Ids[0]);
            AreEqual(1,         root.ChildCount);
            AreSame (child4,    childNodes[0]);
            int count = 0;
            foreach (ref var node in childNodes) {
                count++;
                AreSame(child4, node.Entity);
            }
            AreEqual(1,         count);
            
            // --- insert same child (id: 4) at same index again
            root.InsertChild(0, child4);     // event handler is not called
            AreEqual(1,                                 childNodes.Ids.Length);
            var rootNode = store.Nodes[1];
            AreEqual(1,                                 rootNode.ChildCount);
            AreEqual(1,                                 rootNode.ChildIds.Length);
            AreEqual("id: 1  \"root\"  ChildCount: 1  flags: Created",  rootNode.ToString());
            AreSame (child4,                             childNodes[0]);
        }
        var child5 = store.CreateEntity(5);
        {
            // --- insert new child (id: 5) at index 0
            AreEqual(5,         child5.Id);
            SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 5", args.ToString()));
            root.InsertChild(0, child5);
            
            AreSame (root,      child5.Parent);
            AreEqual(attached,  child5.StoreOwnership);
            var childNodes =    root.ChildNodes;
            AreEqual(2,         childNodes.Ids.Length);
            AreEqual(5,         childNodes.Ids[0]);
            AreEqual(4,         childNodes.Ids[1]);
            AreEqual(2,         root.ChildCount);
            AreSame (child5,    childNodes[0]);
            AreSame (child4,    childNodes[1]);
            var count = 0;
            foreach (ref var node in childNodes) {
                switch(count++) {
                    case 0:     AreSame(child5, node.Entity);   break;
                    case 1:     AreSame(child4, node.Entity);   break;
                    default:    Fail("unexpected");             return;
                }
            }
            AreEqual(2,         count);
        }
        // --- copy child GameEntity's to array
        {
            var childNodes =    root.ChildNodes;
            var array = new GameEntity[childNodes.Length];
            childNodes.ToArray(array);
            AreSame(child5, array[0]);
            AreSame(child4, array[1]);
        }
    }
    
    /// <summary>code coverage for <see cref="GameEntityStore.SetTreeFlags"/></summary>
    [Test]
    public static void Test_AddChild_move_root_tree_entity() {
        var store       = new GameEntityStore();
        var root        = store.CreateEntity(1);
        store.SetStoreRoot(root);
        var child1      = store.CreateEntity(2);
        var child2      = store.CreateEntity(3);
        var subChild    = store.CreateEntity(4);
        
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 2", args.ToString()));
        root.AddChild(child1);
        SetHandler(store, args => AreEqual("entity: 2 - Add ChildNodes[0] = 4", args.ToString()));
        child1.AddChild(subChild);
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[1] = 3", args.ToString()));
        root.AddChild(child2);
        AreEqual(1, child1.ChildCount);
        AreEqual(0, child2.ChildCount);
        
        SetHandlerSeq(store, (args, seq) => {
            switch (seq) {
                case 0:     AreEqual("entity: 2 - Remove ChildNodes[0] = 4",    args.ToString()); return;
                case 1:     AreEqual("entity: 3 - Add ChildNodes[0] = 4",       args.ToString()); return;
                default:    Fail("unexpected"); return;
            }
        });
        child2.AddChild(subChild);  // subChild is moved from child1 to child2
        AreEqual(0, child1.ChildCount);
        AreEqual(1, child2.ChildCount);
    }
    
    [Test]
    public static void Test_RemoveChild() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        var child   = store.CreateEntity(2);
        AreEqual(floating,  root.TreeMembership);
        AreEqual(floating,  child.TreeMembership);
        
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 2",     args.ToString()));
        root.AddChild(child);
        
        store.SetStoreRoot(root);
        IsNull  (root.Parent);
        NotNull (child.Parent);
        AreEqual(treeNode,  root.TreeMembership);
        AreEqual(treeNode,  child.TreeMembership);
        
        // --- remove child
        SetHandler(store, args => AreEqual("entity: 1 - Remove ChildNodes[0] = 2",  args.ToString()));
        root.RemoveChild(child);
        AreEqual(treeNode,  root.TreeMembership);
        AreEqual(floating,  child.TreeMembership);
        AreEqual(0,         root.ChildCount);
        IsNull  (child.Parent);
        
        // --- remove same child again
        root.RemoveChild(child);        // event handler is not called
        
        AreEqual(0,         root.ChildCount);
        IsNull  (child.Parent);
        AreEqual(2,         store.EntityCount);
    }
    
    [Test]
    public static void Test_RemoveChild_from_multiple_children() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        var child2  = store.CreateEntity(2);
        var child3  = store.CreateEntity(3);
        var child4  = store.CreateEntity(4);
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 2",     args.ToString()));
        root.AddChild(child2);
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[1] = 3",     args.ToString()));
        root.AddChild(child3);
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[2] = 4",     args.ToString()));
        root.AddChild(child4);
        
        SetHandler(store, args => AreEqual("entity: 1 - Remove ChildNodes[1] = 3",  args.ToString()));
        root.RemoveChild(child3);
        var childIds = root.ChildIds; 
        AreEqual(2, childIds.Length);
        AreEqual(2, childIds[0]);
        AreEqual(4, childIds[1]);
    }
    
    [Test]
    public static void Test_SetRoot() {
        var store   = new GameEntityStore();
        
        var root    = store.CreateEntity(1);
        IsNull (root.Parent);
        var start = Mem.GetAllocatedBytes();
        
        store.SetStoreRoot(root);
        Mem.AssertNoAlloc(start);
        AreSame(root,           store.StoreRoot);
        IsNull (root.Parent);
        
        var child   = store.CreateEntity(2);
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 2",     args.ToString()));
        root.AddChild(child);
        AreSame(root,           child.Parent);
        AreEqual(treeNode,      child.TreeMembership);
        AreEqual(2,             store.EntityCount);
        var nodes = store.Nodes;
        AreEqual("id: 0",                                   nodes[0].ToString());
        AreEqual("id: 2  []  flags: TreeNode | Created",    nodes[2].ToString());
        
        AreEqual(NullNode,                                  nodes[0].Flags);
        AreEqual(TreeNode | Created,                        nodes[2].Flags);
    }
    
    [Test]
    public static void Test_move_with_AddChild() {
        var store   = new GameEntityStore();
        var entity1 = store.CreateEntity(1);
        var entity2 = store.CreateEntity(2);
        var child   = store.CreateEntity(3);
        entity1.AddComponent(new EntityName("entity-1"));
        entity2.AddComponent(new EntityName("entity-2"));
        child.AddComponent  (new EntityName("child"));
        
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 3", args.ToString()));
        entity1.AddChild(child);
        AreEqual(1,     entity1.ChildCount);
        
        // --- move child from entity1 -> entity2
        SetHandlerSeq(store, (args, seq) => {
            switch (seq) {
                case 0:     AreEqual("entity: 1 - Remove ChildNodes[0] = 3",    args.ToString()); return;
                case 1:     AreEqual("entity: 2 - Add ChildNodes[0] = 3",       args.ToString()); return;
                default:    Fail("unexpected"); return;
            }
        });
        entity2.AddChild(child);
        AreEqual(0,     entity1.ChildCount);
        AreEqual(1,     entity2.ChildCount);
        AreSame(child,  entity2.ChildNodes[0]);
    }
    
    [Test]
    public static void Test_DeleteEntity()
    {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        root.AddComponent(new EntityName("root"));
        store.SetStoreRoot(root);
        var child   = store.CreateEntity(2);
        AreEqual(attached,  child.StoreOwnership);
        child.AddComponent(new EntityName("child"));
        SetHandler(store, args => AreEqual("entity: 1 - Add ChildNodes[0] = 2", args.ToString()));
        root.AddChild(child);
        var subChild= store.CreateEntity(3);
        AreSame (root,      subChild.Store.StoreRoot);
        subChild.AddComponent(new EntityName("subChild"));
        SetHandler(store, args => AreEqual("entity: 2 - Add ChildNodes[0] = 3", args.ToString()));
        child.AddChild(subChild);
        
        AreEqual(3,         store.EntityCount);
        AreSame (root,      child.Parent);
        AreSame (root,      subChild.Store.StoreRoot);
        var childArchetype = child.Archetype;
        AreEqual(3,         childArchetype.EntityCount);
        AreEqual(treeNode,  subChild.TreeMembership);
        NotNull (child.Archetype);
        NotNull (child.Store);
        
        
        var start = Mem.GetAllocatedBytes();
        store.SetChildNodesChangedHandler(null);
        child.DeleteEntity();
        Mem.AssertNoAlloc(start);
        AreEqual(2,         childArchetype.EntityCount);
        AreEqual(2,         store.EntityCount);
        AreEqual(0,         root.ChildCount);
        AreEqual(floating,  subChild.TreeMembership);
        IsNull  (child.Archetype);
        AreEqual("id: 2  (detached)", child.ToString());
        AreSame (subChild,  store.Nodes[3].Entity);
        AreEqual(detached,  child.StoreOwnership);
        IsNull  (child.Archetype);
        IsNull  (child.Store);
        
        var childNode = store.Nodes[2]; // child is detached => all fields have their default value
        IsNull  (           childNode.Entity);
        AreEqual(2,         childNode.Id);
        AreEqual(0,         childNode.Pid);
        AreEqual(0,         childNode.ChildIds.Length);
        AreEqual(0,         childNode.ChildCount);
        AreEqual(0,         childNode.ParentId);
        AreEqual(NullNode,  childNode.Flags);
        
        // From now: access to components and tree nodes throw a NullReferenceException
        Throws<NullReferenceException> (() => {
            _ = child.Name; // access component
        });
        Throws<NullReferenceException> (() => {
            _ = child.Parent; // access tree node
        });
    }
    
    /// <summary>cover <see cref="GameEntityStore.DeleteNode"/></summary>
    [Test]
    public static void Test_Test_Entity_Tree_cover_DeleteNode() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        var child2  = store.CreateEntity(2);
        var child3  = store.CreateEntity(3);
        var child4  = store.CreateEntity(4);
        root.AddChild(child2);
        root.AddChild(child3);
        root.AddChild(child4);
        AreEqual(3, root.ChildCount);
        
        SetHandler(store, args => AreEqual("entity: 1 - Remove ChildNodes[1] = 3", args.ToString()));
        child3.DeleteEntity();
        AreEqual(2, root.ChildCount);
        AreEqual(2, root.ChildNodes[0].Id);
        AreEqual(4, root.ChildNodes[1].Id);
        
        var childIds = root.ChildIds; 
        AreEqual(2, childIds.Length);
        AreEqual(2, childIds[0]);
        AreEqual(4, childIds[1]);
    }
    
    [Test]
    public static void Test_Entity_Tree_ChildEnumerator() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity(1);
        var child   = store.CreateEntity(2);
        root.AddChild(child);
        
        ChildEnumerator enumerator = root.ChildNodes.GetEnumerator();
        while (enumerator.MoveNext()) { }
        enumerator.Reset();
        
        int count = 0;
        while (enumerator.MoveNext()) {
            count++;
        }
        enumerator.Dispose();
        AreEqual(1, count);
    }
    
    [Test]
    public static void Test_Entity_id_argument_exceptions() {
        var store   = new GameEntityStore();
        var e = Throws<ArgumentException>(() => {
            store.CreateEntity(0);
        });
        AreEqual("invalid entity id <= 0. was: 0 (Parameter 'id')", e!.Message);
        
        store.CreateEntity(42);
        e = Throws<ArgumentException>(() => {
            store.CreateEntity(42);
        });
        AreEqual("id already in use in EntityStore. id: 42 (Parameter 'id')", e!.Message);
    }
    
    [Test]
    public static void Test_Entity_SetRoot_assertions() {
        {
            var store   = new GameEntityStore();
            var e       = Throws<ArgumentNullException>(() => {
                store.SetStoreRoot(null);
            });
            AreEqual("Value cannot be null. (Parameter 'entity')", e!.Message);
        } {
            var store1  = new GameEntityStore();
            var store2  = new GameEntityStore();
            var entity  = store1.CreateEntity();
            var e       = Throws<ArgumentException>(() => {
                store2.SetStoreRoot(entity);            
            });
            AreEqual("entity is owned by a different store (Parameter 'entity')", e!.Message);
        } {
            var store   = new GameEntityStore();
            var entity1 = store.CreateEntity();
            var entity2 = store.CreateEntity();
            store.SetStoreRoot(entity1);
            var e = Throws<InvalidOperationException>(() => {
                store.SetStoreRoot(entity2);
            });
            AreEqual("EntityStore already has a StoreRoot. StoreRoot id: 1", e!.Message);
        } {
            var store   = new GameEntityStore();
            var entity1 = store.CreateEntity();
            var entity2 = store.CreateEntity();
            entity1.AddChild(entity2);
            var e = Throws<InvalidOperationException>(() => {
                store.SetStoreRoot(entity2);
            });
            AreEqual("entity must not have a parent to be StoreRoot. current parent id: 1", e!.Message);
        }
    }
    
    [Test]
    public static void Test_Entity_Tree_InvalidStoreException()
    {
        var store1   = new GameEntityStore();
        var store2   = new GameEntityStore();
        
        var entity1 = store1.CreateEntity();
        var entity2 = store2.CreateEntity();

        var e = Throws<ArgumentException>(() => {
            entity1.AddChild(entity2);
        });
        AreEqual("entity is owned by a different store (Parameter 'entity')", e!.Message);
        e = Throws<ArgumentException>(() => {
            entity1.RemoveChild(entity2);
        });
        AreEqual("entity is owned by a different store (Parameter 'entity')", e!.Message);
    }

    /// <summary><see cref="GameEntityStore.GenerateRandomPidForId"/></summary>
    [Test]
    public static void Test_Entity_Tree_RandomPid_Coverage()
    {
        var store   = new GameEntityStore();
        store.SetRandomSeed(1);
        store.CreateEntity();
        store.SetRandomSeed(1); // Random generate same pid. use Next() pid
        store.CreateEntity();
    }
    
    [Test]
    public static void Test_Add_Child_Entities_UseRandomPids_Perf() {
        var store   = new GameEntityStore();
        var root    = store.CreateEntity();
        root.AddComponent(new EntityName("Root"));
        long count  = 10; // 10_000_000L ~ 4.009 ms
        for (long n = 0; n < count; n++) {
            var child = store.CreateEntity();
            root.AddChild(child);
        }
        AreEqual(count, root.ChildCount);
    }
    
    [Test]
    public static void Test_Add_Child_Entities_UsePidAsId_Perf() {
        var store   = new GameEntityStore(PidType.UsePidAsId);
        var root    = store.CreateEntity();
        root.AddComponent(new EntityName("Root"));
        long count  = 10; // 10_000_000L ~ 2.014 ms
        for (long n = 0; n < count; n++) {
            var child = store.CreateEntity();
            root.AddChild(child);
        }
        AreEqual(count, root.ChildCount);
    }
    
    [Test]
    public static void Test_Math_Perf() {
        var rand = new Random();
        var count = 10; // 10_000_000 ~  39 ms
        for (int n = 0; n < count; n++) {
            rand.Next();
        }
    }
    
    private static void SetHandler(GameEntityStore store, Action<ChildNodesChangedArgs> action)
    {
        store.SetChildNodesChangedHandler((object _, in ChildNodesChangedArgs args) => {
            action(args);
        });
    }
    
    private static void SetHandlerSeq(GameEntityStore store, Action<ChildNodesChangedArgs, int> action)
    {
        int seq = 0;
        store.SetChildNodesChangedHandler((object _, in ChildNodesChangedArgs args) => {
            action(args, seq++);
        });
    }
}

