using System.Collections.Generic;
using Friflo.Fliox.Engine.ECS;
using NUnit.Framework;
using Tests.Utils;
using static NUnit.Framework.Assert;

// ReSharper disable UseObjectOrCollectionInitializer
// ReSharper disable InconsistentNaming
namespace Tests.ECS.Arch;



public static class Test_Tags
{
    [Test]
    public static void Test_Tags_basics()
    {
        var twoTags = Tags.Get<TestTag, TestTag2>();
        AreEqual("Tags: [#TestTag, #TestTag2]", twoTags.ToString());
        
        var tags    = new Tags();
        AreEqual(0, tags.Count);
        AreEqual("Tags: []",                    tags.ToString());
        IsFalse(tags.Has<TestTag>());
        IsFalse(tags.HasAll(twoTags));
        IsFalse(tags.HasAny(twoTags));
        
        tags.Add<TestTag>();
        AreEqual(1, tags.Count);
        IsTrue (tags.Has<TestTag>());
        IsFalse(tags.HasAll(twoTags));
        IsTrue (tags.HasAny(twoTags));
        
        AreEqual("Tags: [#TestTag]",            tags.ToString());
        
        tags.Add<TestTag2>();
        AreEqual(2, tags.Count);
        AreEqual("Tags: [#TestTag, #TestTag2]", tags.ToString());
        IsTrue (tags.Has<TestTag, TestTag2>());
        IsFalse(tags.Has<TestTag, TestTag2, TestTag3>());
        IsTrue (tags.HasAll(twoTags));
        IsTrue (tags.HasAny(twoTags));

        var copy = new Tags();
        copy.Add(tags);
        AreEqual(2, tags.Count);
        AreEqual("Tags: [#TestTag, #TestTag2]", copy.ToString());
        
        copy.Remove<TestTag>();
        AreEqual(1, copy.Count);
        AreEqual("Tags: [#TestTag2]",           copy.ToString());
        
        copy.Remove(tags);
        AreEqual(0, copy.Count);
        AreEqual("Tags: []",                    copy.ToString());
        
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        
        AreEqual("Tags: []", entity.Tags.ToString());
    }
    
    [Test]
    public static void Test_Tags_constructor()
    {
        var schema  = EntityStore.GetEntitySchema();
        var tagType = schema.TagTypeByType[typeof(TestTag)];
        var tags    = new Tags(tagType);
        int count = 0;
        foreach (var tag in tags) {
            count++;
            AreSame(typeof(TestTag), tag.type);
        }
        AreEqual(1, count);
    }
    
    [Test]
    public static void Test_Tags_generic_IEnumerator()
    {
        IEnumerable<TagType> tags = Tags.Get<TestTag>();
        int count = 0;
        foreach (var _ in tags) {
            count++;
        }
        AreEqual(1, count);
    }
    
    [Test]
    public static void Test_TagsEnumerator()
    {
        var tags = Tags.Get<TestTag>();
        var enumerator = tags.GetEnumerator();
        int count = 0;
        while (enumerator.MoveNext()) {
            count++;
        }
        AreEqual(1, count);
        
        count = 0;
        enumerator.Reset();
        while (enumerator.MoveNext()) {
            count++;
        }
        AreEqual(1, count);
        enumerator.Dispose();
    }
    
    [Test]
    public static void Test_Tags_Get()
    {
        var schema      = EntityStore.GetEntitySchema();
        var testTagType = schema.TagTypeByType[typeof(TestTag)];
        
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
        var sig     = Signature.Get<Position>();

        var query1 = store.Query(sig).AllTags(Tags.Get<TestTag>());
        var query2 = store.Query(sig).AllTags(Tags.Get<TestTag, TestTag2>());
        
        AreEqual("Query: [Position, #TestTag]  EntityCount: 0",             query1.ToString());
        AreEqual("Query: [Position, #TestTag, #TestTag2]  EntityCount: 0",  query2.ToString());
    }
    
