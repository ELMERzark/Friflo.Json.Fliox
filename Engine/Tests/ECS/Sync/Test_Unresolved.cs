using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using Friflo.Fliox.Engine.ECS.Sync;
using Friflo.Json.Fliox;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable InconsistentNaming
namespace Tests.ECS.Sync;

public static class Test_Unresolved
{
    private static JsonValue UnresolvedComponents => new JsonValue("{ \"xxx\": { \"foo\":1 }}");
    
    [Test]
    public static void Test_Unresolved_components()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var sourceEntity = new DataEntity { pid = 1, components = UnresolvedComponents };
        
        for (int n = 0; n < 2; n++)
        {
            var gameEntity  = converter.DataToGameEntity(sourceEntity, store, out _);
            var unresolved  = gameEntity.GetComponent<Unresolved>();
            
            AreEqual(1,                 unresolved.components.Count);
            AreEqual("{ \"foo\":1 }",   unresolved.components["xxx"].ToString());
            
            AreEqual("unresolved components: 'xxx'", unresolved.ToString());
            
            var targetEntity = converter.GameToDataEntity(gameEntity);
            AreEqual("{\"xxx\":{ \"foo\":1 }}", targetEntity.components.ToString());
        }
    }
    
    [Test]
    public static void Test_Unresolved_tags()
    {
        var store       = new GameEntityStore(PidType.UsePidAsId);
        var converter   = EntityConverter.Default;
        
        var sourceEntity = new DataEntity { pid = 1, tags = new List<string>{"yyy"} };
        
        for (int n = 0; n < 2; n++)
        {
            var gameEntity  = converter.DataToGameEntity(sourceEntity, store, out _);
            var unresolved  = gameEntity.GetComponent<Unresolved>();
            
            AreEqual(1, unresolved.tags.Count);
            IsTrue  (unresolved.tags.Contains("yyy"));
            
            AreEqual("unresolved tags: 'yyy'", unresolved.ToString());
            
            var targetEntity = converter.GameToDataEntity(gameEntity);
            
            AreEqual(1, targetEntity.tags.Count);
            IsTrue  (targetEntity.tags.Contains("yyy"));
            IsTrue  (targetEntity.components.IsNull()); // Unresolved component is not serialized.
        }
    }
}

