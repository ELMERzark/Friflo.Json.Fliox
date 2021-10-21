﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System.Collections;
using System.Threading.Tasks;
using Friflo.Json.Fliox.DB.Client;
using Friflo.Json.Fliox.DB.Host;
using Friflo.Json.Fliox.DB.Host.Event;
using Friflo.Json.Fliox.DB.Threading;
using Friflo.Json.Fliox.DB.Protocol;
using Friflo.Json.Fliox.DB.Protocol.Tasks;
using Friflo.Json.Fliox.Mapper;
using Friflo.Json.Fliox.Transform;
using Friflo.Json.Tests.Common.Utils;
using NUnit.Framework;
using UnityEngine.TestTools;
using static NUnit.Framework.Assert;
using static Friflo.Json.Tests.Common.Utils.AssertUtils;

namespace Friflo.Json.Tests.Common.UnitTest.Fliox.Client.Happy
{
    internal enum EventAssertion {
        /// <summary>Assert a <see cref="SubscriptionProcessor"/> will not get change events from the
        /// <see cref="FlioxClient"/> it is attached to.</summary>.
        NoChanges,
        /// <summary>Assert a <see cref="SubscriptionProcessor"/> will get change events from all
        /// <see cref="FlioxClient"/>'s it is not attached to.</summary>.
        Changes
    }
    
    public partial class TestStore
    {
        [UnityTest] public IEnumerator  SubscribeCoroutine() { yield return RunAsync.Await(AssertSubscribe()); }
        [Test]      public void         SubscribeSync() { SingleThreadSynchronizationContext.Run(AssertSubscribe); }
        
        private static async Task AssertSubscribe() {
            using (var _            = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var fileDatabase = new FileDatabase(CommonUtils.GetBasePath() + "assets~/DB/PocStore"))
            using (var hub          = new DatabaseHub(fileDatabase))
            using (var listenDb     = new PocStore(hub, "listenDb", "listen-client")) {
                hub.EventBroker = eventBroker;
                var listenProcessor   = await CreateSubscriptionProcessor(listenDb, EventAssertion.Changes);
                using (var createStore  = new PocStore(hub, "createStore", "create-client")) {
                    var createProcessor = await CreateSubscriptionProcessor(createStore, EventAssertion.NoChanges);
                    await TestRelationPoC.CreateStore(createStore);
                    
                    while (!listenProcessor.receivedAll ) { await Task.Delay(1); }

                    AreEqual(1, createProcessor.EventSequence);  // received no change events for changes done by itself
                }
                listenProcessor.AssertCreateStoreChanges();
                AreEqual(9, listenProcessor.EventSequence);           // non protected access
                await eventBroker.FinishQueues();
            }
        }
        