    [Test]
    public static void Test_Tags_Add_Remove()
    {
        var store       = new EntityStore();
        AreEqual(1,                                 store.Archetypes.Length);
        var entity      = store.CreateEntity();
        var testTag2    = Tags.Get<TestTag2>();
        
        var eventCount  = 0;
        var handler     = new TagsChangedHandler((in TagsChangedArgs args) => {
            var str = args.ToString();
            switch (eventCount++) {
                case 0:     AreEqual(1,                     args.entityId);
                            AreEqual("Tags: [#TestTag]",    args.tags.ToString());
                            // Ensure entity is in new Archetype
                            AreEqual("[#TestTag]  Count: 1",  store.GetEntityById(args.entityId).Archetype.ToString());
                            AreEqual("entity: 1 - tags change: Tags: [#TestTag]",   str);   return;
                case 1:     AreEqual("entity: 1 - tags change: Tags: [#TestTag2]",  str);   return;
                case 2:     AreEqual("entity: 1 - tags change: Tags: [#TestTag]",   str);   return;
                case 3:     AreEqual("entity: 1 - tags change: Tags: [#TestTag2]",  str);   return;
                case 4:     AreEqual("entity: 1 - tags change: Tags: [#TestTag]",   str);   return;
                default:    Fail("unexpected event");                                       return;
            }
        });
        store.TagsChanged += handler;
        
        entity.AddTag<TestTag>();
        AreEqual("[#TestTag]  Count: 1",            entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(2,                                 store.Archetypes.Length);
        
        // add same tag again
        entity.AddTag<TestTag>(); // no event sent
        AreEqual("[#TestTag]  Count: 1",            entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(2,                                 store.Archetypes.Length);
        
        entity.AddTags(testTag2);
        AreEqual("[#TestTag, #TestTag2]  Count: 1",  entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(3,                                 store.Archetypes.Length);
        
        entity.RemoveTag<TestTag>();
        AreEqual("[#TestTag2]  Count: 1",           entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(4,                                 store.Archetypes.Length);
        
        entity.RemoveTags(testTag2);
        AreEqual("[]",                              entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(4,                                 store.Archetypes.Length);
        
        // remove same tag again
        entity.RemoveTags(testTag2); // no event sent
        AreEqual("[]",                              entity.Archetype.ToString());
        AreEqual(1,                                 store.EntityCount);
        AreEqual(4,                                 store.Archetypes.Length);
        
        store.TagsChanged -= handler;
        
        // Execute previous operations again. All required archetypes are now present
        const int count = 10; // 10_000_000 ~ 1.349 ms
        var start = Mem.GetAllocatedBytes();
        // each tags mutation causes a structural change
        for (int n = 0; n < count; n++) {
            entity.AddTag       <TestTag>();
            entity.AddTags      (testTag2);
            entity.RemoveTag    <TestTag>();
            entity.RemoveTags   (testTag2);
        }
        Mem.AssertNoAlloc(start);
        
        AreEqual(1,                                 store.EntityCount);
        AreEqual(4,                                 store.Archetypes.Length);
        AreEqual(4, eventCount); // last assertion ensuring no events sent in perf test
    }
    
    /// <summary>Cover <see cref="EntityStoreBase.GetArchetypeWithTags"/></summary>
    [Test]
    public static void Test_Tags_cover_GetArchetypeWithTags() {
        var store   = new EntityStore();
        var entity  = store.CreateEntity();
        entity.AddComponent<Position>();
        entity.AddTag<TestTag>();
        
        var archetype = store.GetArchetype(Signature.Get<Position>(), Tags.Get<TestTag>());
        AreEqual(1, archetype.EntityCount);
    }
    
    [Test]
    public static void Test_Tags_Query() {
        var store           = new EntityStore();
        var archTestTag     = store.GetArchetype(Tags.Get<TestTag>());
        var archTestTagAll  = store.GetArchetype(Tags.Get<TestTag, TestTag2>());
        AreEqual(3,                             store.Archetypes.Length);
        
        var entity1     = store.CreateEntity(1);
        var entity2     = store.CreateEntity(2);
        entity1.AddTag<TestTag>();
        entity2.AddTag<TestTag>();
        entity2.AddTag<TestTag2>();
        AreEqual("[#TestTag]  Count: 1",            entity1.Archetype.ToString());
        AreEqual("[#TestTag, #TestTag2]  Count: 1", entity2.Archetype.ToString());
        AreEqual(2,                                 store.EntityCount);
        AreEqual(3,                                 store.Archetypes.Length);
        AreEqual(1,                                 archTestTag.EntityCount);
        AreEqual(1,                                 archTestTagAll.EntityCount);
        {
            var query  = store.Query().AllTags(Tags.Get<TestTag>());
            AreEqual("Query: [#TestTag]  EntityCount: 2", query.ToString());
            int count   = 0;
            foreach (var id in query) {
                switch (count) {
                    case 0: AreEqual(1, id); break;
                    case 1: AreEqual(2, id); break;
                }
                count++;
            }
            AreEqual(2, count);
        } {
            var query  = store.Query().AllTags(Tags.Get<TestTag2>());
            AreEqual("Query: [#TestTag2]  EntityCount: 1", query.ToString());
            int count   = 0;
            foreach (var id in query) {
                count++;
                AreEqual(2, id);
            }
            AreEqual(1, count);
        } { 
            var query = store.Query().AllTags(Tags.Get<TestTag, TestTag2>());
            AreEqual("Query: [#TestTag, #TestTag2]  EntityCount: 1", query.ToString());
            int count   = 0;
            foreach (var id in query) {
                count++;
                AreEqual(2, id);
            }
            AreEqual(1, count);
        }
    }
}

