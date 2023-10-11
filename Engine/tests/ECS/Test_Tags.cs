using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Tests.ECS;

internal struct TestTag  : IEntityTag { }

internal struct TestTag2 : IEntityTag { }

public static class Test_Tags
{
    [Test]
    public static void Test_Tags_basics()
    {
        var twoTags = Tags.Get<TestTag, TestTag2>();
        AreEqual("Tags: [#TestTag, #TestTag2]", twoTags.ToString());
        
        var tags    = new Tags();
        AreEqual("Tags: []",                    tags.ToString());
        IsFalse(tags.Has<TestTag>());
        IsFalse(tags.HasAll(twoTags));
        IsFalse(tags.HasAny(twoTags));
        
        tags.Add<TestTag>();
        IsTrue (tags.Has<TestTag>());
        IsFalse(tags.HasAll(twoTags));
        IsTrue (tags.HasAny(twoTags));
        
        AreEqual("Tags: [#TestTag]",            tags.ToString());
        
        tags.Add<TestTag2>();
        AreEqual("Tags: [#TestTag, #TestTag2]", tags.ToString());
        IsTrue (tags.Has<TestTag, TestTag2>());
        IsTrue (tags.HasAll(twoTags));
        IsTrue (tags.HasAny(twoTags));

        var copy = new Tags();
        copy.Add(tags);
        AreEqual("Tags: [#TestTag, #TestTag2]", copy.ToString());
        
        copy.Remove<TestTag>();
        AreEqual("Tags: [#TestTag2]",           copy.ToString());
        
        copy.Remove(tags);
        AreEqual("Tags: []",                    copy.ToString());
        
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        
        AreEqual("Tags: []", entity.Tags.ToString());
    }
    
    [Test]
    public static void Test_Tags_Get()
    {
        var schema          = EntityStore.GetComponentSchema();
        var testTagType     = schema.TagTypeByType[typeof(TestTag)];
        var testTagType2    = schema.TagTypeByType[typeof(TestTag2)];
        
        var tag1    = Tags.Get<TestTag>();
        AreEqual("Tags: [#TestTag]", tag1.ToString());
        int count1 = 0;
        foreach (var tagType in tag1) {
            AreSame(testTagType, tagType);
            count1++;
        }
        AreEqual(1, count1);
        
        var count2 = 0;
        var tag2 = Tags.Get<TestTag, TestTag2>();
        AreEqual("Tags: [#TestTag, #TestTag2]", tag2.ToString());
        foreach (var _ in tag2) {
            count2++;
        }
        AreEqual(2, count2);
        
        AreEqual(tag2, Tags.Get<TestTag2, TestTag>());
    }
    
    [Test]
    public static void Test_Tags_Get_Mem()
    {
        var tag1    = Tags.Get<TestTag>();
        foreach (var _ in tag1) { }
        
        // --- 1 tag
        var start   = Mem.GetAllocatedBytes();
        int count1 = 0;
        foreach (var _ in tag1) {
            count1++;
        }
        Mem.AssertNoAlloc(start);
        AreEqual(1, count1);
        
        // --- 2 tags
        start       = Mem.GetAllocatedBytes();
        var tag2    = Tags.Get<TestTag, TestTag2>();
        var count2 = 0;
        foreach (var _ in tag2) {
            count2++;
        }
        Mem.AssertNoAlloc(start);
        AreEqual(2, count2);
    }
    
    [Test]
    public static void Test_tagged_Query() {
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        
        var sig1 = Signature.Get<Position>();
        var sig2 = Signature.Get<Position, Rotation>();
        var sig3 = Signature.Get<Position, Rotation, Scale3>();
        var sig4 = Signature.Get<Position, Rotation, Scale3, MyComponent1>();
        var sig5 = Signature.Get<Position, Rotation, Scale3, MyComponent1, MyComponent2>();
        //

        var query2 =    store.Query(sig2, Tags.Get<TestTag, TestTag2>());
    }
    
    [Test]
    public static void Test_Tags_Add_Remove() {
        var store       = new EntityStore();
        var entity      = store.CreateEntity();
        var testTag2    = Tags.Get<TestTag2>();
        
        entity.AddTag<TestTag>();
        entity.AddTags(testTag2);
        
        entity.RemoveTag<TestTag>();
        entity.RemoveTags(testTag2);
    }
}