        private static async Task<PocSubscriptionProcessor> CreateSubscriptionProcessor (PocStore store, EventAssertion eventAssertion) {
            var processor = new PocSubscriptionProcessor(store, eventAssertion);
            store.SetSubscriptionProcessor(processor);
            store.SetSubscriptionHandler((handler, ev) => {
                processor.subscribeEventsCalls++;
                var eventInfo = ev.GetEventInfo();
                switch (handler.EventSequence) {
                    case 3:
                        AreEqual(6, eventInfo.Count);
                        AreEqual(6, eventInfo.changes.Count);
                        AreEqual("(creates: 2, upserts: 4, deletes: 0, patches: 0, messages: 0)", eventInfo.ToString());
                        var articleChanges  = handler.GetEntityChanges(store.articles,  ev);
                        var producerChanges = handler.GetEntityChanges(store.producers, ev);
                        AreEqual(1, articleChanges.creates.Count);
                        AreEqual("(creates: 1, upserts: 4, deletes: 0, patches: 0)", articleChanges.ToString());
                        AreEqual("(creates: 1, upserts: 0, deletes: 0, patches: 0)", producerChanges.ToString());
                        break;
                    case 9:
                        AreEqual(6, eventInfo.Count);
                        AreEqual(5, eventInfo.messages);
                        AreEqual(1, eventInfo.changes.upserts);
                        var messages = handler.GetMessages(ev);
                        AreEqual(5, messages.Count);
                        break;
                }
            });
            
            var subscriptions   = store.SubscribeAllChanges(Changes.All);
            // change subscription of specific EntitySet<Article>
            var articlesSub     = store.articles.SubscribeChanges(Changes.All);
            
            var subscribeMessage    = store.SubscribeMessage(TestRelationPoC.EndCreate, (msg) => {
                processor.receivedAll = true;
                AreEqual("null",            msg.Json.AsString());
            });
            var subscribeMessage1   = store.SubscribeMessage<TestMessage>((msg) => {
                processor.testMessageCalls++;
                TestMessage value = msg.Value;
                AreEqual("test message",        value.text);
                AreEqual(nameof(TestMessage),   msg.Name);
            });
            var subscribeMessage2   = store.SubscribeMessage<int>(TestRelationPoC.TestMessageInt, (msg) => {
                processor.testMessageIntCalls++;
                AreEqual(42,                            msg.Value);
                AreEqual("42",                          msg.Json.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.TryReadJson(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.TryReadJson<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", error.Message);
                
                var e = Throws<JsonReaderException> (() => msg.ReadJson<string>());
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", e.Message);
            });
            var subscribeMessage3   = store.SubscribeMessage(TestRelationPoC.TestMessageInt, (msg) => {
                processor.testMessageIntCalls++;
                var val = msg.ReadJson<int>();
                AreEqual(42,                            val);
                AreEqual("42",                          msg.Json.AsString());
                AreEqual(TestRelationPoC.TestMessageInt,msg.Name);
                
                IsTrue(msg.TryReadJson(out int result, out _));
                AreEqual(42, result);
                
                // test reading Json to incompatible types
                IsFalse(msg.TryReadJson<string>(out _, out var error));
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", error.Message);
                
                var e = Throws<JsonReaderException> (() => msg.ReadJson<string>());
                AreEqual("JsonReader/error: Cannot assign number to string. Expect: System.String, got: 42 path: '(root)' at position: 2", e.Message);
            });
            
            var subscribeMessage4   = store.SubscribeMessage  (TestRelationPoC.TestRemoveHandler, RemovedHandler);
            var unsubscribe1        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveHandler, RemovedHandler);

            var subscribeMessage5   = store.SubscribeMessage  (TestRelationPoC.TestRemoveAllHandler, RemovedHandler);
            var unsubscribe2        = store.UnsubscribeMessage(TestRelationPoC.TestRemoveAllHandler, null);
            
            var subscribeAllMessages= store.SubscribeMessage  ("Test*", msg => {
                processor.testWildcardCalls++;
            });

            await store.ExecuteTasksAsync(); // ----------------

            foreach (var subscription in subscriptions) {
                IsTrue(subscription.Success);    
            }
            IsTrue(articlesSub.Success);
                
            IsTrue(subscribeMessage.        Success);
            IsTrue(subscribeMessage1.       Success);
            IsTrue(subscribeMessage2.       Success);
            IsTrue(subscribeMessage3.       Success);
            IsTrue(subscribeMessage4.       Success);
            IsTrue(subscribeMessage5.       Success);
            IsTrue(unsubscribe1.            Success);
            IsTrue(unsubscribe2.            Success);
            IsTrue(subscribeAllMessages.    Success);
            return processor;
        }
        
        private static readonly MessageHandler<int> RemovedHandler = (msg) => {
            Fail("unexpected call");
        };
    }

    // assert expected database changes by counting the entity changes for each DatabaseContainer / EntitySet<>
    internal class PocSubscriptionProcessor : SubscriptionProcessor {
        private readonly    PocStore                client;
        private readonly    ChangeInfo<Order>       orderSum     = new ChangeInfo<Order>();
        private readonly    ChangeInfo<Customer>    customerSum  = new ChangeInfo<Customer>();
        private readonly    ChangeInfo<Article>     articleSum   = new ChangeInfo<Article>();
        private readonly    ChangeInfo<Producer>    producerSum  = new ChangeInfo<Producer>();
        private readonly    ChangeInfo<Employee>    employeeSum  = new ChangeInfo<Employee>();
        private             int                     messageCount;
        internal            int                     testMessageCalls;
        internal            int                     testMessageIntCalls;
        internal            int                     testWildcardCalls;
        internal            int                     subscribeEventsCalls;
        internal            bool                    receivedAll;
        
        private readonly    EventAssertion          eventAssertion;
        
        internal PocSubscriptionProcessor (PocStore client, EventAssertion eventAssertion)
            : base (client)
        {
            this.client         = client;
            this.eventAssertion = eventAssertion;
        }
            
