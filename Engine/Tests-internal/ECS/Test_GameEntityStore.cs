using System;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Internal.ECS;

public static class Test_GameEntityStore
{
    [Test]
    public static void Test_GameEntityStore_DeleteEntity()
    {
        var store = new GameEntityStore(PidType.UsePidAsId);
        var entity = store.CreateEntity(10);
        store.nodes[10].parentId = 5;
        
        var e = Throws<InvalidOperationException>(() => {
            entity.DeleteEntity();
        });
        AreEqual("unexpected state: child id not found. parent id: 5, child id: 10", e!.Message);
    }
    
    /// <summary>Cover <see cref="EntityStore.AddArchetype"/></summary>
    [Test]
    public static void Test_Tags_cover_AddArchetype() {
        var store       = new GameEntityStore();
        var archetype   = store.GetArchetype(Signature.Get<Position>());
        
        archetype.SetInternalField(nameof(archetype.archIndex), 5);
        
        var e = Throws<InvalidOperationException>(() => {
            store.AddArchetype(archetype);
        });
        AreEqual("invalid archIndex. expect: 2, was: 5", e!.Message);
    }
}

