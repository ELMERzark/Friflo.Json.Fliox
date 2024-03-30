using System;
using Friflo.Engine.ECS;
using NUnit.Framework;
using static NUnit.Framework.Assert;

// ReSharper disable RedundantTypeDeclarationBody
// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Arch {


public static class Test_EntityGenericSet
{
    [Test]
    public static void Test_Entity_generic_Set()
    {
        var store   = new EntityStore();

        int componentAddedCount = 0;
        Action<ComponentChanged> componentAdded = changed => {
            var str = changed.ToString();
            switch (componentAddedCount++)
            {                   
                // --- entity 1
                case 0:     AreEqual(new Position(2,2,2),       changed.Component<Position>());     
                            AreEqual(new Position(1,1,1),       changed.OldComponent<Position>());
                            AreEqual("entity: 1 - event > Update Component: [Position]",        str);   break;
                
                // --- entity 2
                case 1:     AreEqual(new Position(2,2,2),       changed.Component<Position>());     
                            AreEqual("entity: 2 - event > Update Component: [Position]",        str);   break;
                case 2:     AreEqual(new Scale3(2,2,2),         changed.Component<Scale3>());     
                            AreEqual("entity: 2 - event > Update Component: [Scale3]",          str);   break;
                
                // --- entity 3
                case 3:     AreEqual(new EntityName("new"),     changed.Component<EntityName>());     
                            AreEqual("entity: 3 - event > Update Component: [EntityName]",      str);   break;
                case 4:     AreEqual(new Position(2,2,2),       changed.Component<Position>());     
                            AreEqual("entity: 3 - event > Update Component: [Position]",        str);   break;
                case 5:     AreEqual(new Scale3(2,2,2),         changed.Component<Scale3>());     
                            AreEqual("entity: 3 - event > Update Component: [Scale3]",          str);   break;
                
                // --- entity 4
                case 6:     AreEqual(new EntityName("new"),     changed.Component<EntityName>());     
                            AreEqual("entity: 4 - event > Update Component: [EntityName]",      str);   break;
                case 7:     AreEqual(new Position(2,2,2),       changed.Component<Position>());     
                            AreEqual("entity: 4 - event > Update Component: [Position]",        str);   break;
                case 8:     AreEqual(new Scale3(2,2,2),         changed.Component<Scale3>());     
                            AreEqual("entity: 4 - event > Update Component: [Scale3]",          str);   break;
                case 9:     AreEqual(new MyComponent1{ a = 2 }, changed.Component<MyComponent1>());     
                            AreEqual("entity: 4 - event > Update Component: [MyComponent1]",    str);   break;
                
                // --- entity 5
                case 10:    AreEqual(new EntityName("new"),     changed.Component<EntityName>());     
                            AreEqual("entity: 5 - event > Update Component: [EntityName]",      str);   break;
                case 11:    AreEqual(new Position(2,2,2),       changed.Component<Position>());     
                            AreEqual("entity: 5 - event > Update Component: [Position]",        str);   break;
                case 12:    AreEqual(new Scale3(2,2,2),         changed.Component<Scale3>());     
                            AreEqual("entity: 5 - event > Update Component: [Scale3]",          str);   break;
                case 13:    AreEqual(new MyComponent1{ a = 2 }, changed.Component<MyComponent1>());     
                            AreEqual("entity: 5 - event > Update Component: [MyComponent1]",    str);   break;
                case 14:    AreEqual(new MyComponent2 { b = 2}, changed.Component<MyComponent2>());     
                            AreEqual("entity: 5 - event > Update Component: [MyComponent2]",    str);   break;
            }
        };
        var tag         = Tags.Get<TestTag>();
        var entity1     = store.CreateEntity(1);
        var entity2     = store.CreateEntity(2);
        var entity3     = store.CreateEntity(3);
        var entity4     = store.CreateEntity(4);
        var entity5     = store.CreateEntity(5);
        
        entity1.Add(new Position(1,1,1), tag);
        entity2.Add(new Position(1,1,1), new Scale3(1,1,1), tag);
        entity3.Add(new Position(1,1,1), new Scale3(1,1,1), new EntityName("old"), tag);
        entity4.Add(new Position(1,1,1), new Scale3(1,1,1), new EntityName("old"), new MyComponent1 { a = 1 }, tag);
        entity5.Add(new Position(1,1,1), new Scale3(1,1,1), new EntityName("old"), new MyComponent1 { a = 1 }, new MyComponent2 { b = 1 }, tag);
        
        store.OnComponentAdded  += componentAdded;
        
        for (int n = 0; n < 2; n++)
        {
            entity1.Set(new Position(2,2,2));
            entity2.Set(new Position(2,2,2), new Scale3(2,2,2));
            entity3.Set(new Position(2,2,2), new Scale3(2,2,2), new EntityName("new"));
            entity4.Set(new Position(2,2,2), new Scale3(2,2,2), new EntityName("new"), new MyComponent1 { a = 2 });
            entity5.Set(new Position(2,2,2), new Scale3(2,2,2), new EntityName("new"), new MyComponent1 { a = 2 }, new MyComponent2 { b = 2 });
            
            store.OnComponentAdded  -= componentAdded;
        }
        AreEqual(15, componentAddedCount);
    }
}

}