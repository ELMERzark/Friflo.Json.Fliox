using System.Collections.Generic;
using Friflo.Fliox.Engine.Client;
using Friflo.Fliox.Engine.ECS;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS;

public static class Test_ComponentReader
{
    [Test]
    public static void Test_ReadComponents() {
        var store       = new EntityStore(100, PidType.UsePidAsId);
        store.RegisterStructComponent<Position>();
        
        var rootNode    = new DataNode {
            pid         = 10,
            components  = new JsonValue("{ \"pos\": { \"x\": 1, \"y\": 2, \"z\": 3 } }"),
            children    = new List<int> { 11 } 
        };
        var childNode = new DataNode {
            pid         = 11,
        };
        var root    = store.CreateFromDataNode(rootNode);
        var child   = store.CreateFromDataNode(childNode);
        
        AreEqual(1,     root.ChildCount);
        AreEqual(11,    root.ChildNodes.Ids[0]);
        AreEqual(1,     root.ComponentCount);
        AreEqual(1f,    root.Position.x);
        AreEqual(2f,    root.Position.y);
        AreEqual(3f,    root.Position.z);
        var posType = store.GetArchetype<Position>();
        AreEqual(1,     posType.EntityCount);

        AreEqual(11,    child.Id);
    }
   
}
