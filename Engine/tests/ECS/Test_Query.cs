using System;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS;

public static class Test_Query
{
    [Test]
    public static void Test_Create_Query()
    {
        IsTrue(true);
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        var query1  = store.Query<Position>();
        var query2  = store.Query<Position, Rotation>();
        var query3  = store.Query<Position, Rotation, Scale3>();
        var query4  = store.Query<Position, Rotation, Scale3, EntityName>();
        
        AreEqual(0, query1.Archetypes.Length);
        AreEqual(0, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        
        entity.AddComponent<Position>();
        AreEqual(1, query1.Archetypes.Length);
        AreEqual(0, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        
        entity.AddComponent<Rotation>();
        AreEqual(2, query1.Archetypes.Length);
        AreEqual(1, query2.Archetypes.Length);
        AreEqual(0, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        
        entity.AddComponent<Scale3>();
        AreEqual(3, query1.Archetypes.Length);
        AreEqual(2, query2.Archetypes.Length);
        AreEqual(1, query3.Archetypes.Length);
        AreEqual(0, query4.Archetypes.Length);
        
        entity.AddComponent<EntityName>();
        AreEqual(4, query1.Archetypes.Length);
        AreEqual(3, query2.Archetypes.Length);
        AreEqual(2, query3.Archetypes.Length);
        AreEqual(1, query4.Archetypes.Length);
    }

    [Test]
    public static void Test_ArchetypeMask()
    {
        {
            var mask = new ArchetypeMask(Array.Empty<int>());
            AreEqual("<0, 0, 0, 0>", mask.ToString());
        } {
            var mask = new ArchetypeMask(new [] { 0 });
            AreEqual("<1, 0, 0, 0>", mask.ToString());
        } {
            var mask = new ArchetypeMask(new [] { 0, 64, 128, 192 });
            AreEqual("<1, 1, 1, 1>", mask.ToString());
        }  {
            var mask = new ArchetypeMask(new [] { 63, 127, 191, 255 });
            AreEqual("<-9223372036854775808, -9223372036854775808, -9223372036854775808, -9223372036854775808>", mask.ToString());
        }
    }
}