        /// All tests using <see cref="PocSubscriptionProcessor"/> are required to use "createStore" as userId
        protected override void ProcessEvent (EventMessage ev) {
            AreEqual("createStore", ev.srcUserId.ToString());
            base.ProcessEvent(ev);
            var orderChanges    = GetEntityChanges(client.orders,    ev);
            var customerChanges = GetEntityChanges(client.customers, ev);
            var articleChanges  = GetEntityChanges(client.articles,  ev);
            var producerChanges = GetEntityChanges(client.producers, ev);
            var employeeChanges = GetEntityChanges(client.employees, ev);
            var messages        = GetMessages                      (ev);
            
            orderSum.   AddChanges(orderChanges);
            customerSum.AddChanges(customerChanges);
            articleSum. AddChanges(articleChanges);
            producerSum.AddChanges(producerChanges);
            employeeSum.AddChanges(employeeChanges);
            messageCount += messages.Count;

            foreach (var message in messages) {
                switch (message.Name) {
                    case nameof(TestRelationPoC.EndCreate):
                        AreEqual("null", message.Json.AsString());
                        break;
                    case nameof(TestRelationPoC.TestMessageInt):
                        var intVal = message.ReadJson<int>();
                        AreEqual(42, intVal);
                        break;
                    case nameof(TestRelationPoC.TestRemoveHandler):
                    case nameof(TestRelationPoC.TestRemoveAllHandler):
                        break;
                    case nameof(TestMessage):
                        var testVal = message.ReadJson<TestMessage>();
                        AreEqual("test message", testVal.text);
                        break;
                    default:
                        Fail("test expect handling all messages");
                        break;
                }
            }
            
            switch (eventAssertion) {
                case EventAssertion.NoChanges:
                    var changeInfo = ev.GetChangeInfo();
                    IsTrue(changeInfo.Count == 0);
                    break;
                case EventAssertion.Changes:
                    changeInfo = ev.GetChangeInfo();
                    IsTrue(changeInfo.Count > 0);
                    AssertChangeEvent(articleChanges);
                    break;
            }
        }
        
        private  void AssertChangeEvent (EntityChanges<string, Article> articleChanges) {
            switch (EventSequence) {
                case 2:
                    AreEqual("(creates: 0, upserts: 2, deletes: 0, patches: 0)", articleChanges.Info.ToString());
                    AreEqual("iPad Pro", articleChanges.upserts["article-ipad"].name);
                    break;
                case 5:
                    AreEqual("(creates: 0, upserts: 0, deletes: 1, patches: 1)", articleChanges.Info.ToString());
                    IsTrue(articleChanges.deletes.Contains("article-delete"));
                    ChangePatch<Article> articlePatch = articleChanges.patches["article-1"];
                    AreEqual("article-1",               articlePatch.ToString());
                    var articlePatch0 = (PatchReplace)  articlePatch.patches[0];
                    AreEqual("Changed name",            articlePatch.entity.name);
                    AreEqual("/name",                   articlePatch0.path);
                    AreEqual("\"Changed name\"",        articlePatch0.value.json.AsString());
                    break;
            }
        }
        
        /// assert that all database changes by <see cref="TestRelationPoC.CreateStore"/> are reflected
        public void AssertCreateStoreChanges() {
            AreEqual(9, EventSequence);
            AreEqual(9, subscribeEventsCalls);
            
            AreSimilar("(creates: 0, upserts: 2, deletes: 0, patches: 0)", orderSum);
            AreSimilar("(creates: 1, upserts: 5, deletes: 1, patches: 0)", customerSum);
            AreSimilar("(creates: 2, upserts: 7, deletes: 5, patches: 2)", articleSum);
            AreSimilar("(creates: 3, upserts: 0, deletes: 3, patches: 0)", producerSum);
            AreSimilar("(creates: 1, upserts: 0, deletes: 1, patches: 0)", employeeSum);
            
            AreEqual(5, messageCount);
            AreEqual(4, testWildcardCalls);

            AreEqual(1, testMessageCalls);
            AreEqual(2, testMessageIntCalls);
        }
    }
    
    static class PocUtils
    {
        public static void AddChanges<TKey, T> (this ChangeInfo<T> sum, EntityChanges<TKey, T> changes) where T: class {
            sum.creates += changes.creates.Count;
            sum.upserts += changes.upserts.Count;
            sum.deletes += changes.deletes.Count;
            sum.patches += changes.patches.Count;
        }
    }
    
    public partial class TestStore {
        [Test]      public void         AcknowledgeMessages() { SingleThreadSynchronizationContext.Run(AssertAcknowledgeMessages); }
            
        private static async Task AssertAcknowledgeMessages() {
            using (var _            = UtilsInternal.SharedPools) // for LeakTestsFixture
            using (var eventBroker  = new EventBroker(false))
            using (var database     = new MemoryDatabase())
            using (var hub          = new DatabaseHub(database))
            using (var typeStore    = new TypeStore())
            using (var listenDb     = new FlioxClient(hub, typeStore, null, "listenDb")) {
                listenDb.Hub.EventBroker = eventBroker;
                bool receivedHello = false;
                listenDb.SubscribeMessage("Hello", msg => {
                    receivedHello = true;
                });
                await listenDb.ExecuteTasksAsync();

                using (var sendStore  = new FlioxClient(hub, typeStore, null, "sendStore")) {
                    sendStore.SendMessage("Hello", "some text");
                    await sendStore.ExecuteTasksAsync();
                    
                    while (!receivedHello) {
                        await Task.Delay(1); // release thread to process message event handler
                    }
                    
                    await listenDb.ExecuteTasksAsync();

                    // assert no send events are pending which are not acknowledged
                    AreEqual(0, eventBroker.NotAcknowledgedEvents());
                }
            }
        }
    }

}